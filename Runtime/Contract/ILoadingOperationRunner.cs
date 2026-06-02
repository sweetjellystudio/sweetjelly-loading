using System;
using System.Threading;
using System.Threading.Tasks;

namespace SWeetJelly.Loading.Contract
{
    public interface ILoadingOperationRunner
    {
        Task RunAsync(LoadingRequest request, Action<float, string> progressCallback, CancellationToken ct = default);
    }

}