using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using SWeetJelly.Loading.Contract;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SWeetJelly.Loading.Framework.Unity
{
    public sealed class LoadingBehaviour : MonoBehaviour, ILoadingOperationRunner
    {
        private object owner;
        private CancellationTokenSource activeCancellation;
        private AsyncOperation activeOperation;
        private bool destroyed;

        public bool IsRunning => activeCancellation != null;

        /// <summary>Claim an idle adapter for a bootstrap owner. Calls use Unity's main thread.</summary>
        public bool TryAcquire(object candidate)
        {
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));
            if (destroyed || owner != null || IsRunning) return false;
            owner = candidate;
            return true;
        }

        public bool Release(object candidate)
        {
            if (!ReferenceEquals(owner, candidate) || owner == null) return false;
            owner = null;
            return true;
        }

        public async Task RunAsync(LoadingRequest request, Action<float, string> progressCallback,
            CancellationToken ct = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (destroyed) throw new ObjectDisposedException(nameof(LoadingBehaviour));
            if (IsRunning) throw new InvalidOperationException("Loading adapter is already running.");
            ct.ThrowIfCancellationRequested();
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var token = cancellation.Token;
            activeCancellation = cancellation;
            try
            {
                switch (request.OperationType)
                {
                    case LoadingOperationType.None:
                        return;
                    case LoadingOperationType.Simple:
                        await Task.Yield();
                        token.ThrowIfCancellationRequested();
                        return;
                    case LoadingOperationType.LoadScene:
                    case LoadingOperationType.LoadAdditiveScene:
                        if (string.IsNullOrWhiteSpace(request.SceneName))
                            throw new ArgumentException("Scene name cannot be empty.", nameof(request));
                        token.ThrowIfCancellationRequested();
                        var mode = request.OperationType == LoadingOperationType.LoadScene
                            ? LoadSceneMode.Single : LoadSceneMode.Additive;
                        var operation = SceneManager.LoadSceneAsync(request.SceneName, mode);
                        if (operation == null)
                            throw new InvalidOperationException($"Failed to load scene: {request.SceneName}");
                        activeOperation = operation;
                        await DrainSceneAsync(operation, request.Message, progressCallback, token);
                        return;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(request));
                }
            }
            finally
            {
                activeOperation = null;
                activeCancellation = null;
            }
        }

        private void OnDestroy()
        {
            Close();
            owner = null;
        }

        /// <summary>Close before deferred destruction so a new owner cannot claim a doomed component.</summary>
        public void Close()
        {
            if (destroyed) return;
            destroyed = true;
            // Unlock synchronously: the holder can disappear before any continuation runs.
            if (activeOperation != null) activeOperation.allowSceneActivation = true;
            activeCancellation?.Cancel();
        }

        private static async Task DrainSceneAsync(AsyncOperation operation, string message,
            Action<float, string> progressCallback, CancellationToken ct)
        {
            ExceptionDispatchInfo failure = null;
            try
            {
                operation.allowSceneActivation = false;
                while (operation.progress < 0.9f)
                {
                    ct.ThrowIfCancellationRequested();
                    progressCallback?.Invoke(Mathf.Clamp01(operation.progress / 0.9f), message);
                    await Task.Yield();
                }
                ct.ThrowIfCancellationRequested();
                operation.allowSceneActivation = true;
                while (!operation.isDone)
                {
                    ct.ThrowIfCancellationRequested();
                    progressCallback?.Invoke(1f, message);
                    await Task.Yield();
                }
                ct.ThrowIfCancellationRequested();
            }
            catch (Exception ex)
            {
                failure = ExceptionDispatchInfo.Capture(ex);
            }
            finally
            {
                operation.allowSceneActivation = true;
                // Engine operations cannot be cancelled. Drain without invoking consumer callbacks.
                while (!operation.isDone) await Task.Yield();
            }
            failure?.Throw();
            ct.ThrowIfCancellationRequested();
        }
    }
}
