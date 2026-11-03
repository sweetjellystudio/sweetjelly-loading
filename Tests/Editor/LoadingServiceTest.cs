namespace SWeetJelly.Loading.Tests.EditMode
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using SWeetJelly.Loading.App;
    using SWeetJelly.Loading.Contract;

    public sealed class LoadingServiceTest
    {
        [Test]
        public void RunAsync_RaisesLifecycleEvents()
        {
            var runner = new FakeOperationRunner();
            var service = new LoadingService(runner);
            var events = new List<string>();

            service.LoadingStarted += (_, args) => events.Add($"started:{args.Progress}:{args.Message}");
            service.LoadingUpdated += (_, args) => events.Add($"updated:{args.Progress}:{args.Message}");
            service.LoadingCompleted += (_, args) => events.Add($"completed:{args.Progress}:{args.Message}");

            var result = service.RunAsync(new LoadingRequest(
                    LoadingOperationType.Simple,
                    message: "Loading"))
                .GetAwaiter()
                .GetResult();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(events, Does.Contain("started:0:Loading"));
            Assert.That(events, Does.Contain("updated:0.5:Loading"));
            Assert.That(events, Does.Contain("updated:1:Done"));
            Assert.That(events, Does.Contain("completed:1:Done"));
            Assert.That(service.IsLoading, Is.False);
            Assert.That(runner.RunCount, Is.EqualTo(1));
        }

        [Test]
        public void RunAsync_WhenAlreadyRunning_ReturnsFailure()
        {
            var runner = new BlockingOperationRunner();
            var service = new LoadingService(runner);

            var firstRun = service.RunAsync(new LoadingRequest(LoadingOperationType.Simple));

            var result = service.RunAsync(new LoadingRequest(LoadingOperationType.Simple))
                .GetAwaiter()
                .GetResult();

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo(LoadingErrors.AlreadyRunning));

            runner.Release();
            Assert.That(firstRun.GetAwaiter().GetResult().IsSuccess, Is.True);
        }

        [Test]
        public void RunAsync_WhenTaskFails_RaisesFailedAndResetsLoadingState()
        {
            var service = new LoadingService(new FakeOperationRunner());
            LoadingFailedEventArgs failedArgs = null;

            service.LoadingFailed += (_, args) => failedArgs = args;

            var result = service.RunAsync(new LoadingRequest(
                    LoadingOperationType.Simple,
                    tasks: new ILoadingTask[] { new FailingLoadingTask() }))
                .GetAwaiter()
                .GetResult();

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Loading.OperationFailed"));
            Assert.That(failedArgs, Is.Not.Null);
            Assert.That(failedArgs.Error, Is.EqualTo(result.Error));
            Assert.That(failedArgs.ErrorMessage, Is.EqualTo("Task failed."));
            Assert.That(service.IsLoading, Is.False);
        }

        [Test]
        public async Task RunAsync_WhenWaitingForManualCompletion_CompletesAfterMarkAsReadyToComplete()
        {
            var service = new LoadingService(new FakeOperationRunner());

            var run = service.RunAsync(new LoadingRequest(
                LoadingOperationType.Simple,
                waitForManualCompletion: true));

            Assert.That(run.IsCompleted, Is.False);

            service.MarkAsReadyToComplete();
            var result = await run;

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(service.IsLoading, Is.False);
        }

        private sealed class FakeOperationRunner : ILoadingOperationRunner
        {
            public int RunCount { get; private set; }

            public Task RunAsync(
                LoadingRequest request,
                Action<float, string> progressCallback,
                CancellationToken ct = default)
            {
                RunCount++;
                progressCallback?.Invoke(0.5f, request.Message);
                return Task.CompletedTask;
            }
        }

        private sealed class BlockingOperationRunner : ILoadingOperationRunner
        {
            private readonly TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();

            public Task RunAsync(
                LoadingRequest request,
                Action<float, string> progressCallback,
                CancellationToken ct = default)
            {
                return completionSource.Task;
            }

            public void Release()
            {
                completionSource.TrySetResult(true);
            }
        }

        private sealed class FailingLoadingTask : ILoadingTask
        {
            public Task RunAsync(CancellationToken ct)
            {
                throw new InvalidOperationException("Task failed.");
            }
        }

    }
}
