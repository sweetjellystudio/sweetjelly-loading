namespace SWeetJelly.Loading.InitialiserIntegration
{

    using UnityEngine;
    using SWeetJelly.Loading.Contract;
    using SWeetJelly.Loading.App;
    using SWeetJelly.Loading.Framework;
    using SWeetJelly.Initialiser.Contract;
    using SWeetJelly.Initialiser.Framework;

    [RegisterModule("Loading")]
    [CreateAssetMenu(
        fileName = "Loading Init Module", 
        menuName = "SWeetJelly/Initialiser/Modules/Loading")]
    public class LoadingInitModule : InitModuleSo
    {
        public override string ModuleName => "Loading";

        public override void Install(InitModuleInstallContext context)
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