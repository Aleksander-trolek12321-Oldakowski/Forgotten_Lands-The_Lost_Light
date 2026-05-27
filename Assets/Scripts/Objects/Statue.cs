using UnityEngine;
using Player;
using Inventory;

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
        private PlayerBase interactingPlayer;
        private bool isUiOpen = false;
        private float reopenBlockUntilUnscaledTime = 0f;

        private void OnTriggerEnter(Collider other)
        {
            PlayerBase player = other.GetComponent<PlayerBase>();
            if (player == null)
                player = other.GetComponentInParent<PlayerBase>();
            if (player != null)
            {
                playerInRange = true;
                nearbyPlayer = player;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerBase player = other.GetComponent<PlayerBase>();
            if (player == null)
                player = other.GetComponentInParent<PlayerBase>();
            if (player != null && player == nearbyPlayer)
            {
                playerInRange = false;
                nearbyPlayer = null;
            }
        }

        private void Update()
        {
            if (!playerInRange || nearbyPlayer == null) return;
            if (isUiOpen) return;
            if (Time.unscaledTime < reopenBlockUntilUnscaledTime) return;

            if (Input.GetKeyDown(KeyCode.E))
            {
                OpenStatueUI();
            }
        }

        public void OpenStatueUI()
        {
            if (isUiOpen)
                return;

            if (StatueUI == null)
            {
                Debug.LogWarning("Statue: not assignes StatueUI!");
                return;
            }

            interactingPlayer = nearbyPlayer;
            StatueUI.SetActive(true);
            isUiOpen = true;
            InputBlocker.Block(interactingPlayer);
        }

        public void CloseStatueUI()
        {
            if (!isUiOpen)
                return;

            if (StatueUI != null)
                StatueUI.SetActive(false);

            isUiOpen = false;
            // Prevent immediate re-open in the same / next frame on some build input orders.
            reopenBlockUntilUnscaledTime = Time.unscaledTime + 0.15f;
            InputBlocker.Restore(interactingPlayer);
            interactingPlayer = null;
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
            if (isUiOpen)
                CloseStatueUI();
        }
    }
}
