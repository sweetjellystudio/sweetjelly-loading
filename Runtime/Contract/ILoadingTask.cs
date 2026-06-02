using System.Threading;
using System.Threading.Tasks;

namespace SWeetJelly.Loading.Contract
{
    public interface ILoadingTask
    {
        Task RunAsync(CancellationToken ct);
    }
}