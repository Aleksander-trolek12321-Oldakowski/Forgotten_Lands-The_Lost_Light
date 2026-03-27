using UnityEngine;
using UnityEngine.UI;

namespace SideQuests
{
    public class SideQuestOfferSlotUI : MonoBehaviour
    {
        [Header("UI")]
        public Text titleText;
        public Text objectiveText;
        public Text rewardText;
        public Button selectButton;

        private int questIndex = -1;
        private SideQuestBoardUI boardUI;

        private void Awake()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(OnSelectClicked);
            }
        }

        public void SetQuest(SideQuestDefinition quest, int index, SideQuestBoardUI owner)
        {
            boardUI = owner;
            questIndex = index;

            if (titleText != null)
                titleText.text = quest != null ? quest.title : "";

            if (objectiveText != null)
            {
                if (quest != null)
                {
                    string objective = SideQuestManager.Instance != null
                        ? SideQuestManager.Instance.GetOfferObjectiveText(quest)
                        : "";

                    objectiveText.text = string.IsNullOrEmpty(objective) ? quest.description : objective;
                }
                else
                {
                    objectiveText.text = "";
                }
            }

            if (rewardText != null)
            {
                rewardText.text = quest != null && SideQuestManager.Instance != null
                    ? SideQuestManager.Instance.GetRewardText(quest)
                    : "";
            }

            if (selectButton != null)
                selectButton.interactable = quest != null;
        }

        public void Clear()
        {
            questIndex = -1;
            boardUI = null;

            if (titleText != null) titleText.text = "";
            if (objectiveText != null) objectiveText.text = "";
            if (rewardText != null) rewardText.text = "";

            if (selectButton != null)
                selectButton.interactable = false;
        }

        private void OnSelectClicked()
        {
            if (boardUI != null && questIndex >= 0)
            {
                boardUI.ChooseQuest(questIndex);
            }
        }
    }
}