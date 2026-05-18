using System;
using System.Collections.Generic;

namespace GameSave
{
    [Serializable]
    public class SavedItemStack
    {
        public string itemId = "";
        public int quantity = 0;
    }

    [Serializable]
    public class SavedEquipmentSlot
    {
        public string slotType = "";
        public SavedItemStack item = new SavedItemStack();
    }

    [Serializable]
    public class SavedChestState
    {
        public string sceneName = "";
        public string chestKey = "";
        public List<SavedItemStack> slots = new List<SavedItemStack>();
    }

    [Serializable]
    public class SavedPlayerStats
    {
        public float maxHp = 10f;
        public float maxMp = 5f;
        public float strength = 1f;
        public float defense = 1f;
        public float damageMultiplier = 1f;
        public float percentDmgTaken = 1f;
        public float currentHp = 10f;
        public float currentMp = 5f;
        public float hpRestorePercentage = 0.2f;
        public float mpRestorePercentage = 0.5f;
        public float potionCooldown = 3f;
        public int currentStack = 0;
        public int maxStack = 10;
        public int level = 1;
        public float currentExp = 0f;
        public float expToNextLevel = 100f;
        public int skillPoints = 0;
        public float speed = 3f;
        public float money = 0f;
    }

    [Serializable]
    public class SavedSkillState
    {
        public string skillId = "";
        public bool unlocked = false;
    }

    [Serializable]
    public class SavedSkillTreeState
    {
        public int skillPoints = 0;
        public List<SavedSkillState> skills = new List<SavedSkillState>();
    }

    [Serializable]
    public class SavedSideQuestState
    {
        public bool hasActiveQuest = false;
        public int activeQuestIndex = -1;
        public float activeQuestElapsedSeconds = 0f;
        public int startEnemyKills = 0;
        public int startBossKills = 0;
        public int startSpiderKills = 0;
        public int startSkeletonKills = 0;
        public int startLevelUps = 0;
        public int totalEnemyKills = 0;
        public int totalBossKills = 0;
        public int totalSpiderKills = 0;
        public int totalSkeletonKills = 0;
        public int totalLevelUps = 0;
        public List<int> offerQuestIndices = new List<int>();
    }

    [Serializable]
    public class SavedVector3
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public class SavedQuaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;
    }

    [Serializable]
    public class GameSaveData
    {
        public int version = 1;
        public bool isInitialized = false;
        public string continueSceneName = "HUB";
        public bool hasHubPosition = false;
        public SavedVector3 hubPosition = new SavedVector3();
        public SavedQuaternion hubRotation = new SavedQuaternion();
        public SavedPlayerStats player = new SavedPlayerStats();
        public List<SavedItemStack> backpackSlots = new List<SavedItemStack>();
        public List<SavedEquipmentSlot> equipmentSlots = new List<SavedEquipmentSlot>();
        public List<SavedChestState> chestStates = new List<SavedChestState>();
        public SavedSkillTreeState skillTree = new SavedSkillTreeState();
        public SavedSideQuestState sideQuest = new SavedSideQuestState();
    }
}
