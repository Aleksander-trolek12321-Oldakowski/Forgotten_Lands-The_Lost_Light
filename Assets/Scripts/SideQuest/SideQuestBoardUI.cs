using UnityEngine;
using UnityEngine.UI;
using Player;
using TMPro;
using Inventory;

namespace SideQuests
{
    public class SideQuestBoardUI : MonoBehaviour
    {
        [Header("Root")]
        public GameObject root;

        [Header("UI")]
        public TextMeshProUGUI infoText;
        public SideQuestOfferSlotUI[] offerSlots = new SideQuestOfferSlotUI[3];

        private PlayerBase currentPlayer;
        public bool IsOpen => root != null && root.activeSelf;

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
            if (IsOpen)
                return;

            currentPlayer = player;

            root.SetActive(true);
            InputBlocker.Block(currentPlayer);

            if (SideQuestManager.Instance != null)
            {
                SideQuestManager.Instance.EnsureOffers();
            }

            Refresh();
        }

        public void Close()
        {
            if (!IsOpen && currentPlayer == null)
                return;

            if (root != null)
                root.SetActive(false);

            InputBlocker.Restore(currentPlayer);

            currentPlayer = null;
        }

        private void Update()
        {
            if (root != null && root.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                InputBlocker.NotifyEscapeHandledByUi();
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
