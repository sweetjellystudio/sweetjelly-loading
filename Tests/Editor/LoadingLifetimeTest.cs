using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SWeetJelly.Loading.App;
using SWeetJelly.Loading.Contract;

namespace SWeetJelly.Loading.Tests.EditMode
{
    public sealed class LoadingLifetimeTest
    {
        [Test]
        public async Task ShutdownWaitsForRunnerDrainAndIgnoresItsLateProgress()
        {
            var runner = new DrainingRunner();
            var service = new LoadingService(runner);
            var updates = 0;
            service.LoadingUpdated += (_, __) => updates++;
            var run = service.RunAsync(new LoadingRequest(LoadingOperationType.Simple));
            var shutdown = service.ShutdownAsync();
            Assert.That(service.IsLoading, Is.True);
            Assert.That(shutdown.IsCompleted, Is.False);
            runner.Progress(0.5f, "late");
            Assert.That(updates, Is.Zero);
            runner.Drain.TrySetResult(true);
            Assert.That((await run).Error.Code, Is.EqualTo("Loading.Cancelled"));
            await shutdown;
            Assert.That(service.IsLoading, Is.False);
            runner.Progress(1f, "after cleanup");
            Assert.That(updates, Is.Zero);
        }

        [TestCase(LoadingOperationType.LoadScene, null)]
        [TestCase(LoadingOperationType.LoadAdditiveScene, " ")]
        [TestCase((LoadingOperationType)99, null)]
        public async Task InvalidOperationRequestHasNoStartedNotification(LoadingOperationType type, string scene)
        {
            var service = new LoadingService(new ImmediateRunner());
            var started = false;
            service.LoadingStarted += (_, __) => started = true;
            var result = await service.RunAsync(new LoadingRequest(type, sceneName: scene));
            Assert.That(result.Error.Code, Is.EqualTo("Loading.InvalidRequest"));
            Assert.That(started, Is.False);
        }

        [Test]
        public async Task PreCancelledRunDoesNotNotifyStarted()
        {
            var service = new LoadingService(new ImmediateRunner());
            var started = false;
            service.LoadingStarted += (_, __) => started = true;
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var result = await service.RunAsync(new LoadingRequest(LoadingOperationType.None), cancellation.Token);
            Assert.That(result.Error.Code, Is.EqualTo("Loading.Cancelled"));
            Assert.That(started, Is.False);
        }

        [Test]
        public void FailedObserverErrorNotifiesRemainingObserversAndRestoresState()
        {
            var service = new LoadingService(new ImmediateRunner());
            var failure = new InvalidOperationException("started");
            var observerFailure = new ArgumentException("failed observer");
            Exception reported = null;
            service.LoadingStarted += (_, __) => throw failure;
            service.LoadingFailed += (_, __) => throw observerFailure;
            service.LoadingFailed += (_, args) => reported = args.Exception;
            var thrown = Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.RunAsync(new LoadingRequest(LoadingOperationType.None)));
            Assert.That(thrown, Is.SameAs(observerFailure));
            Assert.That(reported, Is.SameAs(failure));
            Assert.That(service.IsLoading, Is.False);
        }

        [Test]
        public async Task VeryLongMinimumDurationRemainsCancellable()
        {
            var service = new LoadingService(new ImmediateRunner());
            using var cancellation = new CancellationTokenSource(100);
            var result = await service.RunAsync(new LoadingRequest(LoadingOperationType.None,
                minimumDuration: float.MaxValue), cancellation.Token);
            Assert.That(result.Error.Code, Is.EqualTo("Loading.Cancelled"));
        }

        [Test]
        public async Task DisposalCancelsOwnRunAndRejectsSubsequentRun()
        {
            var service = new LoadingService(new WaitingRunner());
            using var safety = new CancellationTokenSource(2000);
            var run = service.RunAsync(new LoadingRequest(LoadingOperationType.Simple), safety.Token);
            try
            {
                var lifetime = (object)service as IDisposable;
                Assert.That(lifetime, Is.Not.Null, "The owner needs an explicit close boundary.");
                lifetime.Dispose();
                Assert.That((await run).Error.Code, Is.EqualTo("Loading.Cancelled"));
                var rejected = await service.RunAsync(new LoadingRequest(LoadingOperationType.None));
                Assert.That(rejected.Error.Code, Is.EqualTo("Loading.Disposed"));
            }
            finally
            {
                safety.Cancel();
                await run;
            }
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public async Task InvalidDurationHasNoStartedNotification(float duration)
        {
            var service = new LoadingService(new ImmediateRunner());
            var started = false;
            service.LoadingStarted += (_, __) => started = true;
            var result = await service.RunAsync(new LoadingRequest(LoadingOperationType.None,
                minimumDuration: duration));
            Assert.That(result.Error.Code, Is.EqualTo("Loading.InvalidRequest"));
            Assert.That(started, Is.False);
        }

        [Test]
        public async Task ReadyFromStartedIsRememberedUntilManualGate()
        {
            var service = new LoadingService(new ImmediateRunner());
            service.LoadingStarted += (_, __) => service.MarkAsReadyToComplete();
            using var timeout = new CancellationTokenSource(500);
            var result = await service.RunAsync(new LoadingRequest(LoadingOperationType.None,
                waitForManualCompletion: true), timeout.Token);
            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void CompletedObserverFailureNotifiesRemainingObserversWithoutFailed()
        {
            var service = new LoadingService(new ImmediateRunner());
            var observed = false;
            var failed = false;
            var error = new InvalidOperationException("observer");
            service.LoadingCompleted += (_, __) => throw error;
            service.LoadingCompleted += (_, __) => observed = true;
            service.LoadingFailed += (_, __) => failed = true;

            var thrown = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await service.RunAsync(new LoadingRequest(LoadingOperationType.None)));
            Assert.That(thrown, Is.SameAs(error));
            Assert.That(observed, Is.True);
            Assert.That(failed, Is.False);
            Assert.That(service.IsLoading, Is.False);
        }

        private sealed class ImmediateRunner : ILoadingOperationRunner
        {
            public Task RunAsync(LoadingRequest request, Action<float, string> progressCallback,
                CancellationToken ct = default) => Task.CompletedTask;
        }

        private sealed class WaitingRunner : ILoadingOperationRunner
        {
            public Task RunAsync(LoadingRequest request, Action<float, string> progressCallback,
                CancellationToken ct = default) => Task.Delay(Timeout.Infinite, ct);
        }

        private sealed class DrainingRunner : ILoadingOperationRunner
        {
            public readonly TaskCompletionSource<bool> Drain = new TaskCompletionSource<bool>();
            public Action<float, string> Progress;
            public Task RunAsync(LoadingRequest request, Action<float, string> progressCallback,
                CancellationToken ct = default)
            {
                Progress = progressCallback;
                return Drain.Task;
            }
        }
    }
}
