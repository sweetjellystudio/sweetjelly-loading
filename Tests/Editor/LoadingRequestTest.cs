using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SWeetJelly.Loading.Contract;

namespace SWeetJelly.Loading.Tests.EditMode
{
    public sealed class LoadingRequestTest
    {
        [Test]
        public void RequestKeepsTasksWhenCallerChangesOriginalList()
        {
            var task = new EmptyTask();
            var tasks = new List<ILoadingTask> { task };
            var request = new LoadingRequest(LoadingOperationType.None, tasks: tasks);
            tasks.Clear();
            Assert.That(request.Tasks, Is.EqualTo(new[] { task }));
        }

        private sealed class EmptyTask : ILoadingTask
        {
            public Task RunAsync(CancellationToken ct) => Task.CompletedTask;
        }
    }
}
