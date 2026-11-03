namespace SWeetJelly.Loading.Tests.EditMode
{
    using NUnit.Framework;
    using SWeetJelly.Bootstrapper.App;
    using SWeetJelly.Bootstrapper.Framework.Unity;
    using SWeetJelly.Loading.Contract;
    using SWeetJelly.Loading.Framework.Unity;
    using SWeetJelly.Loading.Framework.Bootstrapper;
    using UnityEngine;

    public sealed class LoadingPackageIntegrationTest
    {
        [Test]
        public void ClosingCreatedAdapterStillAllowsOwnerToRemoveIt()
        {
            var holder = new GameObject("Closed owned adapter");
            var context = new PackageBootstrapContext(holder, null, null, new ServiceRegistry());
            var module = ScriptableObject.CreateInstance<LoadingPackageBootstrapperSo>();
            try
            {
                module.Bootstrap(context);
                holder.GetComponent<LoadingBehaviour>().Close();
                module.Shutdown(context);
                Assert.That(holder.GetComponent<LoadingBehaviour>(), Is.Null);
            }
            finally
            {
                module.Shutdown(context);
                Object.DestroyImmediate(module);
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void FailedRegistrationPreservesAdapterClaimedByReplacementOwner()
        {
            var holder = new GameObject("Replacement during registration");
            var replacement = new object();
            var registry = new FailingRegistry();
            registry.AfterRegistration = published =>
            {
                var selected = holder.GetComponent<LoadingBehaviour>();
                Assert.That(selected.Release(published), Is.True);
                Assert.That(selected.TryAcquire(replacement), Is.True);
            };
            var context = new PackageBootstrapContext(holder, null, null, registry);
            var module = ScriptableObject.CreateInstance<LoadingPackageBootstrapperSo>();
            try
            {
                Assert.Throws<System.InvalidOperationException>(() => module.Bootstrap(context));
                var selected = holder.GetComponent<LoadingBehaviour>();
                Assert.That(selected, Is.Not.Null);
                Assert.That(selected.Release(replacement), Is.True);
                Assert.That(registry.TryResolve<ILoadingService>(out _), Is.False);
            }
            finally
            {
                module.Shutdown(context);
                Object.DestroyImmediate(module);
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void BorrowedAdapterSurvivesShutdownAndCanBeReused()
        {
            var holder = new GameObject("Borrowed adapter");
            var adapter = holder.AddComponent<LoadingBehaviour>();
            var registry = new ServiceRegistry();
            var context = new PackageBootstrapContext(holder, null, null, registry);
            var module = ScriptableObject.CreateInstance<LoadingPackageBootstrapperSo>();
            try
            {
                module.Bootstrap(context);
                var old = registry.Resolve<ILoadingService>();
                module.Shutdown(context);
                Assert.That(adapter != null, Is.True);
                Assert.That(registry.TryResolve<ILoadingService>(out _), Is.False);
                Assert.That(old.RunAsync(new LoadingRequest(LoadingOperationType.None)).Result.Error.Code,
                    Is.EqualTo("Loading.Disposed"));
                module.Bootstrap(context);
                Assert.That(registry.Resolve<ILoadingService>(), Is.Not.SameAs(old));
                Assert.That(holder.GetComponent<LoadingBehaviour>(), Is.SameAs(adapter));
            }
            finally
            {
                module.Shutdown(context);
                Object.DestroyImmediate(module);
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void RegistrationFailureRollsBackPublishedServiceAndCreatedAdapter()
        {
            var holder = new GameObject("Failed registration");
            var registry = new FailingRegistry();
            var context = new PackageBootstrapContext(holder, null, null, registry);
            var module = ScriptableObject.CreateInstance<LoadingPackageBootstrapperSo>();
            try
            {
                Assert.Throws<System.InvalidOperationException>(() => module.Bootstrap(context));
                Assert.That(registry.TryResolve<ILoadingService>(out _), Is.False);
                Assert.That(holder.GetComponent<LoadingBehaviour>(), Is.Null);
            }
            finally
            {
                module.Shutdown(context);
                Object.DestroyImmediate(module);
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void ShutdownPreservesReplacedRegistrationAndAdapterOwner()
        {
            var holder = new GameObject("Replacement owner");
            var registry = new ServiceRegistry();
            var context = new PackageBootstrapContext(holder, null, null, registry);
            var module = ScriptableObject.CreateInstance<LoadingPackageBootstrapperSo>();
            var replacement = new SWeetJelly.Loading.App.LoadingService(new NoOperation());
            var replacementOwner = new object();
            try
            {
                module.Bootstrap(context);
                var original = registry.Resolve<ILoadingService>();
                var adapter = holder.GetComponent<LoadingBehaviour>();
                Assert.That(adapter.Release(original), Is.True);
                Assert.That(adapter.TryAcquire(replacementOwner), Is.True);
                registry.Unregister<ILoadingService>();
                registry.Register<ILoadingService>(replacement);
                module.Shutdown(context);
                Assert.That(registry.Resolve<ILoadingService>(), Is.SameAs(replacement));
                Assert.That(adapter != null, Is.True);
                Assert.That(adapter.Release(replacementOwner), Is.True);
                Assert.That(replacement.RunAsync(new LoadingRequest(LoadingOperationType.None)).Result.IsSuccess,
                    Is.True);
            }
            finally
            {
                module.Shutdown(context);
                replacement.Dispose();
                Object.DestroyImmediate(module);
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void SharedAdapterCannotBeClaimedByAnotherBootstrapper()
        {
            var holder = new GameObject("Shared adapter");
            var adapter = holder.AddComponent<LoadingBehaviour>();
            var first = ScriptableObject.CreateInstance<LoadingPackageBootstrapperSo>();
            var second = ScriptableObject.CreateInstance<LoadingPackageBootstrapperSo>();
            var context = new PackageBootstrapContext(holder, null, null, new ServiceRegistry());
            var other = new PackageBootstrapContext(holder, null, null, new ServiceRegistry());
            try
            {
                first.Bootstrap(context);
                Assert.Throws<System.InvalidOperationException>(() => second.Bootstrap(other));
                Assert.That(other.Services.TryResolve<ILoadingService>(out _), Is.False);
                Assert.That(holder.GetComponent<LoadingBehaviour>(), Is.SameAs(adapter));
            }
            finally
            {
                second.Shutdown(other);
                first.Shutdown(context);
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
                Object.DestroyImmediate(holder);
            }
        }

        private sealed class FailingRegistry : SWeetJelly.Bootstrapper.Contract.IServiceRegistry
        {
            private readonly ServiceRegistry inner = new ServiceRegistry();
            public System.Action<object> AfterRegistration;
            public void Register<T>(T service) where T : class
            {
                inner.Register(service);
                AfterRegistration?.Invoke(service);
                throw new System.InvalidOperationException("Failure after publication");
            }
            public void Unregister<T>() where T : class => inner.Unregister<T>();
            public T Resolve<T>() where T : class => inner.Resolve<T>();
            public bool TryResolve<T>(out T service) where T : class => inner.TryResolve(out service);
        }

        [Test]
        public void ExistingRegistrationIsPreservedWithoutCreatingAdapter()
        {
            var holder = new GameObject("Foreign registration");
            var module = ScriptableObject.CreateInstance<LoadingPackageBootstrapperSo>();
            var registry = new ServiceRegistry();
            var existing = new SWeetJelly.Loading.App.LoadingService(new NoOperation());
            registry.Register<ILoadingService>(existing);
            try
            {
                Assert.Throws<System.InvalidOperationException>(() => module.Bootstrap(
                    new PackageBootstrapContext(holder, null, null, registry)));
                Assert.That(registry.Resolve<ILoadingService>(), Is.SameAs(existing));
                Assert.That(holder.GetComponent<LoadingBehaviour>(), Is.Null);
            }
            finally
            {
                module.Shutdown(new PackageBootstrapContext(holder, null, null, registry));
                Object.DestroyImmediate(module);
                Object.DestroyImmediate(holder);
            }
        }

        private sealed class NoOperation : ILoadingOperationRunner
        {
            public System.Threading.Tasks.Task RunAsync(LoadingRequest request,
                System.Action<float, string> progressCallback, System.Threading.CancellationToken ct = default)
                => System.Threading.Tasks.Task.CompletedTask;
        }

        [Test]
        public void LoadingPackageBootstrapperSo_RegistersLoadingServiceAndAddsLoadingBehaviour()
        {
            var holder = new GameObject("Loading Test Holder");
            var module = ScriptableObject.CreateInstance<LoadingPackageBootstrapperSo>();

            try
            {
                var registry = new ServiceRegistry();
                var context = new PackageBootstrapContext(holder, null, null, registry);

                module.Bootstrap(context);

                Assert.That(holder.GetComponent<LoadingBehaviour>(), Is.Not.Null);
                Assert.That(registry.TryResolve<ILoadingService>(out var service), Is.True);
                Assert.That(service, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(module);
                Object.DestroyImmediate(holder);
            }
        }
    }
}
