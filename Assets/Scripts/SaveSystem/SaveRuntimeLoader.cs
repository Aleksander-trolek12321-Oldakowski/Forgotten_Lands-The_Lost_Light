using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameSave
{
    public class SaveRuntimeLoader : MonoBehaviour
    {
        private static SaveRuntimeLoader instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null) return;

            GameObject go = new GameObject("SaveRuntimeLoader");
            instance = go.AddComponent<SaveRuntimeLoader>();
            DontDestroyOnLoad(go);
            instance.StartCoroutine(instance.ApplyAfterFrame());
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == SaveService.MenuSceneName)
                return;

            StartCoroutine(ApplyAfterFrame());
        }

        private IEnumerator ApplyAfterFrame()
        {
            yield return null;
            SaveService.ApplyToActiveScene();
        }
    }
}
