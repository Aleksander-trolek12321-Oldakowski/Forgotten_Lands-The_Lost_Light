using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Player;

namespace Item
{
    [RequireComponent(typeof(Collider))]
    public class LootBag : MonoBehaviour
    {
        [Header("Loot settings")]
        [Tooltip("List of items.")]
        public List<ItemData> lootTable = new List<ItemData>();

        [Tooltip("Prompt message for player.")]
        public string promptMessage = "Press E to open bag.";

        private bool playerInRange = false;
        private PlayerBase nearbyPlayer;

        public GameObject toolTip;

        private void Reset()
        {
            Collider c = GetComponent<Collider>();
            if (c != null) c.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerBase player = other.GetComponentInParent<PlayerBase>() ?? other.GetComponent<PlayerBase>();

            if (player != null)
            {
                playerInRange = true;
                nearbyPlayer = player;
                Debug.Log(promptMessage);
                if (toolTip != null) toolTip.SetActive(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerBase player = other.GetComponentInParent<PlayerBase>() ?? other.GetComponent<PlayerBase>();

            if (player != null && player == nearbyPlayer)
            {
                playerInRange = false;
                nearbyPlayer = null;
                if (toolTip != null) toolTip.SetActive(false);
            }
        }

        private void Update()
        {
            if (!playerInRange || nearbyPlayer == null) return;

            if (Input.GetKeyDown(KeyCode.E))
            {
                GiveRandomItemToPlayer();
            }
        }

        private void GiveRandomItemToPlayer()
        {
            if (lootTable == null || lootTable.Count == 0)
            {
                Debug.LogWarning("LootBag: no items in lootTable.");
                return;
            }

            int index = Random.Range(0, lootTable.Count);
            ItemData selected = lootTable[index];

            if (selected != null)
            {
                InventoryManager inv = nearbyPlayer.GetComponent<InventoryManager>() ?? FindObjectOfType<InventoryManager>();
                bool added = false;
                if (inv != null)
                {
                    added = inv.TryAddToBackpack(selected, 1);
                }
                else
                {
                    nearbyPlayer.PickupItem(selected);
                    added = true;
                }

                if (added)
                {
                    Debug.Log($"LootBag: player received '{selected.itemName}'");
                    if (toolTip != null) toolTip.SetActive(false);
                    Destroy(gameObject);
                }
                else
                {
                    Debug.Log("LootBag: Backpack full - cannot pick up item.");
                }
            }
            else
            {
                Debug.LogWarning("LootBag: null item.");
            }
        }
    }
}
