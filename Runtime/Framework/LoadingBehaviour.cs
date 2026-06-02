using System;
using System.Threading;
using System.Threading.Tasks;
using SWeetJelly.Loading.Contract;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SWeetJelly.Loading.Framework
{
    public sealed class LoadingBehaviour : MonoBehaviour, ILoadingOperationRunner
    {
        public async Task RunAsync(
            LoadingRequest request,
            Action<float, string> progressCallback,
            CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            switch (request.OperationType)
            {
                case LoadingOperationType.None:
                    return;

                case LoadingOperationType.Simple:
                    await Task.Yield();
                    return;

                case LoadingOperationType.LoadScene:
                    await LoadSceneInternalAsync(
                        request.SceneName,
                        LoadSceneMode.Single,
                        request.Message,
                        progressCallback,
                        ct);
                    return;

                case LoadingOperationType.LoadAdditiveScene:
                    await LoadSceneInternalAsync(
                        request.SceneName,
                        LoadSceneMode.Additive,
                        request.Message,
                        progressCallback,
                        ct);
                    return;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async Task LoadSceneInternalAsync(
            string sceneName,
            LoadSceneMode mode,
            string message,
            Action<float, string> progressCallback,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                throw new ArgumentException("Scene name cannot be empty.", nameof(sceneName));

            var operation = SceneManager.LoadSceneAsync(sceneName, mode);
            if (operation == null)
                throw new InvalidOperationException($"Failed to load scene: {sceneName}");

            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
            {
                ct.ThrowIfCancellationRequested();

                var progress = Mathf.Clamp01(operation.progress / 0.9f);
                progressCallback?.Invoke(progress, message);

                await Task.Yield();
            }

            operation.allowSceneActivation = true;

            while (!operation.isDone)
            {
                ct.ThrowIfCancellationRequested();

                progressCallback?.Invoke(1f, message);

                await Task.Yield();
            }
        }
    }
}