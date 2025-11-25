using UnityEngine;
using UnityEngine.EventSystems;
using Inventory;

namespace shop
{
    public class ShopPanelDropZone : MonoBehaviour, IDropHandler
    {
        public ShopUIController shopController;

        public void OnDrop(PointerEventData eventData)
        {
            if (shopController == null) return;

            var dragged = eventData.pointerDrag;
            if (dragged == null) return;

            var playerSlot = dragged.GetComponentInParent<ItemSlotUI>();
            if (playerSlot != null)
            {
                playerSlot.wasHandledByUI = true;

                shopController.SellFromPlayerToShop(playerSlot.slotIndex, -1);

                playerSlot.RefreshFromInventory();

                eventData.Use();

                shopController.RefreshAllSlots();
                return;
            }
        }
    }
}
