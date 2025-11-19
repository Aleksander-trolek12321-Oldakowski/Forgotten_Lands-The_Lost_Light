using System;
using System.Collections.Generic;
using UnityEngine;
using Item;
using Player;

namespace Inventory
{
    [DisallowMultipleComponent]
    public class InventoryUIController : MonoBehaviour
    {
        [Header("References")]
        public InventoryManager inventoryManager;
        public TooltipUI tooltip;

        [Header("Root / Panels")]
        [Tooltip("The root GameObject that represents the inventory UI. This object will be activated/deactivated by ToggleInventory.")]
        public GameObject inventoryRoot;
        public RectTransform equipPanel;
        public RectTransform backpackPanel;

        [Header("Equip slot fallback order")]
        public List<ItemType> equipmentOrderFallback = new List<ItemType>
        {
            ItemType.Helmet, ItemType.Chest, ItemType.Legs, ItemType.Boots, ItemType.Weapon, ItemType.Shield
        };

        [Header("Toggle settings")]
        public KeyCode toggleKey = KeyCode.I;
        public bool startClosed = true;

        [NonSerialized] public List<ItemSlotUI> equipSlotUIs = new List<ItemSlotUI>();
        [NonSerialized] public List<ItemSlotUI> backpackSlotUIs = new List<ItemSlotUI>();

        PlayerBase player;

        private void Awake()
        {
            if (inventoryManager == null)
                inventoryManager = FindObjectOfType<InventoryManager>();

            if (tooltip == null)
                tooltip = FindObjectOfType<TooltipUI>();

            if (inventoryRoot == null)
                inventoryRoot = this.gameObject;

            AutoAssignPanelsIfNeeded();

            player = inventoryManager?.player ?? FindObjectOfType<PlayerBase>();

            AutoCollectSlots();

            if (inventoryManager != null)
                inventoryManager.OnInventoryChanged += RefreshAllSlots;

            RefreshAllSlots();

            if (inventoryRoot != null)
            {
                inventoryRoot.SetActive(!startClosed);
                if (startClosed)
                {
                    if (player != null) player.SetControlsEnabled(true);
                }
            }
        }

        private void OnDestroy()
        {
            if (inventoryManager != null)
                inventoryManager.OnInventoryChanged -= RefreshAllSlots;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                ToggleInventory();
        }

        private void AutoAssignPanelsIfNeeded()
        {
            if (inventoryRoot != null)
            {
                if (equipPanel == null)
                {
                    var t = inventoryRoot.transform.Find("EquipPanel");
                    if (t != null) equipPanel = t as RectTransform;
                }
                if (backpackPanel == null)
                {
                    var t = inventoryRoot.transform.Find("BackpackPanel");
                    if (t != null) backpackPanel = t as RectTransform;
                }
            }

            if (equipPanel == null)
            {
                GameObject go = GameObject.Find("EquipPanel");
                if (go != null) equipPanel = go.GetComponent<RectTransform>();
            }
            if (backpackPanel == null)
            {
                GameObject go = GameObject.Find("BackpackPanel");
                if (go != null) backpackPanel = go.GetComponent<RectTransform>();
            }
        }

        [ContextMenu("Auto Collect Slots")]
        public void AutoCollectSlots()
        {
            equipSlotUIs.Clear();
            backpackSlotUIs.Clear();

            if (equipPanel != null)
            {
                var found = equipPanel.GetComponentsInChildren<ItemSlotUI>(true);
                Array.Sort(found, (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

                for (int i = 0; i < found.Length; i++)
                {
                    var s = found[i];
                    s.isEquipmentSlot = true;

                    ItemType parsedType;
                    if (TryParseItemTypeFromName(s.gameObject.name, out parsedType))
                        s.slotType = parsedType;
                    else if (i < equipmentOrderFallback.Count)
                        s.slotType = equipmentOrderFallback[i];
                    else
                        s.slotType = ItemType.Helmet;

                    if (s.icon == null)
                    {
                        var icon = s.transform.Find("Icon")?.GetComponent<UnityEngine.UI.Image>();
                        if (icon != null) s.icon = icon;
                    }

                    equipSlotUIs.Add(s);
                }

                Debug.Log($"InventoryUIController: Collected {equipSlotUIs.Count} equip slots.");
            }
            else
            {
                Debug.LogWarning("InventoryUIController: equipPanel is null - no equip slots collected.");
            }

            if (backpackPanel != null)
            {
                var found = backpackPanel.GetComponentsInChildren<ItemSlotUI>(true);
                Array.Sort(found, (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

                for (int i = 0; i < found.Length; i++)
                {
                    var s = found[i];
                    s.isEquipmentSlot = false;
                    s.slotIndex = i;

                    if (s.icon == null)
                    {
                        var icon = s.transform.Find("Icon")?.GetComponent<UnityEngine.UI.Image>();
                        if (icon != null) s.icon = icon;
                    }

                    backpackSlotUIs.Add(s);
                }

                Debug.Log($"InventoryUIController: Collected {backpackSlotUIs.Count} backpack slots.");
            }
            else
            {
                Debug.LogWarning("InventoryUIController: backpackPanel is null - no backpack slots collected.");
            }
        }

        [ContextMenu("Refresh All Slots")]
        public void RefreshAllSlots()
        {
            if (inventoryManager == null)
            {
                Debug.LogWarning("InventoryUIController: inventoryManager is null - cannot refresh.");
                return;
            }

            if (backpackSlotUIs != null && inventoryManager.backpackSlots != null)
            {
                for (int i = 0; i < backpackSlotUIs.Count; i++)
                {
                    if (i < inventoryManager.backpackSlots.Count)
                        backpackSlotUIs[i].Refresh(inventoryManager.backpackSlots[i]);
                    else
                        backpackSlotUIs[i].Refresh(new InventoryItem(null, 0));
                }
            }

            if (equipSlotUIs != null && inventoryManager.equipment != null)
            {
                foreach (var slot in equipSlotUIs)
                {
                    if (slot == null) continue;
                    if (inventoryManager.equipment.TryGetValue(slot.slotType, out InventoryItem it))
                        slot.Refresh(it);
                    else
                        slot.Refresh(new InventoryItem(null, 0));
                }
            }
        }

        public void ToggleInventory()
        {
            if (inventoryRoot == null)
            {
                Debug.LogWarning("InventoryUIController: inventoryRoot not assigned - cannot toggle.");
                return;
            }

            bool isOpen = inventoryRoot.activeSelf;
            if (isOpen) CloseInventory(); else OpenInventory();
        }

        public void OpenInventory()
        {
            if (inventoryRoot == null) return;
            inventoryRoot.SetActive(true);

            if (player == null) player = inventoryManager?.player ?? FindObjectOfType<PlayerBase>();
            if (player != null) player.SetControlsEnabled(false);

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            RefreshAllSlots();
        }

        public void CloseInventory()
        {
            if (inventoryRoot == null) return;
            tooltip?.Hide();
            inventoryRoot.SetActive(false);

            if (player == null) player = inventoryManager?.player ?? FindObjectOfType<PlayerBase>();
            if (player != null) player.SetControlsEnabled(true);

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private bool TryParseItemTypeFromName(string goName, out ItemType result)
        {
            result = default;
            if (string.IsNullOrEmpty(goName)) return false;

            string[] parts = goName.Split(new char[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = parts.Length - 1; i >= 0; i--)
            {
                string token = parts[i].Trim();
                if (string.IsNullOrEmpty(token)) continue;
                if (Enum.TryParse<ItemType>(token, true, out result))
                    return true;
            }

            if (Enum.TryParse<ItemType>(goName, true, out result)) return true;
            return false;
        }

    #if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                if (equipPanel == null && inventoryRoot != null)
                {
                    Transform t = inventoryRoot.transform.Find("EquipPanel");
                    if (t != null) equipPanel = t as RectTransform;
                }
                if (backpackPanel == null && inventoryRoot != null)
                {
                    Transform t = inventoryRoot.transform.Find("BackpackPanel");
                    if (t != null) backpackPanel = t as RectTransform;
                }
            }
        }
    #endif
    }
}
