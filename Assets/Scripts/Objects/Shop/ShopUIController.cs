using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Player;
using Item;
using Inventory;
namespace shop
{
    public class ShopUIController : MonoBehaviour
    {
        [Header("Manual UI references")]
        public GameObject inventoryRoot;
        public RectTransform shopGridPanel;
        public Text playerMoneyText;
        public TooltipUI tooltip;
        public InventoryUIController inventoryController;

        [HideInInspector] public Shop currentShop;
        private PlayerBase interactingPlayer;
        private InventoryManager playerInventoryManager;
        private List<ShopSlotUI> shopSlotUIs = new List<ShopSlotUI>();

        [Header("Respawn")]
        [Tooltip("How long to block lootbag respawn after a sale (seconds)")]
        public float lootbagRespawnBlockDuration = 0.6f;

        private void Awake()
        {
            if (tooltip == null) tooltip = FindObjectOfType<TooltipUI>();
            if (inventoryController == null) inventoryController = FindObjectOfType<InventoryUIController>();
        }

        private void Update()
        {
            if (inventoryRoot != null && inventoryRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                InputBlocker.NotifyEscapeHandledByUi();
                Close();
            }
        }

        public void Open(Shop shop, PlayerBase player)
        {
            if (inventoryRoot != null && inventoryRoot.activeSelf)
                return;

            if (inventoryRoot == null)
            {
                Debug.LogWarning("ShopUIController: inventoryRoot not assigned.");
                if (shop != null)
                    shop.CloseShopUI();
                return;
            }

            InputBlocker.Block(player);

            currentShop = shop;
            interactingPlayer = player;
            playerInventoryManager = FindObjectOfType<InventoryManager>();

            if (inventoryController != null) inventoryController.OpenInventory(false);

            inventoryRoot.SetActive(true);
            AutoCollectSlots();
            RefreshAllSlots();

            if (interactingPlayer != null) interactingPlayer.SetControlsEnabled(false);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
        }

        public void Close()
        {
            bool isShopOpen = inventoryRoot != null && inventoryRoot.activeSelf;
            if (!isShopOpen && currentShop == null && interactingPlayer == null)
                return;

            Shop shopToClose = currentShop;
            if (shopToClose != null)
                shopToClose.CloseShopUI();

            InputBlocker.Restore(interactingPlayer);
            
            if (inventoryRoot != null) inventoryRoot.SetActive(false);
            if (inventoryController != null) inventoryController.CloseInventory();

            if (interactingPlayer != null) interactingPlayer.SetControlsEnabled(true);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            currentShop = null;
            interactingPlayer = null;
            playerInventoryManager = null;
        }

        [ContextMenu("AutoCollectSlots")]
        public void AutoCollectSlots()
        {
            shopSlotUIs.Clear();
            if (shopGridPanel == null)
            {
                Debug.LogWarning("ShopUIController: shopGridPanel is null.");
                return;
            }

            var found = shopGridPanel.GetComponentsInChildren<ShopSlotUI>(true);
            Array.Sort(found, (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            for (int i = 0; i < found.Length; i++)
            {
                var s = found[i];
                s.Initialize(this, i, tooltip);
                shopSlotUIs.Add(s);
                Debug.Log($"ShopUIController: Collected shop slot {i} -> {s.gameObject.name}");
            }

            if (currentShop != null && currentShop.shopSlots.Count != shopSlotUIs.Count)
                Debug.LogWarning($"ShopUIController: shopSlots ({currentShop.shopSlots.Count}) != uiSlots ({shopSlotUIs.Count})");
        }

        public void RefreshAllSlots()
        {
            if (currentShop == null)
            {
                Debug.LogWarning("ShopUIController: currentShop is null.");
                return;
            }

            for (int i = 0; i < shopSlotUIs.Count; i++)
            {
                InventoryItem it = (i < currentShop.shopSlots.Count) ? currentShop.shopSlots[i] : new InventoryItem(null,0);
                shopSlotUIs[i].Refresh(it);
                string name = (it != null && !it.IsEmpty && it.data != null) ? it.data.name : "<empty>";
                Debug.Log($"  shop slot[{i}] -> {name}");
            }

            if (playerMoneyText != null && interactingPlayer != null)
                playerMoneyText.text = $"{interactingPlayer.GetMoney():F1}";
        }

        public bool SellFromPlayerToShop(int playerBackpackIndex, int shopDestIndex)
        {
            Inventory.InventoryManager inv = null;
            if (interactingPlayer != null)
                inv = interactingPlayer.GetComponent<Inventory.InventoryManager>();
            if (inv == null)
                inv = FindObjectOfType<Inventory.InventoryManager>();

            if (inv == null)
            {
                Debug.LogWarning("ShopUIController: InventoryManager not found.");
                return false;
            }

            if (playerBackpackIndex < 0 || playerBackpackIndex >= inv.backpackSlots.Count)
            {
                Debug.LogWarning($"ShopUIController: invalid playerBackpackIndex {playerBackpackIndex}");
                return false;
            }

            var item = inv.backpackSlots[playerBackpackIndex];
            if (item == null || item.IsEmpty || item.data == null)
            {
                Debug.LogWarning("ShopUIController: attempted to sell empty slot.");
                return false;
            }

            if (currentShop == null || currentShop.shopSlots == null || currentShop.shopSlots.Count == 0)
            {
                Debug.LogWarning("ShopUIController: currentShop invalid or empty.");
                return false;
            }

            if (shopDestIndex < 0 || shopDestIndex >= currentShop.shopSlots.Count)
            {
                int found = -1;
                for (int i = 0; i < currentShop.shopSlots.Count; i++)
                {
                    var s = currentShop.shopSlots[i];
                    if (s == null || s.IsEmpty)
                    {
                        found = i;
                        break;
                    }
                }
                shopDestIndex = (found != -1) ? found : 0;
            }

            if (shopDestIndex < 0 || shopDestIndex >= currentShop.shopSlots.Count)
            {
                Debug.LogWarning("ShopUIController: could not resolve valid shopDestIndex.");
                return false;
            }

            Debug.Log($"SellFromPlayerToShop: BEFORE sell - player slot[{playerBackpackIndex}] = {(inv.backpackSlots[playerBackpackIndex].data != null ? inv.backpackSlots[playerBackpackIndex].data.itemName : "<empty>")}, shop slot[{shopDestIndex}] = {(currentShop.shopSlots[shopDestIndex].data != null ? currentShop.shopSlots[shopDestIndex].data.itemName : "<empty>")}");

            float buyPrice = item.data.Price;
            float sellPrice = Mathf.Round((buyPrice / 3f) * 10f) / 10f;

            InventoryItem previousShopItem = currentShop.shopSlots[shopDestIndex];

            currentShop.shopSlots[shopDestIndex] = item;

            inv.backpackSlots[playerBackpackIndex] = new InventoryItem(null, 0);

            if (interactingPlayer != null)
            {
                interactingPlayer.AddMoney(sellPrice);
            }

            Debug.Log($"ShopUIController: Player sold '{item.data.itemName}' for {sellPrice:F1} into shop slot {shopDestIndex}");

            if (this != null)
                StartCoroutine(BlockLootbagRespawnCoroutine());

            inv.NotifyInventoryChanged();
            RefreshAllSlots();

            Debug.Log($"SellFromPlayerToShop: AFTER sell - player slot[{playerBackpackIndex}] = {(inv.backpackSlots[playerBackpackIndex].data != null ? inv.backpackSlots[playerBackpackIndex].data.itemName : "<empty>")}, shop slot[{shopDestIndex}] = {(currentShop.shopSlots[shopDestIndex].data != null ? currentShop.shopSlots[shopDestIndex].data.itemName : "<empty>")}");

            return true;
        }

        public bool BuyFromShopToPlayer(int shopIndex, int playerDestIndex = -1)
        {
            if (playerInventoryManager == null) playerInventoryManager = FindObjectOfType<InventoryManager>();
            if (playerInventoryManager == null) return false;

            if (currentShop == null) return false;
            if (shopIndex < 0 || shopIndex >= currentShop.shopSlots.Count) return false;
            var item = currentShop.shopSlots[shopIndex];
            if (item == null || item.IsEmpty || item.data == null) return false;

            float buyPrice = item.data.Price;

            if (!interactingPlayer.TrySpend(buyPrice))
            {
                Debug.Log($"ShopUIController: Player cannot afford '{item.data.name}' price {buyPrice:F1}");
                return false;
            }

            if (playerDestIndex >= 0 && playerDestIndex < playerInventoryManager.backpackSlots.Count)
            {
                var existing = playerInventoryManager.backpackSlots[playerDestIndex];
                playerInventoryManager.backpackSlots[playerDestIndex] = item;
                currentShop.shopSlots[shopIndex] = new InventoryItem(null,0);
                Debug.Log($"ShopUIController: Player bought '{item.data.name}' for {buyPrice:F1} into player slot {playerDestIndex}");
                RefreshAllSlots();
                playerInventoryManager.NotifyInventoryChanged();
                return true;
            }

            for (int i = 0; i < playerInventoryManager.backpackSlots.Count; i++)
            {
                if (playerInventoryManager.backpackSlots[i] == null || playerInventoryManager.backpackSlots[i].IsEmpty)
                {
                    playerInventoryManager.backpackSlots[i] = item;
                    currentShop.shopSlots[shopIndex] = new InventoryItem(null,0);
                    Debug.Log($"ShopUIController: Player bought '{item.data.name}' for {buyPrice:F1} into player slot {i}");
                    RefreshAllSlots();
                    playerInventoryManager.NotifyInventoryChanged();
                    return true;
                }
            }

            interactingPlayer.AddMoney(buyPrice);
            Debug.LogWarning("ShopUIController: Player has no free backpack slot. Purchase refunded.");
            return false;
        }

        private IEnumerator BlockLootbagRespawnCoroutine()
        {
            LootBagRespawnManager.AllowRespawn = false;
            Debug.Log("ShopUIController: Blocking LootBag respawn for " + lootbagRespawnBlockDuration + "s");
            yield return new WaitForSeconds(lootbagRespawnBlockDuration);
            LootBagRespawnManager.AllowRespawn = true;
            Debug.Log("ShopUIController: Re-enabled LootBag respawn.");
        }
    }
}
