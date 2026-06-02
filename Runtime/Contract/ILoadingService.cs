using System;
using System.Threading;
using System.Threading.Tasks;

namespace SWeetJelly.Loading.Contract
{
    public interface ILoadingService
    {
        event EventHandler<LoadingEventArgs> LoadingStarted;
        event EventHandler<LoadingEventArgs> LoadingUpdated;
        event EventHandler<LoadingEventArgs> LoadingCompleted;
        event EventHandler<LoadingFailedEventArgs> LoadingFailed;

        bool IsLoading { get; }

        Task RunAsync(LoadingRequest request, CancellationToken ct = default);
        void MarkAsReadyToComplete();
    }
}