namespace SWeetJelly.Loading.App
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Contract;
    using SWeetJelly.Shared.Result;

    #nullable enable

    public sealed class LoadingService : ILoadingService
    {
        private readonly ILoadingOperationRunner operationRunner;
        private TaskCompletionSource<bool>? manualCompletionSource;

        public event EventHandler<LoadingEventArgs>? LoadingStarted;
        public event EventHandler<LoadingEventArgs>? LoadingUpdated;
        public event EventHandler<LoadingEventArgs>? LoadingCompleted;
        public event EventHandler<LoadingFailedEventArgs>? LoadingFailed;

        public bool IsLoading { get; private set; }

        public LoadingService(ILoadingOperationRunner operationRunner)
        {
            this.operationRunner = operationRunner 
                ?? throw new ArgumentNullException(nameof(operationRunner));
        }

        public async Task<Result> RunAsync(LoadingRequest request, CancellationToken ct = default)
        {
            if (request == null)
                return Result.Failure(LoadingErrors.InvalidRequest);

            if (IsLoading)
                return Result.Failure(LoadingErrors.AlreadyRunning);

            IsLoading = true;
            manualCompletionSource = null;

            try
            {
                RaiseStarted(0f, request.Message);

                var startedAt = DateTimeOffset.UtcNow;

                await RunTasksAsync(request, ct);
                await operationRunner.RunAsync(request, RaiseUpdated, ct);
                await WaitMinimumDurationAsync(startedAt, request.MinimumDuration, ct);

                if (request.WaitForManualCompletion)
                    await WaitManualCompletionAsync(ct);

                RaiseUpdated(1f, "Done");
                RaiseCompleted(1f, "Done");

                return Result.Success();
            }
            catch (OperationCanceledException)
            {
                Error error = LoadingErrors.Cancelled;
                RaiseFailed(error, null);
                return Result.Failure(error);
            }
            catch (Exception ex)
            {
                Error error = LoadingErrors.OperationFailed(ex);
                RaiseFailed(error, ex);
                return Result.Failure(error);
            }
            finally
            {
                IsLoading = false;
                manualCompletionSource = null;
            }
        }

        public void MarkAsReadyToComplete()
        {
            manualCompletionSource?.TrySetResult(true);
        }

        private static async Task RunTasksAsync(LoadingRequest request, CancellationToken ct)
        {
            foreach (var task in request.Tasks)
            {
                ct.ThrowIfCancellationRequested();

                if (task == null)
                    continue;

                await task.RunAsync(ct);
            }
        }

        private static async Task WaitMinimumDurationAsync(
            DateTimeOffset startedAt,
            float minimumDuration,
            CancellationToken ct)
        {
            if (minimumDuration <= 0f)
                return;

            var minimum = TimeSpan.FromSeconds(minimumDuration);

            while (DateTimeOffset.UtcNow - startedAt < minimum)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
            }
        }

        private async Task WaitManualCompletionAsync(CancellationToken ct)
        {
            manualCompletionSource = new TaskCompletionSource<bool>();

            using (ct.Register(() => manualCompletionSource.TrySetCanceled(ct)))
            {
                await manualCompletionSource.Task;
            }
        }

        private void RaiseStarted(float progress, string message)
        {
            LoadingStarted?.Invoke(this, new LoadingEventArgs(progress, message));
        }

        private void RaiseUpdated(float progress, string message)
        {
            LoadingUpdated?.Invoke(this, new LoadingEventArgs(progress, message));
        }

        private void RaiseCompleted(float progress, string message)
        {
            LoadingCompleted?.Invoke(this, new LoadingEventArgs(progress, message));
        }

        private void RaiseFailed(Error error, Exception? exception)
        {
            LoadingFailed?.Invoke(this, new LoadingFailedEventArgs(error, exception));
        }
    }
}
