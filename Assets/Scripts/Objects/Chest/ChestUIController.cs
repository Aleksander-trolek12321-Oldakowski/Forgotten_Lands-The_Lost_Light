using System;
using System.Collections.Generic;
using UnityEngine;
using Player;
using Item;
using Inventory;

namespace chest
{
    public class ChestUIController : MonoBehaviour
    {
        [Header("Manual UI references (assign in Editor)")]
        [Tooltip("Root GameObject of the chest UI (panel). This is toggled open/close.")]
        public GameObject inventoryRoot;

        [Tooltip("Panel (parent) that contains ChestSlotUI elements as children.")]
        public RectTransform chestGridPanel;

        [Tooltip("Optional tooltip used by the slots.")]
        public TooltipUI tooltip;

        [Header("Optional references")]
        [Tooltip("Reference to player's InventoryUIController. If not assigned, will try to FindObjectOfType at runtime.")]
        public InventoryUIController inventoryController;

        [HideInInspector] public Chest currentChest;
        public bool IsOpen => inventoryRoot != null && inventoryRoot.activeSelf;

        private PlayerBase interactingPlayer;
        private InventoryManager playerInventoryManager;
        private List<ChestSlotUI> chestSlotUIs = new List<ChestSlotUI>();

        private void Awake()
        {
            if (tooltip == null) tooltip = FindObjectOfType<TooltipUI>();

            if (inventoryController == null)
            {
                inventoryController = FindObjectOfType<InventoryUIController>();
                if (inventoryController != null)
                    Debug.Log("ChestUIController: Auto-found InventoryUIController.");
            }
        }

        public void Open(Chest chest, PlayerBase player)
        {
            if (IsOpen && currentChest == chest)
                return;

            if (inventoryRoot == null)
            {
                Debug.LogWarning("ChestUIController: inventoryRoot is not assigned. Assign your UI root in inspector.");
                return;
            }

            InputBlocker.Block(player);

            currentChest = chest;
            interactingPlayer = player;
            playerInventoryManager = FindObjectOfType<InventoryManager>();

            if (inventoryController == null)
            {
                inventoryController = FindObjectOfType<InventoryUIController>();
            }
            if (inventoryController != null)
            {
                inventoryController.OpenInventory(false);
            }

            inventoryRoot.SetActive(true);

            AutoCollectSlots();

            RefreshAllSlots();

            if (interactingPlayer != null) interactingPlayer.SetControlsEnabled(false);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
        }

        public void Close()
        {
            if (!IsOpen && currentChest == null && interactingPlayer == null)
                return;

            if (inventoryRoot != null) inventoryRoot.SetActive(false);

            if (interactingPlayer != null) interactingPlayer.SetControlsEnabled(true);

            if (inventoryController == null)
            {
                inventoryController = FindObjectOfType<InventoryUIController>();
            }
            if (inventoryController != null)
            {
                inventoryController.CloseInventory();
            }

            InputBlocker.Restore(interactingPlayer);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            currentChest = null;
            interactingPlayer = null;
            playerInventoryManager = null;
        }

        [ContextMenu("Auto Collect Slots")]
        public void AutoCollectSlots()
        {
            chestSlotUIs.Clear();

            if (chestGridPanel == null)
            {
                Debug.LogWarning("ChestUIController: chestGridPanel is null - cannot collect slots.");
                return;
            }

            var found = chestGridPanel.GetComponentsInChildren<ChestSlotUI>(true);
            Array.Sort(found, (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

            for (int i = 0; i < found.Length; i++)
            {
                var s = found[i];
                s.Initialize(this, i, tooltip);
                chestSlotUIs.Add(s);
            }

            Debug.Log($"ChestUIController: Collected {chestSlotUIs.Count} chest slots.");
            if (currentChest != null && currentChest.chestSlots.Count != chestSlotUIs.Count)
            {
                Debug.LogWarning($"ChestUIController: chest storage size ({currentChest.chestSlots.Count}) != number of slot UIs ({chestSlotUIs.Count}). Make them match.");
            }
        }

        public void RefreshAllSlots()
        {
            if (currentChest == null)
            {
                Debug.LogWarning("ChestUIController: currentChest is null - nothing to refresh.");
                return;
            }

            Debug.Log($"ChestUIController: RefreshAllSlots() - chest '{currentChest.name}' slotCount={currentChest.chestSlots.Count}, uiSlots={chestSlotUIs.Count}");

            for (int i = 0; i < chestSlotUIs.Count; i++)
            {
                InventoryItem item = (i < currentChest.chestSlots.Count) ? currentChest.chestSlots[i] : null;
                chestSlotUIs[i].Refresh(item);

                string itemName = (item != null && !item.IsEmpty && item.data != null) ? item.data.name : "<empty>";
                Debug.Log($"  slot[{i}] -> item = {itemName}  (slotGameObject = {chestSlotUIs[i].gameObject.name})");
            }

            Canvas.ForceUpdateCanvases();
            if (chestGridPanel != null)
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(chestGridPanel);
        }

        public bool TransferFromPlayerToChest(int playerBackpackIndex, int chestDestIndex)
        {
            if (playerInventoryManager == null) playerInventoryManager = FindObjectOfType<InventoryManager>();
            if (playerInventoryManager == null)
            {
                Debug.LogWarning("ChestUIController: playerInventoryManager not found.");
                return false;
            }

            if (playerBackpackIndex < 0 || playerBackpackIndex >= playerInventoryManager.backpackSlots.Count) return false;
            var item = playerInventoryManager.backpackSlots[playerBackpackIndex];
            if (item == null || item.IsEmpty) return false;

            if (currentChest == null) return false;

            if (currentChest.chestSlots[chestDestIndex] == null || currentChest.chestSlots[chestDestIndex].IsEmpty)
            {
                currentChest.chestSlots[chestDestIndex] = item;
                playerInventoryManager.backpackSlots[playerBackpackIndex] = new InventoryItem(null, 0);

                Debug.Log($"ChestUIController: Moved '{item.data?.name}' from player slot {playerBackpackIndex} -> chest slot {chestDestIndex}");
                RefreshAllSlots();
                playerInventoryManager.NotifyInventoryChanged();
                return true;
            }
            else
            {
                InventoryItem temp = currentChest.chestSlots[chestDestIndex];
                currentChest.chestSlots[chestDestIndex] = item;
                playerInventoryManager.backpackSlots[playerBackpackIndex] = temp;

                Debug.Log($"ChestUIController: Swapped player slot {playerBackpackIndex} with chest slot {chestDestIndex}");
                RefreshAllSlots();
                playerInventoryManager.NotifyInventoryChanged();
                return true;
            }
        }

        public bool TransferFromChestToPlayer(int chestIndex, int playerDestIndex = -1)
        {
            if (playerInventoryManager == null) playerInventoryManager = FindObjectOfType<InventoryManager>();
            if (playerInventoryManager == null)
            {
                Debug.LogWarning("ChestUIController: playerInventoryManager not found.");
                return false;
            }

            if (currentChest == null) return false;

            var item = currentChest.chestSlots[chestIndex];
            if (item == null || item.IsEmpty) return false;

            if (playerDestIndex >= 0 && playerDestIndex < playerInventoryManager.backpackSlots.Count)
            {
                var target = playerInventoryManager.backpackSlots[playerDestIndex];
                playerInventoryManager.backpackSlots[playerDestIndex] = item;
                currentChest.chestSlots[chestIndex] = (target == null ? new InventoryItem(null, 0) : target);

                Debug.Log($"ChestUIController: Moved chest slot {chestIndex} -> player slot {playerDestIndex}");
                RefreshAllSlots();
                playerInventoryManager.NotifyInventoryChanged();
                return true;
            }

            for (int i = 0; i < playerInventoryManager.backpackSlots.Count; i++)
            {
                if (playerInventoryManager.backpackSlots[i] == null || playerInventoryManager.backpackSlots[i].IsEmpty)
                {
                    playerInventoryManager.backpackSlots[i] = item;
                    currentChest.chestSlots[chestIndex] = new InventoryItem(null, 0);

                    Debug.Log($"ChestUIController: Moved chest slot {chestIndex} -> player slot {i}");
                    RefreshAllSlots();
                    playerInventoryManager.NotifyInventoryChanged();
                    return true;
                }
            }

            Debug.Log("ChestUIController: Player backpack is full - cannot move item from chest.");
            return false;
        }

        private void Update()
        {
            if (inventoryRoot != null && inventoryRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                InputBlocker.NotifyEscapeHandledByUi();
                Close();
            }
        }
    }
}
