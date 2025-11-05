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

        [Tooltip("Log communicate.")]
        public string promptMessage = "Naciśnij E, aby otworzyć worek.";

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
            PlayerBase player = other.GetComponent<PlayerBase>();
            if (player == null)
                player = other.GetComponent<PlayerBase>();

            if (player != null)
            {
                playerInRange = true;
                nearbyPlayer = player;
                Debug.Log(promptMessage);
                toolTip.SetActive(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerBase player = other.GetComponent<PlayerBase>();
            if (player == null)
                player = other.GetComponent<PlayerBase>();

            if (player != null && player == nearbyPlayer)
            {
                playerInRange = false;
                nearbyPlayer = null;
                toolTip.SetActive(false);
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
                nearbyPlayer.PickupItem(selected);
                Debug.Log($"Lootbag: random item is '{selected.name}'");
            }
            else
            {
                Debug.LogWarning("Lootbag: null item.");
            }

            Destroy(gameObject);
        }
    }
}