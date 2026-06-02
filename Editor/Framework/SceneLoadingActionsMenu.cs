using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace SWeetJelly.Core
{
    public static class SceneLoadingActionMenu
    {
        [MenuItem("SWeetJelly/Actions/Game Scene", priority = 100)]
        private static void GameScene()
        {
            EditorSceneManager.OpenScene(@"Assets\Project Data\Game\Scenes\Game.unity");
        }

        [MenuItem("SWeetJelly/Actions/Game Scene", true)]
        private static bool GameSceneValidation()
        {
            return !Application.isPlaying;
        }
    }
}