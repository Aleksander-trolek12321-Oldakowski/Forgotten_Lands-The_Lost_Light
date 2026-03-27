using UnityEngine;
using UnityEngine.UI;
using Player;

namespace SideQuests
{
    public class SideQuestBoardUI : MonoBehaviour
    {
        [Header("Root")]
        public GameObject root;

        [Header("UI")]
        public Text infoText;
        public SideQuestOfferSlotUI[] offerSlots = new SideQuestOfferSlotUI[3];

        private PlayerBase currentPlayer;

        private void Awake()
        {
            if (root != null)
                root.SetActive(false);
        }

        public void Open(PlayerBase player)
        {
            if (root == null)
            {
                Debug.LogWarning("SideQuestBoardUI: root is not assigned.");
                return;
            }

            currentPlayer = player;

            root.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (currentPlayer != null)
                currentPlayer.SetControlsEnabled(false);

            if (SideQuestManager.Instance != null)
            {
                SideQuestManager.Instance.EnsureOffers();
            }

            Refresh();
        }

        public void Close()
        {
            if (root != null)
                root.SetActive(false);

            if (currentPlayer != null)
                currentPlayer.SetControlsEnabled(true);

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            currentPlayer = null;
        }

        private void Update()
        {
            if (root != null && root.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        public void Refresh()
        {
            if (SideQuestManager.Instance == null) return;

            if (infoText != null)
            {
                if (SideQuestManager.Instance.HasActiveQuest)
                    infoText.text = "Masz już aktywnego sidequesta.";
                else
                    infoText.text = "Wybierz 1 sidequesta. Tylko jeden może być aktywny naraz.";
            }

            var offers = SideQuestManager.Instance.CurrentOffers;

            for (int i = 0; i < offerSlots.Length; i++)
            {
                if (offerSlots[i] == null) continue;

                if (i < offers.Count)
                {
                    offerSlots[i].SetQuest(offers[i], i, this);
                }
                else
                {
                    offerSlots[i].Clear();
                }
            }
        }

        public void ChooseQuest(int index)
        {
            if (SideQuestManager.Instance == null) return;

            bool taken = SideQuestManager.Instance.TakeQuest(index);
            if (taken)
            {
                if (SideQuestManager.Instance.activeQuestHUD != null)
                    SideQuestManager.Instance.activeQuestHUD.Refresh();

                Close();
            }
            else
            {
                Refresh();
            }
        }
    }
}