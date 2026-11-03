using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SWeetJelly.Loading.App;
using SWeetJelly.Loading.Contract;
using SWeetJelly.Loading.Framework.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SWeetJelly.Loading.Tests.PlayMode
{
    public sealed class LoadingEngineLifetimeTest : IPrebuildSetup, IPostBuildCleanup
    {
        private const string SceneA = "Packages/com.sweetjelly.loading/Tests/Runtime/Scenes/LoadingTargetA.unity";
        private const string SceneB = "Packages/com.sweetjelly.loading/Tests/Runtime/Scenes/LoadingTargetB.unity";
        private GameObject holder;
        private LoadingService service;
        private LoadingBehaviour adapter;
#if UNITY_EDITOR
        private const string SceneSetupKey = "SWeetJelly.Loading.Tests.SceneSetup";
        [Serializable]
        private sealed class SceneSnapshot
        {
            public string[] paths, guids;
            public bool[] enabled;
            public string settingsBytes;
        }
#endif

        public void Setup()
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(SessionState.GetString(SceneSetupKey, "")))
                throw new InvalidOperationException("A previous Loading test scene setup still needs cleanup.");
            var settingsBytes = System.IO.File.ReadAllBytes("ProjectSettings/EditorBuildSettings.asset");
            var originalScenes = EditorBuildSettings.scenes;
            var snapshot = new SceneSnapshot {
                paths = Array.ConvertAll(originalScenes, s => s.path),
                guids = Array.ConvertAll(originalScenes, s => s.guid.ToString()),
                enabled = Array.ConvertAll(originalScenes, s => s.enabled),
                settingsBytes = Convert.ToBase64String(settingsBytes)
            };
            SessionState.SetString(SceneSetupKey, JsonUtility.ToJson(snapshot));
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(originalScenes);
            foreach (var path in new[] { SceneA, SceneB })
                if (!scenes.Exists(s => s.path == path && s.enabled))
                    scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
#endif
        }

        public void Cleanup()
        {
#if UNITY_EDITOR
            var json = SessionState.GetString(SceneSetupKey, "");
            if (string.IsNullOrEmpty(json)) return;
            var snapshot = JsonUtility.FromJson<SceneSnapshot>(json);
            var scenes = new EditorBuildSettingsScene[snapshot.paths.Length];
            for (var i = 0; i < scenes.Length; i++)
                scenes[i] = new EditorBuildSettingsScene(snapshot.paths[i], snapshot.enabled[i]) {
                    guid = new GUID(snapshot.guids[i])
                };
            EditorBuildSettings.scenes = scenes;
            // The native setter normalizes unresolved scene GUIDs. Preserve the original file too.
            System.IO.File.WriteAllBytes("ProjectSettings/EditorBuildSettings.asset",
                Convert.FromBase64String(snapshot.settingsBytes));
            SessionState.EraseString(SceneSetupKey);
#endif
        }

        [SetUp]
        public void SetUp()
        {
            holder = new GameObject("Loading persistent test owner");
            UnityEngine.Object.DontDestroyOnLoad(holder);
            adapter = holder.AddComponent<LoadingBehaviour>();
            service = new LoadingService(adapter);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var shutdown = service.ShutdownAsync();
            yield return Wait(shutdown);
            UnityEngine.Object.Destroy(holder);
            // A Single operation can leave the fixture as the only scene. Keep one empty scene
            // before unloading the fixtures; the test runner restores the Editor's scene itself.
            var onlyFixtures = true;
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var path = SceneManager.GetSceneAt(index).path;
                if (path != SceneA && path != SceneB) onlyFixtures = false;
            }
            if (onlyFixtures)
                SceneManager.CreateScene("Loading test cleanup");
            foreach (var path in new[] { SceneA, SceneB })
            {
                var scene = SceneManager.GetSceneByPath(path);
                if (scene.IsValid() && scene.isLoaded)
                {
                    var unload = SceneManager.UnloadSceneAsync(scene);
                    var deadline = Time.realtimeSinceStartup + 10f;
                    while (unload != null && !unload.isDone)
                    {
                        Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
                        yield return null;
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator AdditiveSuccessLoadsScene()
        {
            var run = service.RunAsync(new LoadingRequest(LoadingOperationType.LoadAdditiveScene, SceneA));
            yield return Wait(run);
            Assert.That(run.Result.IsSuccess, Is.True);
            Assert.That(SceneManager.GetSceneByPath(SceneA).isLoaded, Is.True);
        }

        [UnityTest]
        public IEnumerator SingleSuccessReplacesScene()
        {
            var run = service.RunAsync(new LoadingRequest(LoadingOperationType.LoadScene, SceneA));
            yield return Wait(run);
            Assert.That(run.Result.IsSuccess, Is.True);
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(SceneA));
        }

        [UnityTest]
        public IEnumerator CancelledAdditiveDrainsAndLeavesSceneLoaded() => VerifyDrain(0, false);

        [UnityTest]
        public IEnumerator CancelledSingleDrainsAndLeavesSceneLoaded() => VerifyDrain(0, true);

        [UnityTest]
        public IEnumerator ProgressFailureDrainsAndPreservesException() => VerifyDrain(1, false);

        [UnityTest]
        public IEnumerator AdapterDestructionDrainsWithoutComponent() => VerifyDrain(2, false);

        [UnityTest]
        public IEnumerator PreCancelledSceneDoesNotStart()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var run = service.RunAsync(new LoadingRequest(LoadingOperationType.LoadAdditiveScene, SceneA),
                cancellation.Token);
            yield return Wait(run);
            Assert.That(run.Result.Error.Code, Is.EqualTo("Loading.Cancelled"));
            Assert.That(SceneManager.GetSceneByPath(SceneA).isLoaded, Is.False);
        }

        [UnityTest]
        public IEnumerator MinimumAndManualCompletionContinueAtTimeScaleZero()
        {
            var original = Time.timeScale;
            try
            {
                Time.timeScale = 0;
                service.LoadingStarted += (_, __) => service.MarkAsReadyToComplete();
                var started = Time.realtimeSinceStartup;
                var run = service.RunAsync(new LoadingRequest(LoadingOperationType.Simple,
                    minimumDuration: 0.1f, waitForManualCompletion: true));
                yield return Wait(run);
                Assert.That(run.Result.IsSuccess, Is.True);
                Assert.That(Time.realtimeSinceStartup - started, Is.GreaterThanOrEqualTo(0.09f));
            }
            finally { Time.timeScale = original; }
        }

        private IEnumerator VerifyDrain(int interruption, bool single)
        {
            using var cancellation = new CancellationTokenSource();
            var interrupted = false;
            var expected = new InvalidOperationException("progress observer");
            Exception reported = null;
            service.LoadingFailed += (_, args) => reported = args.Exception;
            service.LoadingUpdated += (_, __) =>
            {
                if (interrupted) return;
                interrupted = true;
                if (interruption == 0) cancellation.Cancel();
                else if (interruption == 1) throw expected;
                else UnityEngine.Object.Destroy(holder);
            };
            var run = service.RunAsync(new LoadingRequest(single ? LoadingOperationType.LoadScene
                : LoadingOperationType.LoadAdditiveScene, SceneA), cancellation.Token);
            Assert.That(service.IsLoading, Is.True, "A started engine operation must drain.");
            var duplicate = service.RunAsync(new LoadingRequest(LoadingOperationType.None));
            Assert.That(duplicate.Result.Error.Code, Is.EqualTo("Loading.AlreadyRunning"));
            yield return Wait(run);
            Assert.That(interrupted, Is.True);
            Assert.That(run.Result.Error.Code, Is.EqualTo(interruption == 1
                ? "Loading.OperationFailed" : "Loading.Cancelled"));
            if (interruption == 1) Assert.That(reported, Is.SameAs(expected));
            Assert.That(service.IsLoading, Is.False);
            Assert.That(SceneManager.GetSceneByPath(SceneA).isLoaded, Is.True);
            // A second engine operation proves the queue is no longer activation-locked.
            var next = SceneManager.LoadSceneAsync(SceneB, LoadSceneMode.Additive);
            var deadline = Time.realtimeSinceStartup + 10f;
            while (!next.isDone)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Engine queue is stalled.");
                yield return null;
            }
            Assert.That(SceneManager.GetSceneByPath(SceneB).isLoaded, Is.True);
        }

        private static IEnumerator Wait(Task task)
        {
            var deadline = Time.realtimeSinceStartup + 10f;
            while (!task.IsCompleted)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Loading cleanup timed out.");
                yield return null;
            }
            if (task.IsFaulted) throw task.Exception.GetBaseException();
        }
    }
}
