using System;
using System.Collections.Generic;
using UnityEngine;
using Player;

namespace Item
{
    public class InventoryManager : MonoBehaviour
    {
        [Header("Player reference (for stat changes)")]
        public PlayerBase player;

        [Header("Backpack settings")]
        public int backpackSize = 20;
        public List<InventoryItem> backpackSlots;

        [Header("Equipment slots")]
        public List<ItemType> allowedEquipmentOrder = new List<ItemType> {
        ItemType.Helmet, ItemType.Chest, ItemType.Legs, ItemType.Boots, ItemType.Weapon, ItemType.Shield
    };
        public Dictionary<ItemType, InventoryItem> equipment = new Dictionary<ItemType, InventoryItem>();

        [Header("Drop settings")]
        public GameObject lootBagPrefab;

        public event Action OnInventoryChanged;

        private void Awake()
        {
            if (player == null)
                player = GetComponent<PlayerBase>();

            backpackSlots = new List<InventoryItem>(backpackSize);
            for (int i = 0; i < backpackSize; i++) backpackSlots.Add(new InventoryItem(null, 0));

            equipment = new Dictionary<ItemType, InventoryItem>();
            foreach (var t in allowedEquipmentOrder)
                equipment[t] = new InventoryItem(null, 0);
        }

        public bool TryAddToBackpack(ItemData data)
        {
            if (data == null) return false;
            for (int i = 0; i < backpackSlots.Count; i++)
            {
                if (backpackSlots[i].IsEmpty)
                {
                    backpackSlots[i] = new InventoryItem(data, 1);
                    OnInventoryChanged?.Invoke();
                    Debug.Log($"Inventory: added {data.itemName} to backpack slot {i}");
                    return true;
                }
            }
            Debug.Log("Inventory: backpack full! Cannot add item.");
            return false;
        }

        public bool HasFreeBackpackSlot()
        {
            foreach (var s in backpackSlots) if (s.IsEmpty) return true;
            return false;
        }

        public bool IsBackpackFull()
        {
            return !HasFreeBackpackSlot();
        }

        public bool EquipFromBackpack(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= backpackSlots.Count) return false;
            var invItem = backpackSlots[slotIndex];
            if (invItem.IsEmpty) return false;

            ItemType type = invItem.data.itemType;
            if (!equipment.ContainsKey(type))
            {
                Debug.Log($"Inventory: no equipment slot for type {type}");
                return false;
            }

            var currentlyEquipped = equipment[type];
            if (!currentlyEquipped.IsEmpty)
            {
                if (!HasFreeBackpackSlot())
                {
                    Debug.Log("Inventory: cannot unequip - backpack full");
                    return false;
                }
                for (int i = 0; i < backpackSlots.Count; i++)
                {
                    if (backpackSlots[i].IsEmpty)
                    {
                        backpackSlots[i] = currentlyEquipped;
                        break;
                    }
                }
                ApplyStatsToPlayer(currentlyEquipped.data, remove: true);
            }

            equipment[type] = invItem;
            ApplyStatsToPlayer(invItem.data, remove: false);

            backpackSlots[slotIndex] = new InventoryItem(null, 0);
            OnInventoryChanged?.Invoke();
            Debug.Log($"Inventory: equipped {invItem.data.itemName} to {type}");
            return true;
        }

        public bool Unequip(ItemType type)
        {
            if (!equipment.ContainsKey(type)) return false;
            var eq = equipment[type];
            if (eq.IsEmpty) return false;

            if (!HasFreeBackpackSlot())
            {
                Debug.Log("Inventory: cannot unequip - backpack full");
                return false;
            }

            for (int i = 0; i < backpackSlots.Count; i++)
            {
                if (backpackSlots[i].IsEmpty)
                {
                    backpackSlots[i] = eq;
                    break;
                }
            }
            ApplyStatsToPlayer(eq.data, remove: true);
            equipment[type] = new InventoryItem(null, 0);
            OnInventoryChanged?.Invoke();
            Debug.Log($"Inventory: unequipped {eq.data.itemName} from {type}");
            return true;
        }

        public void SwapBackpackSlots(int indexA, int indexB)
        {
            if (indexA < 0 || indexA >= backpackSlots.Count || indexB < 0 || indexB >= backpackSlots.Count) return;
            var tmp = backpackSlots[indexA];
            backpackSlots[indexA] = backpackSlots[indexB];
            backpackSlots[indexB] = tmp;
            OnInventoryChanged?.Invoke();
        }

        public bool DropFromBackpack(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= backpackSlots.Count) return false;
            var it = backpackSlots[slotIndex];
            if (it.IsEmpty) return false;

            if (lootBagPrefab != null && player != null)
            {
                GameObject bag = Instantiate(lootBagPrefab, player.transform.position + player.transform.forward * 1.2f + Vector3.up * 0.2f, Quaternion.identity);

                var lb = bag.GetComponent<MonoBehaviour>();
                var lootComp = bag.GetComponent("LootBag") ?? bag.GetComponent("LootBagDebug");

                if (lootComp != null)
                {
                    var type = lootComp.GetType();
                    var field = type.GetField("lootTable");
                    if (field != null)
                    {
                        var itemField = type.GetField("itemData");
                        if (itemField != null)
                        {
                            itemField.SetValue(lootComp, it.data);
                        }
                        else
                        {
                            var lt = type.GetField("lootTable");
                            if (lt != null)
                            {
                                var list = lt.GetValue(lootComp) as System.Collections.IList;
                                if (list != null)
                                {
                                    list.Clear();
                                    list.Add(it.data);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("Inventory: lootBagPrefab or player reference missing, cannot spawn loot bag on drop.");
            }

            backpackSlots[slotIndex] = new InventoryItem(null, 0);
            OnInventoryChanged?.Invoke();
            Debug.Log($"Inventory: dropped item {it.data.itemName} from slot {slotIndex}");
            return true;
        }

        void ApplyStatsToPlayer(ItemData data, bool remove)
        {
            if (data == null || player == null) return;
            float sign = remove ? -1f : 1f;
            player.ModifyStats(data.HP * sign, data.Mana * sign, data.Damage * sign, data.Defense * sign, data.Speed * sign);
        }
    }
}