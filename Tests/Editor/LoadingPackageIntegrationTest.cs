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
