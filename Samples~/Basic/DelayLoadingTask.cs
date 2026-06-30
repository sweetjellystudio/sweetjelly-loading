namespace SWeetJelly.Samples.Loading
{
    using System.Threading;
    using System.Threading.Tasks;
    using SWeetJelly.Loading.Contract;

    public sealed class DelayLoadingTask : ILoadingTask
    {
        private readonly int milliseconds;

        public DelayLoadingTask(int milliseconds)
        {
            this.milliseconds = milliseconds < 0 ? 0 : milliseconds;
        }

        public Task RunAsync(CancellationToken ct)
        {
            return Task.Delay(milliseconds, ct);
        }
    }
}
