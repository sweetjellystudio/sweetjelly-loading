namespace SWeetJelly.Loading.Framework.Bootstrapper
{

    using UnityEngine;
    using SWeetJelly.Loading.Contract;
    using SWeetJelly.Loading.App;
    using SWeetJelly.Loading.Framework.Unity;
    using SWeetJelly.Bootstrapper.Framework.Unity;
    [CreateAssetMenu(
        fileName = "Loading Package Bootstrapper", 
        menuName = "SWeetJelly/Bootstrapper/Packages/Loading")]
    public class LoadingPackageBootstrapperSo : PackageBootstrapperSo
    {
        public override string PackageName => "Loading";

        public override void Bootstrap(PackageBootstrapContext context)
        {
            if (context == null)
                throw new System.ArgumentNullException(nameof(context));

            var adapter = context.HolderObject.GetComponent<LoadingBehaviour>();

            if (adapter == null)
            {
                adapter = context.HolderObject.AddComponent<LoadingBehaviour>();
            }

            var service = new LoadingService(adapter);

            context.Services.Register<ILoadingService>(service);
        }
    }
}