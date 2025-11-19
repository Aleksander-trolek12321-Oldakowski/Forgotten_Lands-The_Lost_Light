using System;
using System.Collections.Generic;
using UnityEngine;
using Item;
using Player;

namespace Inventory
{
    public class InventoryManager : MonoBehaviour
    {
        [Header("Player reference (for stat changes & spawn position)")]
        public PlayerBase player;

        [Header("Backpack settings")]
        public int backpackSize = 20;
        public List<InventoryItem> backpackSlots;

        [Header("Equipment slots - define which ItemTypes are equippable")]
        public List<ItemType> allowedEquipmentOrder = new List<ItemType>
        {
            ItemType.Helmet, ItemType.Chest, ItemType.Legs, ItemType.Boots, ItemType.Weapon, ItemType.Shield, ItemType.Ring, ItemType.Consumable
        };
        public Dictionary<ItemType, InventoryItem> equipment = new Dictionary<ItemType, InventoryItem>();

        [Header("Drop / pickup settings")]
        [Tooltip("Prefab of LootBag. Prefab must have public List<ItemData> lootTable (as in your LootBag script).")]
        public GameObject lootBagPrefab;

        public event Action OnInventoryChanged;

        private void Awake()
        {
            if (player == null)
                player = GetComponent<PlayerBase>() ?? FindObjectOfType<PlayerBase>();

            backpackSlots = new List<InventoryItem>(backpackSize);
            for (int i = 0; i < backpackSize; i++) backpackSlots.Add(new InventoryItem(null, 0));

            equipment = new Dictionary<ItemType, InventoryItem>();
            foreach (var t in allowedEquipmentOrder) equipment[t] = new InventoryItem(null, 0);
        }

        public void NotifyInventoryChanged()
        {
            OnInventoryChanged?.Invoke();
        }

        public bool TryAddToBackpack(ItemData data, int qty = 1)
        {
            if (data == null) return false;

            int remaining = qty;

            if (data.itemType == ItemType.Consumable && player != null)
            {
                int freeOnPlayer = Mathf.Max(0, player.MaxStack - player.currentStack);
                int toPlayer = Mathf.Min(remaining, freeOnPlayer);
                if (toPlayer > 0)
                {
                    player.currentStack += toPlayer;
                    remaining -= toPlayer;
                    Debug.Log($"Inventory: added {toPlayer} x '{data.itemName}' directly to player stack ({player.currentStack}/{player.MaxStack}).");
                    NotifyInventoryChanged();
                }
            }

            if (remaining <= 0) return true;

            if (data.itemType == ItemType.Consumable && data.stackSize > 1)
            {
                for (int i = 0; i < backpackSlots.Count && remaining > 0; i++)
                {
                    var s = backpackSlots[i];
                    if (!s.IsEmpty && s.data == data && s.quantity < data.stackSize)
                    {
                        int space = data.stackSize - s.quantity;
                        int toAdd = Mathf.Min(space, remaining);
                        s.quantity += toAdd;
                        remaining -= toAdd;
                        Debug.Log($"Inventory: stacked {toAdd} x '{data.itemName}' into backpack slot {i} (now {s.quantity}/{data.stackSize}).");
                    }
                }
            }

            for (int i = 0; i < backpackSlots.Count && remaining > 0; i++)
            {
                if (backpackSlots[i].IsEmpty)
                {
                    int take = remaining;
                    if (data.itemType == ItemType.Consumable && data.stackSize > 1)
                    {
                        take = Mathf.Min(remaining, data.stackSize);
                        backpackSlots[i] = new InventoryItem(data, take);
                    }
                    else
                    {
                        backpackSlots[i] = new InventoryItem(data, 1);
                        take = 1;
                    }
                    remaining -= take;
                    Debug.Log($"Inventory: placed {take} x '{data.itemName}' into backpack slot {i}.");
                }
            }

            NotifyInventoryChanged();

            if (remaining > 0)
            {
                Debug.LogWarning($"Inventory: not enough space; {remaining} x '{data.itemName}' could not be added.");
                return false;
            }
            return true;
        }

        public bool HasFreeBackpackSlot()
        {
            foreach (var s in backpackSlots) if (s.IsEmpty) return true;
            return false;
        }

        public bool IsBackpackFull()
        {
            foreach (var s in backpackSlots)
            {
                if (s.IsEmpty) return false;
            }
            return true;
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
                if (currentlyEquipped.data.itemType != ItemType.Consumable)
                    ApplyStatsToPlayer(currentlyEquipped.data, remove: true);
            }

            equipment[type] = invItem;

            if (invItem.data.itemType != ItemType.Consumable)
                ApplyStatsToPlayer(invItem.data, remove: false);

            backpackSlots[slotIndex] = new InventoryItem(null, 0);
            NotifyInventoryChanged();
            Debug.Log($"Inventory: Equipped {invItem.data.itemName} to slot {type}");
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

            if (eq.data.itemType != ItemType.Consumable)
                ApplyStatsToPlayer(eq.data, remove: true);

            equipment[type] = new InventoryItem(null, 0);
            NotifyInventoryChanged();
            Debug.Log($"Inventory: Unequipped {eq.data.itemName} from {type}");
            return true;
        }

        public void SwapBackpackSlots(int indexA, int indexB)
        {
            if (indexA < 0 || indexA >= backpackSlots.Count || indexB < 0 || indexB >= backpackSlots.Count) return;
            var tmp = backpackSlots[indexA];
            backpackSlots[indexA] = backpackSlots[indexB];
            backpackSlots[indexB] = tmp;
            NotifyInventoryChanged();
            Debug.Log($"Inventory: swapped backpack slots {indexA} <-> {indexB}");
        }

        public bool UseConsumableSlot(ItemType consumableSlotType)
        {
            if (!equipment.ContainsKey(consumableSlotType)) return false;
            var it = equipment[consumableSlotType];
            if (it.IsEmpty) return false;
            if (it.data == null) return false;

            if (player != null)
            {
                if (it.data.HP > 0) player.Heal(it.data.HP);
                if (it.data.Mana > 0) player.Restore(it.data.Mana);

                equipment[consumableSlotType] = new InventoryItem(null, 0);
                NotifyInventoryChanged();
                Debug.Log($"Inventory: used consumable {it.data.itemName} from equipment slot {consumableSlotType}");
                return true;
            }
            return false;
        }

        public bool DropFromBackpack(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= backpackSlots.Count) return false;
            var it = backpackSlots[slotIndex];
            if (it.IsEmpty) return false;

            if (lootBagPrefab != null && player != null)
            {
                Vector3 spawnPos = player.transform.position + player.transform.forward * 1.2f + Vector3.up * 0.2f;
                GameObject bag = Instantiate(lootBagPrefab, spawnPos, Quaternion.identity);

                var lootComp = bag.GetComponent<Item.LootBag>();
                if (lootComp != null)
                {
                    lootComp.lootTable = new List<ItemData> { it.data };
                    Debug.Log($"Inventory: spawned LootBag with '{it.data.itemName}'");
                }
                else
                {
                    Debug.LogWarning("Inventory: LootBag component not found on prefab or has different script type. Please ensure prefab uses Item.LootBag.");
                }
            }
            else
            {
                Debug.LogWarning("Inventory: lootBagPrefab or player reference missing - cannot spawn loot bag on drop.");
                return false;
            }

            backpackSlots[slotIndex] = new InventoryItem(null, 0);
            NotifyInventoryChanged();
            Debug.Log($"Inventory: dropped item {it.data.itemName} from slot {slotIndex}");
            return true;
        }

        void ApplyStatsToPlayer(ItemData data, bool remove)
        {
            if (data == null || player == null) return;
            float sign = remove ? -1f : 1f;
            player.ModifyStats(data.HP * sign, data.Mana * sign, data.Damage * sign, data.Defense * sign, data.Speed * sign);
        }

        public bool DropEquipmentSlot(ItemType type)
        {
            if (!equipment.ContainsKey(type)) return false;
            var eq = equipment[type];
            if (eq.IsEmpty) return false;

            if (lootBagPrefab != null && player != null)
            {
                Vector3 spawnPos = player.transform.position + player.transform.forward * 1.2f + Vector3.up * 0.2f;
                GameObject bag = Instantiate(lootBagPrefab, spawnPos, Quaternion.identity);

                var lootComp = bag.GetComponent<Item.LootBag>();
                if (lootComp != null)
                {
                    lootComp.lootTable = new List<ItemData> { eq.data };
                    Debug.Log($"Inventory: spawned LootBag (from equipment) with '{eq.data.itemName}'");
                }
                else
                {
                    Debug.LogWarning("Inventory: LootBag component not found on prefab. Cannot set item.");
                }
            }
            else
            {
                Debug.LogWarning("Inventory: lootBagPrefab or player reference missing - cannot spawn loot bag on drop.");
                return false;
            }

            if (eq.data.itemType != ItemType.Consumable)
                ApplyStatsToPlayer(eq.data, remove: true);

            equipment[type] = new InventoryItem(null, 0);
            NotifyInventoryChanged();
            Debug.Log($"Inventory: dropped equipment {eq.data.itemName} from slot {type}");
            return true;
        }

    }
}