using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SideQuests
{
    public class SideQuestHUDUI : MonoBehaviour
    {
        [Header("Root")]
        public GameObject root;

        [Header("UI")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI progressText;
        public TextMeshProUGUI rewardText;
        public TextMeshProUGUI statusText;

        private SideQuestManager subscribedManager;

        private void Awake()
        {
            Refresh();
        }

        private void OnEnable()
        {
            TryBindToManager();
            Refresh();
        }

        private void OnDisable()
        {
            UnbindFromManager();
        }

        private void Update()
        {
            if (subscribedManager == null && SideQuestManager.Instance != null)
            {
                TryBindToManager();
                Refresh();
            }
        }

        private void TryBindToManager()
        {
            SideQuestManager manager = SideQuestManager.Instance;
            if (manager == null || subscribedManager == manager)
                return;

            UnbindFromManager();
            subscribedManager = manager;
            subscribedManager.OnQuestDataChanged += Refresh;
        }

        private void UnbindFromManager()
        {
            if (subscribedManager == null)
                return;

            subscribedManager.OnQuestDataChanged -= Refresh;
            subscribedManager = null;
        }

        public void Refresh()
        {
            if (SideQuestManager.Instance == null)
            {
                if (root != null) root.SetActive(false);
                return;
            }

            var manager = SideQuestManager.Instance;

            if (!manager.HasActiveQuest)
            {
                if (root != null) root.SetActive(false);
                return;
            }

            if (root != null) root.SetActive(true);

            var quest = manager.ActiveQuest;

            if (titleText != null) titleText.text = quest.title;
            if (descriptionText != null) descriptionText.text = quest.description;
            if (rewardText != null) rewardText.text = manager.GetRewardText(quest);
            if (statusText != null) statusText.text = manager.GetActiveStatusText();

            if (progressText != null)
            {
                string progress = manager.GetActiveObjectiveText();
                progressText.text = string.IsNullOrEmpty(progress) ? "" : progress;
                progressText.gameObject.SetActive(!string.IsNullOrEmpty(progress));
            }
        }
    }
}
