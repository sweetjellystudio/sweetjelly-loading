namespace SWeetJelly.Samples.Loading
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using SWeetJelly.Bootstrapper.Framework.Unity;
    using SWeetJelly.Loading.Contract;
    using UnityEngine;
    using UnityEngine.UIElements;

    [RequireComponent(typeof(UIDocument))]
    public sealed class LoadingSampleConsumerBehaviour : MonoBehaviour
    {
        private ILoadingService loadingService;
        private CancellationTokenSource ownRequest;
        private bool ownManualRequest;
        private string status = "Waiting for Loading service...";
        private float progress;
        private Label statusLabel;
        private ProgressBar progressBar;
        private Button timedButton, manualButton, failureButton, readyButton, cancelButton;

        private void OnEnable()
        {
            BootstrapperBehaviour.Bootstrapped += OnBootstrapped;
            BindView();
            ResolveIfReady();
        }

        private void Start() => BindView();

        private void Update()
        {
            if (statusLabel == null) BindView();
            Render();
        }

        private void OnDisable()
        {
            BootstrapperBehaviour.Bootstrapped -= OnBootstrapped;
            Unsubscribe();
            UnbindView();
            var request = ownRequest;
            ownRequest = null;
            ownManualRequest = false;
            request?.Cancel(); // The awaiting method disposes after its Run has drained.
        }

        private void OnBootstrapped(object sender, EventArgs args) => ResolveIfReady();

        private void ResolveIfReady()
        {
            if (!BootstrapperBehaviour.IsBootstrapped || BootstrapperBehaviour.Services == null) return;
            if (!BootstrapperBehaviour.Services.TryResolve<ILoadingService>(out var resolved))
            {
                status = "ILoadingService is not registered.";
                return;
            }
            if (ReferenceEquals(resolved, loadingService)) return;
            Unsubscribe();
            loadingService = resolved;
            loadingService.LoadingStarted += OnLoadingStarted;
            loadingService.LoadingUpdated += OnLoadingUpdated;
            loadingService.LoadingCompleted += OnLoadingCompleted;
            loadingService.LoadingFailed += OnLoadingFailed;
            status = "Loading service is ready.";
        }

        private void Unsubscribe()
        {
            if (loadingService == null) return;
            loadingService.LoadingStarted -= OnLoadingStarted;
            loadingService.LoadingUpdated -= OnLoadingUpdated;
            loadingService.LoadingCompleted -= OnLoadingCompleted;
            loadingService.LoadingFailed -= OnLoadingFailed;
            loadingService = null;
        }

        private void BindView()
        {
            UnbindView();
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return;
            statusLabel = root.Q<Label>("statusLabel");
            progressBar = root.Q<ProgressBar>("loadingProgress");
            timedButton = root.Q<Button>("timedButton");
            manualButton = root.Q<Button>("manualButton");
            failureButton = root.Q<Button>("failureButton");
            readyButton = root.Q<Button>("readyButton");
            cancelButton = root.Q<Button>("cancelButton");
            if (timedButton != null) timedButton.clicked += RunTimedLoad;
            if (manualButton != null) manualButton.clicked += RunManualLoad;
            if (failureButton != null) failureButton.clicked += RunFailureLoad;
            if (readyButton != null) readyButton.clicked += MarkReady;
            if (cancelButton != null) cancelButton.clicked += CancelOwnRequest;
        }

        private void UnbindView()
        {
            if (timedButton != null) timedButton.clicked -= RunTimedLoad;
            if (manualButton != null) manualButton.clicked -= RunManualLoad;
            if (failureButton != null) failureButton.clicked -= RunFailureLoad;
            if (readyButton != null) readyButton.clicked -= MarkReady;
            if (cancelButton != null) cancelButton.clicked -= CancelOwnRequest;
            statusLabel = null;
            progressBar = null;
            timedButton = manualButton = failureButton = readyButton = cancelButton = null;
        }

        private void Render()
        {
            if (statusLabel == null) return;
            statusLabel.text = status;
            progressBar.value = Mathf.Clamp01(progress) * 100f;
            progressBar.title = $"{progress:P0}";
            var canStart = loadingService != null && !loadingService.IsLoading && ownRequest == null;
            timedButton.SetEnabled(canStart);
            manualButton.SetEnabled(canStart);
            failureButton.SetEnabled(canStart);
            var canControl = ownRequest != null && !ownRequest.IsCancellationRequested;
            readyButton.SetEnabled(canControl && ownManualRequest);
            cancelButton.SetEnabled(canControl);
        }

        private void OnLoadingStarted(object sender, LoadingEventArgs args) => Show(args, "Started: ");
        private void OnLoadingUpdated(object sender, LoadingEventArgs args) => Show(args, "");
        private void OnLoadingCompleted(object sender, LoadingEventArgs args) => Show(args, "Completed: ");
        private void Show(LoadingEventArgs args, string prefix)
        {
            try { progress = args.Progress; status = prefix + args.Message; Render(); }
            catch (Exception ex) { Debug.LogException(ex, this); }
        }
        private void OnLoadingFailed(object sender, LoadingFailedEventArgs args)
        {
            try { status = $"Failed: {args.ErrorMessage}"; Render(); }
            catch (Exception ex) { Debug.LogException(ex, this); }
        }

        private async void RunTimedLoad() => await RunAsync(new LoadingRequest(
            LoadingOperationType.Simple, message: "Timed loading sample", minimumDuration: 1.5f,
            tasks: new[] { new DelayLoadingTask(500) }));

        private async void RunManualLoad() => await RunAsync(new LoadingRequest(
            LoadingOperationType.Simple, message: "Manual loading sample", minimumDuration: 0.5f,
            waitForManualCompletion: true, tasks: new[] { new DelayLoadingTask(500) }));

        private async void RunFailureLoad() => await RunAsync(new LoadingRequest(
            LoadingOperationType.LoadScene, message: "Failure sample"));

        private void MarkReady()
        {
            if (ownRequest != null && !ownRequest.IsCancellationRequested && ownManualRequest)
                loadingService?.MarkAsReadyToComplete();
        }

        private void CancelOwnRequest() => ownRequest?.Cancel();

        private async Task RunAsync(LoadingRequest request)
        {
            if (loadingService == null || loadingService.IsLoading || ownRequest != null) return;
            var cancellation = new CancellationTokenSource();
            ownRequest = cancellation;
            ownManualRequest = request.WaitForManualCompletion;
            try
            {
                var result = await loadingService.RunAsync(request, cancellation.Token);
                if (ReferenceEquals(ownRequest, cancellation) && result.IsFailure)
                    status = $"Failed: {result.Error}";
            }
            catch (Exception ex)
            {
                if (this != null && ReferenceEquals(ownRequest, cancellation))
                {
                    status = ex.Message;
                    Debug.LogException(ex, this);
                }
            }
            finally
            {
                if (ReferenceEquals(ownRequest, cancellation))
                {
                    ownRequest = null;
                    ownManualRequest = false;
                }
                cancellation.Dispose();
            }
        }
    }
}
