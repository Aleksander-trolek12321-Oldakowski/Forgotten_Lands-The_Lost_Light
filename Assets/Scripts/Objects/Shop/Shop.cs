using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Player;
using Item;

namespace shop
{
    [RequireComponent(typeof(Collider))]
    public class Shop : MonoBehaviour
    {
        [Header("Shop settings")]
        [Tooltip("Reference to the ShopUIController in the scene (assign in inspector).")]
        public ShopUIController shopUIController;

        [Tooltip("Number of slots inside this shop")]
        public int shopSize = 12;

        [Tooltip("All items shop can offer (assign ItemData instances in inspector).")]
        public List<ItemData> possibleItems = new List<ItemData>();

        [Tooltip("If true, shop will automatically refresh its stock every refreshInterval seconds.")]
        public bool autoRefresh = true;

        [Tooltip("Time in seconds between automatic shop refreshes.")]
        public float refreshInterval = 60f;

        [Tooltip("Allow duplicates when populating the shop. If false, items will be unique if possible.")]
        public bool allowDuplicates = true;

        [Tooltip("Should the shop refresh even when a player currently has the shop UI open?")]
        public bool refreshWhileOpen = false;

        [Tooltip("Optional prompt shown in console")]
        public string prompt = "Press E to open shop.";

        [HideInInspector] public List<InventoryItem> shopSlots = new List<InventoryItem>();

        private PlayerBase nearbyPlayer;
        private bool playerInRange = false;
        private Coroutine autoRefreshCoroutine = null;
        [HideInInspector] public bool isUIOpen = false;

        private void Reset()
        {
            var c = GetComponent<Collider>();
            if (c != null) c.isTrigger = true;
        }

        private void Awake()
        {
            if (shopUIController == null)
                shopUIController = FindObjectOfType<ShopUIController>();

            shopSlots = new List<InventoryItem>(shopSize);
            for (int i = 0; i < shopSize; i++)
                shopSlots.Add(new InventoryItem(null, 0));

            PopulateRandomly();

            if (autoRefresh && refreshInterval > 0f)
            {
                autoRefreshCoroutine = StartCoroutine(AutoRefreshCoroutine());
            }
        }

        private void OnDestroy()
        {
            if (autoRefreshCoroutine != null) StopCoroutine(autoRefreshCoroutine);
        }

        private void OnTriggerEnter(Collider other)
        {
            var p = other.GetComponent<PlayerBase>();
            if (p == null) p = other.GetComponentInParent<PlayerBase>();
            if (p != null)
            {
                playerInRange = true;
                nearbyPlayer = p;
                Debug.Log(prompt);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var p = other.GetComponent<PlayerBase>();
            if (p == null) p = other.GetComponentInParent<PlayerBase>();
            if (p != null && p == nearbyPlayer)
            {
                playerInRange = false;
                nearbyPlayer = null;
            }
        }

        private void Update()
        {
            if (!playerInRange || nearbyPlayer == null) return;

            if (Input.GetKeyDown(KeyCode.E))
            {
                OpenShopUI();
            }
        }

        public void OpenShopUI()
        {
            if (shopUIController == null)
                shopUIController = FindObjectOfType<ShopUIController>();

            if (shopUIController == null)
            {
                Debug.LogWarning("Shop: shopUIController not assigned.");
                return;
            }

            isUIOpen = true;
            shopUIController.Open(this, nearbyPlayer);
        }

        public void CloseShopUI()
        {
            isUIOpen = false;
        }

        public void PopulateRandomly()
        {
            if (possibleItems == null || possibleItems.Count == 0)
            {
                Debug.LogWarning("Shop: possibleItems empty - cannot populate shop.");
                return;
            }

            List<int> availableIndices = new List<int>();
            for (int i = 0; i < possibleItems.Count; i++) availableIndices.Add(i);

            for (int i = 0; i < shopSlots.Count; i++)
            {
                if (allowDuplicates)
                {
                    int idx = Random.Range(0, possibleItems.Count);
                    ItemData chosen = possibleItems[idx];
                    shopSlots[i] = new InventoryItem(chosen, 1);
                }
                else
                {
                    if (availableIndices.Count == 0)
                    {
                        int idx = Random.Range(0, possibleItems.Count);
                        shopSlots[i] = new InventoryItem(possibleItems[idx], 1);
                    }
                    else
                    {
                        int pickListIndex = Random.Range(0, availableIndices.Count);
                        int chosenIdx = availableIndices[pickListIndex];
                        ItemData chosen = possibleItems[chosenIdx];
                        shopSlots[i] = new InventoryItem(chosen, 1);
                        availableIndices.RemoveAt(pickListIndex);
                    }
                }
            }

            Debug.Log($"Shop: Populated {shopSlots.Count} slots randomly.");
            if (shopUIController != null)
                shopUIController.RefreshAllSlots();
        }

        public void ForceRefresh()
        {
            PopulateRandomly();
        }

        private IEnumerator AutoRefreshCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(refreshInterval);
                if (isUIOpen && !refreshWhileOpen)
                {
                    Debug.Log("Shop: Skipping auto-refresh because UI is open and refreshWhileOpen=false.");
                    continue;
                }

                PopulateRandomly();
                Debug.Log("Shop: Auto-refreshed stock.");
            }
        }

        public InventoryItem Peek(int index)
        {
            if (shopSlots == null || index < 0 || index >= shopSlots.Count)
            {
                return new InventoryItem(null, 0);
            }
            var it = shopSlots[index];
            return (it == null) ? new InventoryItem(null, 0) : it;
        }

        public void SetSlot(int index, InventoryItem item)
        {
            if (shopSlots == null)
            {
                shopSlots = new List<InventoryItem>();
            }

            if (index < 0)
                return;

            while (shopSlots.Count <= index)
            {
                shopSlots.Add(new InventoryItem(null, 0));
            }

            shopSlots[index] = (item == null) ? new InventoryItem(null, 0) : item;
        }

        public void ClearSlot(int index)
        {
            if (shopSlots == null || index < 0 || index >= shopSlots.Count) return;
            shopSlots[index] = new InventoryItem(null, 0);
        }
    }
}
