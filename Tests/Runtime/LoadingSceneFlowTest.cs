namespace SWeetJelly.Loading.Tests.PlayMode
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Threading;
    using NUnit.Framework;
    using SWeetJelly.Loading.Contract;
    using SWeetJelly.Loading.Framework.Unity;
    using UnityEngine;
    using UnityEngine.TestTools;

    public sealed class LoadingSceneFlowTest
    {
        [UnityTest]
        public IEnumerator CancelledBeforeStartDoesNotRunSimpleOperation()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var task = behaviour.RunAsync(new LoadingRequest(LoadingOperationType.Simple), null,
                cancellation.Token);
            yield return WaitForTask(task);
            Assert.That(task.IsCanceled, Is.True);
        }

        private GameObject holder;
        private LoadingBehaviour behaviour;

        [SetUp]
        public void SetUp()
        {
            holder = new GameObject("Loading Behaviour Test Holder");
            behaviour = holder.AddComponent<LoadingBehaviour>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.Destroy(holder);
        }

        [UnityTest]
        public IEnumerator RunAsync_WithSimpleOperation_CompletesWithoutProgressCallback()
        {
            var progress = new List<float>();

            var task = behaviour.RunAsync(
                new LoadingRequest(LoadingOperationType.Simple, message: "Simple"),
                (value, _) => progress.Add(value));

            yield return WaitForTask(task);

            Assert.That(progress, Is.Empty);
        }

        [UnityTest]
        public IEnumerator RunAsync_WithEmptySceneName_ThrowsArgumentException()
        {
            var task = behaviour.RunAsync(
                new LoadingRequest(LoadingOperationType.LoadScene),
                (_, _) => { });

            yield return WaitForTask(task);

            Assert.That(task.Exception, Is.Not.Null);
            Assert.That(task.Exception.GetBaseException(), Is.TypeOf<ArgumentException>());
            Assert.That(task.Exception.GetBaseException().Message, Does.Contain("Scene name cannot be empty."));
        }

        private static IEnumerator WaitForTask(Task task)
        {
            var deadline = Time.realtimeSinceStartup + 10f;
            while (!task.IsCompleted)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Loading task timed out.");
                yield return null;
            }
        }
    }
}
