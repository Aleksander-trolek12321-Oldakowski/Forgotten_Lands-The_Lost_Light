using UnityEngine;
using UnityEngine.SceneManagement;
using Player;
using Inventory;
using GameSave;

namespace Menu
{
    public class PauseMenuController : MonoBehaviour
    {
        [Header("UI")]
        public GameObject pauseRoot;

        [Header("Keys")]
        public KeyCode pauseKey = KeyCode.Escape;

        [Header("Scenes")]
        public string hubSceneName = SaveService.HubSceneName;
        public string menuSceneName = SaveService.MenuSceneName;

        private bool isOpen = false;
        private PlayerBase player;

        private void Start()
        {
            player = FindObjectOfType<PlayerBase>();
            if (pauseRoot != null)
                pauseRoot.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(pauseKey))
            {
                if (isOpen) ResumeGame();
                else OpenPause();
            }
        }

        public void OpenPause()
        {
            if (isOpen) return;
            isOpen = true;

            if (player == null) player = FindObjectOfType<PlayerBase>();
            InputBlocker.Block(player);
            if (player != null) player.SetControlsEnabled(false);

            if (pauseRoot != null) pauseRoot.SetActive(true);

            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void ResumeGame()
        {
            if (!isOpen && pauseRoot != null && !pauseRoot.activeSelf) return;

            isOpen = false;
            if (pauseRoot != null) pauseRoot.SetActive(false);

            Time.timeScale = 1f;

            InputBlocker.Restore(player);
            if (player != null) player.SetControlsEnabled(true);

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void ReturnToHub()
        {
            PrepareAndSaveForExit();
            SceneManager.LoadScene(hubSceneName);
        }

        public void ReturnToMenu()
        {
            PrepareAndSaveForExit();
            SceneManager.LoadScene(menuSceneName);
        }

        private void PrepareAndSaveForExit()
        {
            Time.timeScale = 1f;

            string currentScene = SceneManager.GetActiveScene().name;
            bool inHub = string.Equals(currentScene, hubSceneName);

            SaveService.CaptureAndSave(
                targetSceneName: hubSceneName,
                includeHubPosition: inHub,
                clearCurrentSceneChestState: !inHub,
                clearHubPositionWhenNotIncluded: !inHub
            );

            isOpen = false;
            if (pauseRoot != null) pauseRoot.SetActive(false);
        }
    }
}
