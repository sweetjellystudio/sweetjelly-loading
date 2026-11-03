namespace SWeetJelly.Loading.App
{
    using System;
    using System.Diagnostics;
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Contract;
    using SWeetJelly.Shared.Result;

    #nullable enable

    public sealed class LoadingService : ILoadingService, IDisposable
    {
        private readonly ILoadingOperationRunner operationRunner;
        private Run? activeRun;
        private bool disposed;

        public event EventHandler<LoadingEventArgs>? LoadingStarted;
        public event EventHandler<LoadingEventArgs>? LoadingUpdated;
        public event EventHandler<LoadingEventArgs>? LoadingCompleted;
        public event EventHandler<LoadingFailedEventArgs>? LoadingFailed;

        public bool IsLoading => activeRun != null;

        public LoadingService(ILoadingOperationRunner operationRunner)
        {
            this.operationRunner = operationRunner ?? throw new ArgumentNullException(nameof(operationRunner));
        }

        public Task<Result> RunAsync(LoadingRequest request, CancellationToken ct = default)
        {
            if (disposed) return Task.FromResult(Result.Failure(LoadingErrors.Disposed));
            if (!IsValid(request)) return Task.FromResult(Result.Failure(LoadingErrors.InvalidRequest));
            if (IsLoading) return Task.FromResult(Result.Failure(LoadingErrors.AlreadyRunning));
            if (ct.IsCancellationRequested) return Task.FromResult(Result.Failure(LoadingErrors.Cancelled));

            var run = new Run(ct, request.WaitForManualCompletion);
            activeRun = run;
            return ExecuteAsync(request, run);
        }

        public void MarkAsReadyToComplete()
        {
            if (!disposed) activeRun?.Ready?.TrySetResult(true);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            activeRun?.Cancellation.Cancel();
        }

        /// <summary>Close to new requests and await cleanup, independently of observer errors on RunAsync.</summary>
        public Task ShutdownAsync()
        {
            var drained = activeRun?.Drained.Task ?? Task.CompletedTask;
            Dispose();
            return drained;
        }

        private async Task<Result> ExecuteAsync(LoadingRequest request, Run run)
        {
            var ct = run.Cancellation.Token;
            try
            {
                Result result;
                Exception? failure = null;
                try
                {
                    LoadingStarted?.Invoke(this, new LoadingEventArgs(0f, request.Message));
                    ct.ThrowIfCancellationRequested();
                    foreach (var task in request.Tasks)
                    {
                        ct.ThrowIfCancellationRequested();
                        if (task != null) await task.RunAsync(ct);
                    }
                    ct.ThrowIfCancellationRequested();
                    await operationRunner.RunAsync(request, (value, message) =>
                    {
                        if (ReferenceEquals(activeRun, run) && !run.Terminal && !disposed
                            && !ct.IsCancellationRequested)
                            LoadingUpdated?.Invoke(this, new LoadingEventArgs(value, message));
                    }, ct);
                    ct.ThrowIfCancellationRequested();
                    await WaitMinimumDurationAsync(run.StartedAt, request.MinimumDuration, ct);
                    if (run.Ready != null)
                    {
                        using (ct.Register(() => run.Ready.TrySetCanceled(ct)))
                            await run.Ready.Task;
                    }
                    ct.ThrowIfCancellationRequested();
                    LoadingUpdated?.Invoke(this, new LoadingEventArgs(1f, "Done"));
                    ct.ThrowIfCancellationRequested();
                    result = Result.Success();
                }
                catch (OperationCanceledException)
                {
                    result = Result.Failure(LoadingErrors.Cancelled);
                }
                catch (Exception ex)
                {
                    failure = ex;
                    result = Result.Failure(LoadingErrors.OperationFailed(ex));
                }

                run.Terminal = true;
                if (result.IsSuccess)
                    NotifyAll(LoadingCompleted, new LoadingEventArgs(1f, "Done"));
                else
                    NotifyAll(LoadingFailed, new LoadingFailedEventArgs(result.Error, failure));
                return result;
            }
            finally
            {
                run.Terminal = true;
                activeRun = null;
                run.Cancellation.Dispose();
                run.Drained.TrySetResult(true);
            }
        }

        private static bool IsValid(LoadingRequest request)
        {
            return request != null && !float.IsNaN(request.MinimumDuration)
                && !float.IsInfinity(request.MinimumDuration)
                && Enum.IsDefined(typeof(LoadingOperationType), request.OperationType)
                && ((request.OperationType != LoadingOperationType.LoadScene
                    && request.OperationType != LoadingOperationType.LoadAdditiveScene)
                    || !string.IsNullOrWhiteSpace(request.SceneName));
        }

        private static async Task WaitMinimumDurationAsync(Stopwatch elapsed,
            float minimumDuration, CancellationToken ct)
        {
            while (minimumDuration > elapsed.Elapsed.TotalSeconds)
            {
                ct.ThrowIfCancellationRequested();
                var remaining = minimumDuration - elapsed.Elapsed.TotalSeconds;
                var milliseconds = (int)Math.Max(1, Math.Ceiling(Math.Min(remaining * 1000d, 50d)));
                await Task.Delay(milliseconds, ct);
            }
        }

        private void NotifyAll<T>(EventHandler<T>? handlers, T args) where T : EventArgs
        {
            ExceptionDispatchInfo? first = null;
            if (handlers == null) return;
            foreach (EventHandler<T> handler in handlers.GetInvocationList())
            {
                try { handler(this, args); }
                catch (Exception ex) { first ??= ExceptionDispatchInfo.Capture(ex); }
            }
            first?.Throw();
        }

        private sealed class Run
        {
            internal readonly CancellationTokenSource Cancellation;
            internal readonly TaskCompletionSource<bool>? Ready;
            internal readonly TaskCompletionSource<bool> Drained =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            internal readonly Stopwatch StartedAt = Stopwatch.StartNew();
            internal bool Terminal;

            internal Run(CancellationToken token, bool manual)
            {
                Cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
                if (manual) Ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }
    }
}
