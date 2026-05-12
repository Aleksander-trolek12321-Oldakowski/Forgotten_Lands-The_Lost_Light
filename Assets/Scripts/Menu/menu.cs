using UnityEngine.SceneManagement;
using UnityEngine;
using GameSave;

namespace Menu
{
    public class menu : MonoBehaviour
    {
        public void Game()
        {
            string targetScene = SaveService.HasInitializedSave()
                ? SaveService.GetContinueSceneOrDefault("HUB")
                : "HUB";

            SceneManager.LoadScene(targetScene);
        }

        public void Quit()
        {
            Application.Quit();
            Debug.Log("QUIT");
        }
    }
}
