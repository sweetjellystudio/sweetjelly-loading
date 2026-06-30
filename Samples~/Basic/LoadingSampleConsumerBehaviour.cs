namespace SWeetJelly.Samples.Loading
{
    using System;
    using SWeetJelly.Bootstrapper.Framework.Unity;
    using SWeetJelly.Loading.Contract;
    using SWeetJelly.Shared.Result;
    using UnityEngine;

    public sealed class LoadingSampleConsumerBehaviour : MonoBehaviour
    {
        private ILoadingService loadingService;
        private string status = "Waiting for Loading service...";
        private float progress;

        private void OnEnable()
        {
            BootstrapperBehaviour.Bootstrapped += OnBootstrapped;
            ResolveIfReady();
        }

        private void OnDisable()
        {
            BootstrapperBehaviour.Bootstrapped -= OnBootstrapped;
            Unsubscribe();
        }

        private void OnBootstrapped(object sender, EventArgs args)
        {
            ResolveIfReady();
        }

        private void ResolveIfReady()
        {
            if (loadingService != null || !BootstrapperBehaviour.IsBootstrapped || BootstrapperBehaviour.Services == null)
                return;

            if (!BootstrapperBehaviour.Services.TryResolve<ILoadingService>(out loadingService))
            {
                status = "ILoadingService is not registered.";
                return;
            }

            loadingService.LoadingStarted += OnLoadingStarted;
            loadingService.LoadingUpdated += OnLoadingUpdated;
            loadingService.LoadingCompleted += OnLoadingCompleted;
            loadingService.LoadingFailed += OnLoadingFailed;

            status = "Loading service is ready.";
        }

        private void Unsubscribe()
        {
            if (loadingService == null)
                return;

            loadingService.LoadingStarted -= OnLoadingStarted;
            loadingService.LoadingUpdated -= OnLoadingUpdated;
            loadingService.LoadingCompleted -= OnLoadingCompleted;
            loadingService.LoadingFailed -= OnLoadingFailed;
        }

        private void OnLoadingStarted(object sender, LoadingEventArgs args)
        {
            try
            {
                progress = args.Progress;
                status = $"Started: {args.Message}";
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
            }
        }

        private void OnLoadingUpdated(object sender, LoadingEventArgs args)
        {
            try
            {
                progress = args.Progress;
                status = $"{args.Message} ({progress:P0})";
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
            }
        }

        private void OnLoadingCompleted(object sender, LoadingEventArgs args)
        {
            try
            {
                progress = args.Progress;
                status = $"Completed: {args.Message}";
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
            }
        }

        private void OnLoadingFailed(object sender, LoadingFailedEventArgs args)
        {
            try
            {
                status = $"Failed: {args.ErrorMessage}";
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
            }
        }

        private void OnGUI()
        {
            const float x = 16f;
            var y = 16f;

            GUI.Label(new Rect(x, y, 520f, 24f), status);
            y += 30f;

            GUI.Box(new Rect(x, y, 300f, 18f), string.Empty);
            GUI.Box(new Rect(x, y, 300f * Mathf.Clamp01(progress), 18f), string.Empty);
            y += 34f;

            GUI.enabled = loadingService != null && !loadingService.IsLoading;

            if (GUI.Button(new Rect(x, y, 190f, 28f), "Run Timed Load"))
                RunTimedLoad();

            y += 34f;

            if (GUI.Button(new Rect(x, y, 190f, 28f), "Run Manual Load"))
                RunManualLoad();

            y += 34f;

            if (GUI.Button(new Rect(x, y, 190f, 28f), "Trigger Failure"))
                RunFailureLoad();

            y += 42f;

            GUI.enabled = loadingService != null && loadingService.IsLoading;

            if (GUI.Button(new Rect(x, y, 190f, 28f), "Mark Ready"))
                loadingService.MarkAsReadyToComplete();

            GUI.enabled = true;
        }

        private async void RunTimedLoad()
        {
            await RunAsync(new LoadingRequest(
                LoadingOperationType.Simple,
                message: "Timed loading sample",
                minimumDuration: 1.5f,
                tasks: new[] { new DelayLoadingTask(500) }));
        }

        private async void RunManualLoad()
        {
            await RunAsync(new LoadingRequest(
                LoadingOperationType.Simple,
                message: "Manual loading sample",
                minimumDuration: 0.5f,
                waitForManualCompletion: true,
                tasks: new[] { new DelayLoadingTask(500) }));
        }

        private async void RunFailureLoad()
        {
            await RunAsync(new LoadingRequest(
                LoadingOperationType.LoadScene,
                message: "Failure sample"));
        }

        private async System.Threading.Tasks.Task RunAsync(LoadingRequest request)
        {
            if (loadingService == null)
                return;

            try
            {
                Result result = await loadingService.RunAsync(request);

                if (result.IsFailure)
                    status = $"Failed: {result.Error}";
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
            }
        }
    }
}
