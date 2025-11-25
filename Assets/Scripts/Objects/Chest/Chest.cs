using System.Collections.Generic;
using UnityEngine;
using Player;
using Item;
using Inventory;

namespace chest
{
    [RequireComponent(typeof(Collider))]
    public class Chest : MonoBehaviour
    {
        [Header("Chest settings")]
        [Tooltip("Number of slots inside this chest")]
        public int chestSize = 12;

        [Tooltip("Reference to the ChestUIController in the scene (assign from Canvas)")]
        public ChestUIController chestUIController;

        [Tooltip("Optional prompt shown in console")]
        public string promptMessage = "Press E to open chest.";

        [HideInInspector] public List<InventoryItem> chestSlots = new List<InventoryItem>();

        private bool playerInRange = false;
        private PlayerBase nearbyPlayer;

        private void Reset()
        {
            Collider c = GetComponent<Collider>();
            if (c) c.isTrigger = true;
        }

        private void Awake()
        {
            chestSlots = new List<InventoryItem>(chestSize);
            for (int i = 0; i < chestSize; i++)
                chestSlots.Add(new InventoryItem(null, 0));
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<PlayerBase>();
            if (player == null) player = other.GetComponentInParent<PlayerBase>();
            if (player != null)
            {
                playerInRange = true;
                nearbyPlayer = player;
                Debug.Log(promptMessage);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var player = other.GetComponent<PlayerBase>();
            if (player == null) player = other.GetComponentInParent<PlayerBase>();
            if (player != null && player == nearbyPlayer)
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
                OpenChestUI();
            }
        }

        public void OpenChestUI()
        {
            if (chestUIController == null)
            {
                Debug.LogWarning("Chest: chestUIController is not assigned. Please assign it in inspector (Canvas -> ChestUIController).");
                return;
            }

            chestUIController.Open(this, nearbyPlayer);
        }

        public bool TryAddToChest(InventoryItem item)
        {
            if (item == null || item.IsEmpty) return false;

            for (int i = 0; i < chestSlots.Count; i++)
            {
                if (chestSlots[i] == null || chestSlots[i].IsEmpty)
                {
                    chestSlots[i] = item;
                    Debug.Log($"Chest: Added item '{item.data?.name}' to chest slot {i}");
                    return true;
                }
            }

            Debug.Log("Chest: chest is full, cannot add item.");
            return false;
        }

        public InventoryItem RemoveFromChest(int index)
        {
            if (index < 0 || index >= chestSlots.Count) return new InventoryItem(null, 0);

            InventoryItem it = chestSlots[index];
            chestSlots[index] = new InventoryItem(null, 0);
            Debug.Log($"Chest: Removed item from slot {index}");
            return it;
        }

        public InventoryItem Peek(int index)
        {
            if (index < 0 || index >= chestSlots.Count) return new InventoryItem(null, 0);
            return chestSlots[index];
        }
    }
}
