using System;
using System.Collections.Generic;
using UnityEngine;
using Item;
using Inventory;
using Player;

namespace SideQuests
{
    public enum SideQuestKind
    {
        KillEnemies,
        KillBoss,
        KillSpiders,
        KillSkeletons,
        LevelUps,
        SurviveTime
    }

    public enum QuestEnemyCategory
    {
        Generic,
        Spider,
        Skeleton,
        Boss
    }

    public enum QuestRewardTier
    {
        Normal,
        Better,
        Legendary
    }

    [Serializable]
    public class SideQuestDefinition
    {
        [Header("UI")]
        public string title;
        [TextArea] public string description;

        [Header("Quest")]
        public SideQuestKind kind;
        public int targetAmount = 1;

        [Tooltip("Used only for timed quests. The timer is hidden in UI.")]
        public float timeLimitMinutes = 0f;

        [Header("Rewards")]
        public int goldReward = 0;
        public float expReward = 0f;
        public QuestRewardTier rewardTier = QuestRewardTier.Normal;
    }

    public class SideQuestManager : MonoBehaviour
    {
        public static SideQuestManager Instance { get; private set; }

        [Header("References")]
        public PlayerBase player;
        public InventoryManager inventoryManager;

        [Header("Quest pool")]
        public List<SideQuestDefinition> allQuests = new List<SideQuestDefinition>();

        [Header("Reward item pools")]
        public List<ItemData> normalRewardItems = new List<ItemData>();
        public List<ItemData> betterRewardItems = new List<ItemData>();
        public List<ItemData> legendaryRewardItems = new List<ItemData>();

        [Header("UI")]
        public SideQuestBoardUI boardUI;
        public SideQuestHUDUI activeQuestHUD;

        [Header("Settings")]
        public int offersCount = 3;

        private readonly List<SideQuestDefinition> availablePool = new List<SideQuestDefinition>();
        private readonly List<SideQuestDefinition> currentOffers = new List<SideQuestDefinition>();

        private SideQuestDefinition activeQuest;
        private float activeQuestStartedAt;

        private int startEnemyKills;
        private int startBossKills;
        private int startSpiderKills;
        private int startSkeletonKills;
        private int startLevelUps;

        private int totalEnemyKills;
        private int totalBossKills;
        private int totalSpiderKills;
        private int totalSkeletonKills;
        private int totalLevelUps;

        public bool HasActiveQuest => activeQuest != null;
        public SideQuestDefinition ActiveQuest => activeQuest;
        public IReadOnlyList<SideQuestDefinition> CurrentOffers => currentOffers;

        public event Action OnQuestDataChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (player == null) player = FindObjectOfType<PlayerBase>();
            if (inventoryManager == null) inventoryManager = FindObjectOfType<InventoryManager>();

            RebuildPoolIfNeeded();
        }

        private void Start()
        {
            EnsureOffers();
            RefreshAllUI();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (activeQuest == null) return;

            if (activeQuest.kind == SideQuestKind.SurviveTime)
            {
                float limitSeconds = activeQuest.timeLimitMinutes * 60f;
                if (limitSeconds > 0f && Time.time - activeQuestStartedAt >= limitSeconds)
                {
                    CompleteActiveQuest();
                }
            }
        }

        private void RebuildPoolIfNeeded()
        {
            availablePool.Clear();

            for (int i = 0; i < allQuests.Count; i++)
            {
                var q = allQuests[i];
                if (q == null) continue;

                if (activeQuest != null && q == activeQuest) continue;

                availablePool.Add(q);
            }
        }

        public void EnsureOffers()
        {
            if (HasActiveQuest) return;

            if (currentOffers.Count >= offersCount) return;

            if (availablePool.Count < offersCount)
            {
                RebuildPoolIfNeeded();
            }

            currentOffers.Clear();

            List<SideQuestDefinition> temp = new List<SideQuestDefinition>(availablePool);

            while (currentOffers.Count < offersCount && temp.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, temp.Count);
                SideQuestDefinition picked = temp[index];

                currentOffers.Add(picked);
                temp.RemoveAt(index);

                availablePool.Remove(picked);
            }

            OnQuestDataChanged?.Invoke();
        }

        public bool TakeQuest(int offerIndex)
        {
            if (HasActiveQuest) return false;
            if (offerIndex < 0 || offerIndex >= currentOffers.Count) return false;

            activeQuest = currentOffers[offerIndex];
            currentOffers.Clear();

            startEnemyKills = totalEnemyKills;
            startBossKills = totalBossKills;
            startSpiderKills = totalSpiderKills;
            startSkeletonKills = totalSkeletonKills;
            startLevelUps = totalLevelUps;

            activeQuestStartedAt = Time.time;

            Debug.Log($"SideQuestManager: Accepted quest '{activeQuest.title}'");

            RefreshAllUI();
            return true;
        }

        public void ReportEnemyKilled(QuestEnemyCategory category)
        {
            totalEnemyKills++;

            switch (category)
            {
                case QuestEnemyCategory.Spider:
                    totalSpiderKills++;
                    break;
                case QuestEnemyCategory.Skeleton:
                    totalSkeletonKills++;
                    break;
                case QuestEnemyCategory.Boss:
                    totalBossKills++;
                    break;
            }

            CheckActiveQuestProgress();
        }

        public void NotifyPlayerLevelUp()
        {
            totalLevelUps++;
            CheckActiveQuestProgress();
        }

        private void CheckActiveQuestProgress()
        {
            if (activeQuest == null) return;

            bool complete = false;

            switch (activeQuest.kind)
            {
                case SideQuestKind.KillEnemies:
                    complete = (totalEnemyKills - startEnemyKills) >= activeQuest.targetAmount;
                    break;

                case SideQuestKind.KillBoss:
                    complete = (totalBossKills - startBossKills) >= activeQuest.targetAmount;
                    break;

                case SideQuestKind.KillSpiders:
                    complete = (totalSpiderKills - startSpiderKills) >= activeQuest.targetAmount;
                    break;

                case SideQuestKind.KillSkeletons:
                    complete = (totalSkeletonKills - startSkeletonKills) >= activeQuest.targetAmount;
                    break;

                case SideQuestKind.LevelUps:
                    complete = (totalLevelUps - startLevelUps) >= activeQuest.targetAmount;
                    break;

                case SideQuestKind.SurviveTime:
                    break;
            }

            if (complete)
            {
                CompleteActiveQuest();
            }

            RefreshAllUI();
        }

        private void CompleteActiveQuest()
        {
            if (activeQuest == null) return;

            Debug.Log($"SideQuestManager: Quest completed '{activeQuest.title}'");

            GrantRewards(activeQuest);

            activeQuest = null;

            EnsureOffers();
            RefreshAllUI();
        }

        private void GrantRewards(SideQuestDefinition quest)
        {
            if (player != null)
            {
                if (quest.goldReward > 0)
                    player.AddMoney(quest.goldReward);

                if (quest.expReward > 0f)
                    player.AddExp(quest.expReward);
            }

            ItemData rewardItem = RollRewardItem(quest.rewardTier);
            if (rewardItem != null && inventoryManager != null)
            {
                bool added = inventoryManager.TryAddToBackpack(rewardItem, 1);
                if (!added)
                {
                    Debug.LogWarning($"SideQuestManager: Inventory full, reward item '{rewardItem.itemName}' could not be added.");
                }
                else
                {
                    Debug.Log($"SideQuestManager: Reward item given '{rewardItem.itemName}'");
                }
            }
        }

        private ItemData RollRewardItem(QuestRewardTier tier)
        {
            List<ItemData> pool = null;

            switch (tier)
            {
                case QuestRewardTier.Normal:
                    pool = normalRewardItems;
                    break;
                case QuestRewardTier.Better:
                    pool = betterRewardItems;
                    break;
                case QuestRewardTier.Legendary:
                    pool = legendaryRewardItems;
                    break;
            }

            if (pool == null || pool.Count == 0)
            {
                if (legendaryRewardItems.Count > 0) pool = legendaryRewardItems;
                else if (betterRewardItems.Count > 0) pool = betterRewardItems;
                else pool = normalRewardItems;
            }

            if (pool == null || pool.Count == 0) return null;

            return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        private void RefreshAllUI()
        {
            OnQuestDataChanged?.Invoke();

            if (boardUI != null)
                boardUI.Refresh();

            if (activeQuestHUD != null)
                activeQuestHUD.Refresh();
        }

        public string GetRewardText(SideQuestDefinition quest)
        {
            if (quest == null) return "";

            string itemText = quest.rewardTier switch
            {
                QuestRewardTier.Normal => "losowy przedmiot",
                QuestRewardTier.Better => "losowy lepszy przedmiot",
                QuestRewardTier.Legendary => "losowy legendarny przedmiot",
                _ => "losowy przedmiot"
            };

            List<string> parts = new List<string>();
            parts.Add(itemText);

            if (quest.goldReward > 0)
                parts.Add($"{quest.goldReward} golda");

            if (quest.expReward > 0f)
                parts.Add($"{quest.expReward:0} exp");

            return string.Join(", ", parts);
        }

        public string GetOfferObjectiveText(SideQuestDefinition quest)
        {
            if (quest == null) return "";

            if (quest.kind == SideQuestKind.SurviveTime)
            {
                return "";
            }

            return $"0/{quest.targetAmount}";
        }

        public string GetActiveObjectiveText()
        {
            if (activeQuest == null) return "";

            switch (activeQuest.kind)
            {
                case SideQuestKind.KillEnemies:
                    return $"{totalEnemyKills - startEnemyKills}/{activeQuest.targetAmount}";
                case SideQuestKind.KillBoss:
                    return $"{totalBossKills - startBossKills}/{activeQuest.targetAmount}";
                case SideQuestKind.KillSpiders:
                    return $"{totalSpiderKills - startSpiderKills}/{activeQuest.targetAmount}";
                case SideQuestKind.KillSkeletons:
                    return $"{totalSkeletonKills - startSkeletonKills}/{activeQuest.targetAmount}";
                case SideQuestKind.LevelUps:
                    return $"{totalLevelUps - startLevelUps}/{activeQuest.targetAmount}";
                case SideQuestKind.SurviveTime:
                    return "";
                default:
                    return "";
            }
        }

        public string GetActiveStatusText()
        {
            if (activeQuest == null) return "Brak aktywnego sidequesta.";

            if (activeQuest.kind == SideQuestKind.SurviveTime)
            {
                return "Timer działa w tle.";
            }

            return "Postęp liczony od momentu przyjęcia questa.";
        }
    }
}