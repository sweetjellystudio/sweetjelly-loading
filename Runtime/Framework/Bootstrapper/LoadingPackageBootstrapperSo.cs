namespace SWeetJelly.Loading.Framework.Bootstrapper
{
    using System;
    using UnityEngine;
    using SWeetJelly.Loading.Contract;
    using SWeetJelly.Loading.App;
    using SWeetJelly.Loading.Framework.Unity;
    using SWeetJelly.Bootstrapper.Contract;
    using SWeetJelly.Bootstrapper.Framework.Unity;

    [CreateAssetMenu(fileName = "Loading Package Bootstrapper",
        menuName = "SWeetJelly/Bootstrapper/Packages/Loading")]
    public class LoadingPackageBootstrapperSo : PackageBootstrapperSo
    {
        private LoadingService service;
        private LoadingBehaviour adapter;
        private IServiceRegistry registry;
        private bool createdAdapter;

        public override string PackageName => "Loading";

        public override void Bootstrap(PackageBootstrapContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (service != null || context.Services.TryResolve<ILoadingService>(out _))
                throw new InvalidOperationException("A loading service is already registered or owned.");

            var selected = context.HolderObject.GetComponent<LoadingBehaviour>();
            var created = selected == null;
            if (created) selected = context.HolderObject.AddComponent<LoadingBehaviour>();
            var candidate = new LoadingService(selected);
            var acquired = false;
            try
            {
                if (!selected.TryAcquire(candidate))
                    throw new InvalidOperationException("Loading adapter is already owned, running or closed.");
                acquired = true;
                context.Services.Register<ILoadingService>(candidate);
            }
            catch
            {
                try
                {
                    candidate.Dispose();
                }
                finally
                {
                    if (context.Services.TryResolve<ILoadingService>(out var registered)
                        && ReferenceEquals(registered, candidate))
                        context.Services.Unregister<ILoadingService>();
                    var released = acquired && selected != null && selected.Release(candidate);
                    if (created && released) RemoveOwnedAdapter(selected);
                }
                throw;
            }
            service = candidate;
            adapter = selected;
            registry = context.Services;
            createdAdapter = created;
        }

        public override void Shutdown(PackageBootstrapContext context)
        {
            if (service == null) return;
            var owned = service;
            var ownedAdapter = adapter;
            var ownedRegistry = registry;
            var removeAdapter = createdAdapter;
            service = null;
            adapter = null;
            registry = null;
            createdAdapter = false;
            try
            {
                owned.Dispose();
            }
            finally
            {
                try
                {
                    if (ownedRegistry.TryResolve<ILoadingService>(out var registered)
                        && ReferenceEquals(registered, owned))
                        ownedRegistry.Unregister<ILoadingService>();
                }
                finally
                {
                    if (ownedAdapter != null)
                    {
                        if (ownedAdapter.Release(owned) && removeAdapter)
                            RemoveOwnedAdapter(ownedAdapter);
                    }
                }
            }
        }

        private static void RemoveOwnedAdapter(LoadingBehaviour owned)
        {
            owned.Close();
            if (Application.isPlaying) Destroy(owned);
            else DestroyImmediate(owned);
        }
    }
}
