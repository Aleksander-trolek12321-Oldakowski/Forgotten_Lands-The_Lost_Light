using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Item;
using Player;
using Inventory;
using SideQuests;
using chest;

namespace GameSave
{
    public static class SaveService
    {
        public const string HubSceneName = "HUB";
        public const string MenuSceneName = "Menu";

        private static GameSaveData cachedData;
        private static bool cacheLoaded = false;

        private static string SaveDirectory => Path.Combine(Application.persistentDataPath, "ForgottenLands");
        private static string SavePath => Path.Combine(SaveDirectory, "save.json");

        public static string DebugSavePath => SavePath;

        public static bool HasInitializedSave()
        {
            GameSaveData data = LoadFromDisk();
            return data != null && data.isInitialized;
        }

        public static void DeleteSave()
        {
            cachedData = null;
            cacheLoaded = true;

            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                    Debug.Log($"SaveService: Deleted save file at {SavePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"SaveService: Failed to delete save file. {ex.Message}");
            }
        }

        public static string GetContinueSceneOrDefault(string defaultScene = HubSceneName)
        {
            GameSaveData data = LoadFromDisk();
            if (data == null || !data.isInitialized)
                return defaultScene;

            if (string.IsNullOrWhiteSpace(data.continueSceneName))
                return defaultScene;

            return data.continueSceneName;
        }

        public static void CaptureAndSave(string targetSceneName, bool includeHubPosition, bool clearCurrentSceneChestState, bool clearHubPositionWhenNotIncluded)
        {
            GameSaveData data = LoadOrCreate();
            string activeSceneName = SceneManager.GetActiveScene().name;

            CapturePlayerState(data);
            CaptureInventoryState(data);
            CaptureSideQuestState(data);

            if (clearCurrentSceneChestState)
            {
                RemoveSceneChestStates(data, activeSceneName);
            }
            else
            {
                CaptureChestStateForScene(data, activeSceneName);
            }

            data.continueSceneName = string.IsNullOrWhiteSpace(targetSceneName) ? HubSceneName : targetSceneName;
            data.isInitialized = true;

            if (includeHubPosition && string.Equals(activeSceneName, HubSceneName, StringComparison.OrdinalIgnoreCase))
            {
                PlayerBase player = UnityEngine.Object.FindObjectOfType<PlayerBase>();
                if (player != null)
                {
                    data.hasHubPosition = true;
                    data.hubPosition = ToSavedVector3(player.transform.position);
                    data.hubRotation = ToSavedQuaternion(player.transform.rotation);
                }
            }
            else if (clearHubPositionWhenNotIncluded)
            {
                data.hasHubPosition = false;
                data.hubPosition = new SavedVector3();
                data.hubRotation = new SavedQuaternion();
            }

            WriteToDisk(data);
            Debug.Log($"SaveService: Saved game to {SavePath}");
        }

        public static void ApplyToActiveScene()
        {
            GameSaveData data = LoadFromDisk();
            if (data == null || !data.isInitialized)
                return;

            string sceneName = SceneManager.GetActiveScene().name;
            if (string.Equals(sceneName, MenuSceneName, StringComparison.OrdinalIgnoreCase))
                return;

            PlayerBase player = UnityEngine.Object.FindObjectOfType<PlayerBase>();
            InventoryManager inventory = UnityEngine.Object.FindObjectOfType<InventoryManager>();
            SideQuestManager sideQuest = SideQuestManager.Instance ?? UnityEngine.Object.FindObjectOfType<SideQuestManager>();

            if (player != null && data.player != null)
                player.ApplySaveSnapshot(data.player);

            if (inventory != null)
                ApplyInventoryState(inventory, data);

            if (sideQuest != null && data.sideQuest != null)
            {
                if (player != null) sideQuest.player = player;
                if (inventory != null) sideQuest.inventoryManager = inventory;
                sideQuest.ApplySaveSnapshot(data.sideQuest);
            }

            ApplyChestStateForScene(sceneName, data);

            if (player != null && data.hasHubPosition && string.Equals(sceneName, HubSceneName, StringComparison.OrdinalIgnoreCase))
            {
                player.transform.position = ToVector3(data.hubPosition);
                player.transform.rotation = ToQuaternion(data.hubRotation);
            }

            inventory?.NotifyInventoryChanged();
        }

        private static void CapturePlayerState(GameSaveData data)
        {
            PlayerBase player = UnityEngine.Object.FindObjectOfType<PlayerBase>();
            if (player == null) return;
            data.player = player.CreateSaveSnapshot();
        }

        private static void CaptureInventoryState(GameSaveData data)
        {
            InventoryManager inventory = UnityEngine.Object.FindObjectOfType<InventoryManager>();
            if (inventory == null) return;

            data.backpackSlots = new List<SavedItemStack>();
            if (inventory.backpackSlots != null)
            {
                for (int i = 0; i < inventory.backpackSlots.Count; i++)
                {
                    data.backpackSlots.Add(ToSavedItemStack(inventory.backpackSlots[i]));
                }
            }

            data.equipmentSlots = new List<SavedEquipmentSlot>();
            if (inventory.equipment != null)
            {
                foreach (var kvp in inventory.equipment)
                {
                    SavedEquipmentSlot slot = new SavedEquipmentSlot
                    {
                        slotType = kvp.Key.ToString(),
                        item = ToSavedItemStack(kvp.Value)
                    };
                    data.equipmentSlots.Add(slot);
                }
            }
        }

        private static void ApplyInventoryState(InventoryManager inventory, GameSaveData data)
        {
            if (inventory == null || data == null) return;

            int backpackCount = Mathf.Max(inventory.backpackSize, data.backpackSlots != null ? data.backpackSlots.Count : 0);
            if (backpackCount <= 0) backpackCount = Mathf.Max(1, inventory.backpackSize);

            inventory.backpackSize = backpackCount;
            inventory.backpackSlots = new List<InventoryItem>(backpackCount);
            for (int i = 0; i < backpackCount; i++)
            {
                SavedItemStack saved = (data.backpackSlots != null && i < data.backpackSlots.Count) ? data.backpackSlots[i] : null;
                inventory.backpackSlots.Add(ToInventoryItem(saved));
            }

            inventory.equipment = new Dictionary<ItemType, InventoryItem>();
            if (inventory.allowedEquipmentOrder != null)
            {
                for (int i = 0; i < inventory.allowedEquipmentOrder.Count; i++)
                {
                    ItemType slotType = inventory.allowedEquipmentOrder[i];
                    inventory.equipment[slotType] = new InventoryItem(null, 0);
                }
            }

            if (data.equipmentSlots != null)
            {
                for (int i = 0; i < data.equipmentSlots.Count; i++)
                {
                    SavedEquipmentSlot slot = data.equipmentSlots[i];
                    if (slot == null) continue;

                    if (!Enum.TryParse(slot.slotType, true, out ItemType slotType))
                        continue;

                    inventory.equipment[slotType] = ToInventoryItem(slot.item);
                }
            }
        }

        private static void CaptureSideQuestState(GameSaveData data)
        {
            SideQuestManager sideQuest = SideQuestManager.Instance ?? UnityEngine.Object.FindObjectOfType<SideQuestManager>();
            if (sideQuest == null) return;
            data.sideQuest = sideQuest.CreateSaveSnapshot();
        }

        private static void CaptureChestStateForScene(GameSaveData data, string sceneName)
        {
            RemoveSceneChestStates(data, sceneName);

            Chest[] chests = UnityEngine.Object.FindObjectsOfType<Chest>();
            if (chests == null || chests.Length == 0)
                return;

            for (int i = 0; i < chests.Length; i++)
            {
                Chest chest = chests[i];
                if (chest == null) continue;

                SavedChestState savedChest = new SavedChestState
                {
                    sceneName = sceneName,
                    chestKey = BuildChestKey(chest),
                    slots = new List<SavedItemStack>()
                };

                if (chest.chestSlots != null)
                {
                    for (int slotIndex = 0; slotIndex < chest.chestSlots.Count; slotIndex++)
                    {
                        savedChest.slots.Add(ToSavedItemStack(chest.chestSlots[slotIndex]));
                    }
                }

                data.chestStates.Add(savedChest);
            }
        }

        private static void ApplyChestStateForScene(string sceneName, GameSaveData data)
        {
            if (data.chestStates == null) return;

            Chest[] chests = UnityEngine.Object.FindObjectsOfType<Chest>();
            if (chests == null || chests.Length == 0)
                return;

            Dictionary<string, SavedChestState> map = new Dictionary<string, SavedChestState>();
            for (int i = 0; i < data.chestStates.Count; i++)
            {
                SavedChestState saved = data.chestStates[i];
                if (saved == null) continue;
                if (!string.Equals(saved.sceneName, sceneName, StringComparison.Ordinal)) continue;
                if (string.IsNullOrWhiteSpace(saved.chestKey)) continue;
                map[saved.chestKey] = saved;
            }

            for (int i = 0; i < chests.Length; i++)
            {
                Chest chest = chests[i];
                if (chest == null) continue;

                string key = BuildChestKey(chest);
                if (!map.TryGetValue(key, out SavedChestState savedState))
                    continue;

                int slotCount = Mathf.Max(chest.chestSize, savedState.slots != null ? savedState.slots.Count : 0);
                chest.chestSlots = new List<InventoryItem>(slotCount);
                for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
                {
                    SavedItemStack saved = (savedState.slots != null && slotIndex < savedState.slots.Count) ? savedState.slots[slotIndex] : null;
                    chest.chestSlots.Add(ToInventoryItem(saved));
                }
            }
        }

        private static void RemoveSceneChestStates(GameSaveData data, string sceneName)
        {
            if (data.chestStates == null)
                data.chestStates = new List<SavedChestState>();

            data.chestStates.RemoveAll(x => x != null && string.Equals(x.sceneName, sceneName, StringComparison.Ordinal));
        }

        private static SavedItemStack ToSavedItemStack(InventoryItem item)
        {
            SavedItemStack saved = new SavedItemStack();
            if (item == null || item.IsEmpty || item.data == null)
            {
                saved.itemId = "";
                saved.quantity = 0;
                return saved;
            }

            saved.itemId = item.data.itemName;
            saved.quantity = Mathf.Max(1, item.quantity);
            return saved;
        }

        private static InventoryItem ToInventoryItem(SavedItemStack saved)
        {
            if (saved == null || string.IsNullOrWhiteSpace(saved.itemId) || saved.quantity <= 0)
                return new InventoryItem(null, 0);

            ItemData data = ResolveItem(saved.itemId);
            if (data == null)
                return new InventoryItem(null, 0);

            return new InventoryItem(data, Mathf.Max(1, saved.quantity));
        }

        private static ItemData ResolveItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;

            ItemData[] allItems = Resources.FindObjectsOfTypeAll<ItemData>();
            for (int i = 0; i < allItems.Length; i++)
            {
                ItemData it = allItems[i];
                if (it == null) continue;
                if (string.Equals(it.itemName, itemId, StringComparison.Ordinal))
                    return it;
            }

            return null;
        }

        private static string BuildChestKey(Chest chest)
        {
            return BuildTransformPath(chest.transform);
        }

        private static string BuildTransformPath(Transform target)
        {
            if (target == null) return "";

            StringBuilder sb = new StringBuilder();
            Transform current = target;
            while (current != null)
            {
                if (sb.Length == 0)
                    sb.Insert(0, current.name);
                else
                    sb.Insert(0, current.name + "/");
                current = current.parent;
            }
            return sb.ToString();
        }

        private static SavedVector3 ToSavedVector3(Vector3 value)
        {
            return new SavedVector3 { x = value.x, y = value.y, z = value.z };
        }

        private static SavedQuaternion ToSavedQuaternion(Quaternion value)
        {
            return new SavedQuaternion { x = value.x, y = value.y, z = value.z, w = value.w };
        }

        private static Vector3 ToVector3(SavedVector3 value)
        {
            if (value == null) return Vector3.zero;
            return new Vector3(value.x, value.y, value.z);
        }

        private static Quaternion ToQuaternion(SavedQuaternion value)
        {
            if (value == null) return Quaternion.identity;
            return new Quaternion(value.x, value.y, value.z, value.w);
        }

        private static GameSaveData LoadOrCreate()
        {
            GameSaveData loaded = LoadFromDisk();
            if (loaded != null) return loaded;

            cachedData = new GameSaveData();
            cacheLoaded = true;
            return cachedData;
        }

        private static GameSaveData LoadFromDisk()
        {
            if (cacheLoaded)
                return cachedData;

            cacheLoaded = true;

            if (!File.Exists(SavePath))
            {
                cachedData = null;
                return null;
            }

            try
            {
                string json = File.ReadAllText(SavePath, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json))
                {
                    cachedData = null;
                    return null;
                }

                cachedData = JsonUtility.FromJson<GameSaveData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"SaveService: Failed to read save file. {ex.Message}");
                cachedData = null;
            }

            return cachedData;
        }

        private static void WriteToDisk(GameSaveData data)
        {
            if (data == null) return;

            try
            {
                Directory.CreateDirectory(SaveDirectory);
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SavePath, json, Encoding.UTF8);
                cachedData = data;
                cacheLoaded = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"SaveService: Failed to write save file. {ex.Message}");
            }
        }
    }
}
