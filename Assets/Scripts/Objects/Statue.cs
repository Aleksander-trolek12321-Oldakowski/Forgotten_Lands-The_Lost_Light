using UnityEngine;
using Player;

namespace Objects
{
    [RequireComponent(typeof(Collider))]
    public class Statue : MonoBehaviour
    {
        [Header("UI")]
        public GameObject StatueUI;

        [Header("Levels")]
        public string ChoosenLevel = "";

        private bool playerInRange = false;
        private PlayerBase nearbyPlayer;

        private string level1 = "Level1";
        private string level2 = "Level2";

        private bool prevCursorVisible;
        private CursorLockMode prevCursorLockState;
        private Behaviour savedCinemachineBrain = null;

        private void OnTriggerEnter(Collider other)
        {
            PlayerBase player = other.GetComponent<PlayerBase>();
            if (player != null)
            {
                playerInRange = true;
                nearbyPlayer = player;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerBase player = other.GetComponent<PlayerBase>();
            if (player != null && player == nearbyPlayer)
            {
                playerInRange = false;
                nearbyPlayer = null;
            }
        }

        private void Update()
        {
            if (!playerInRange || nearbyPlayer == null) return;

            if (Input.GetKeyDown(KeyCode.E))
            {
                OpenStatueUI();
            }
        }

        public void OpenStatueUI()
        {
            if (StatueUI == null)
            {
                Debug.LogWarning("Statue: not assignes StatueUI!");
                return;
            }

            StatueUI.SetActive(true);

            prevCursorVisible = Cursor.visible;
            prevCursorLockState = Cursor.lockState;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (nearbyPlayer != null)
            {
                nearbyPlayer.SetControlsEnabled(false);
                var brainComp = nearbyPlayer.cam.GetComponent("CinemachineBrain") as Behaviour;
                if (brainComp != null && brainComp.enabled)
                {
                    savedCinemachineBrain = brainComp;
                    savedCinemachineBrain.enabled = false;
                }
            }
        }

        public void CloseStatueUI()
        {
            if (StatueUI != null)
                StatueUI.SetActive(false);

            Cursor.visible = prevCursorVisible;
            Cursor.lockState = prevCursorLockState;

            if (nearbyPlayer != null)
            {
                nearbyPlayer.SetControlsEnabled(true);
            }

            if (savedCinemachineBrain != null)
            {
                savedCinemachineBrain.enabled = true;
                savedCinemachineBrain = null;
            }
        }

        public void Level1()
        {
            ChoosenLevel = level1;
            CloseStatueUI();
        }

        public void Level2()
        {
            ChoosenLevel = level2;
            CloseStatueUI();
        }

        private void OnDisable()
        {
            if (nearbyPlayer != null)
            {
                nearbyPlayer.SetControlsEnabled(true);
            }
        }
    }
}
