using UnityEngine;
using Player;

namespace SideQuests
{
    [RequireComponent(typeof(Collider))]
    public class SideQuestBoard : MonoBehaviour
    {
        public SideQuestBoardUI boardUI;
        private PlayerBase nearbyPlayer;
        private bool playerInRange = false;

        private void Reset()
        {
            Collider c = GetComponent<Collider>();
            if (c != null) c.isTrigger = true;
        }

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

            if (Input.GetKeyDown(KeyCode.E))
            {
                OpenBoard();
            }
        }

        private void OpenBoard()
        {
            if (SideQuestManager.Instance != null && SideQuestManager.Instance.HasActiveQuest)
            {
                Debug.Log("SideQuestBoard: Board is locked because player already has an active side quest.");
                return;
            }

            if (boardUI == null)
            {
                Debug.LogWarning("SideQuestBoard: boardUI reference is missing.");
                return;
            }

            boardUI.Open(nearbyPlayer);
        }
    }
}