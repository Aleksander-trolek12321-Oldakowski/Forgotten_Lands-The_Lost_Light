using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Item;
using Inventory;

namespace shop
{
    [RequireComponent(typeof(RectTransform))]
    public class ShopSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public Image icon;
        public int slotIndex = -1;
        TooltipUI tooltip;
        public ShopUIController controller;

        RectTransform rect;
        Canvas canvas;
        GameObject dragIcon;
        RectTransform dragIconRect;
        Image dragIconImage;
        CanvasGroup canvasGroup;

        public void Initialize(ShopUIController owner, int index, TooltipUI tooltipInstance = null)
        {
            controller = owner;
            slotIndex = index;
            tooltip = tooltipInstance;
            rect = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();

            canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (icon == null)
            {
                var ic = transform.Find("Icon")?.GetComponent<Image>();
                if (ic != null) icon = ic;
            }
        }

        public void Refresh(InventoryItem invItem)
        {
            if (icon == null) return;
            if (invItem == null || invItem.IsEmpty || invItem.data == null)
            {
                icon.sprite = null;
                icon.enabled = false;
                icon.SetAllDirty();
            }
            else
            {
                icon.sprite = invItem.data.itemSprite;
                icon.enabled = true;
                icon.SetAllDirty();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var item = controller?.currentShop?.shopSlots[slotIndex];
            if (item != null && !item.IsEmpty && item.data != null)
            {
                float buy = item.data.Price;
                float sell = Mathf.Round((buy / 3f) * 10f) / 10f;
                Vector2 pos = GetSlotTopCenterScreenPosition();
                tooltip?.Show(item.data, pos, buy, sell);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tooltip?.Hide();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (canvasGroup != null) canvasGroup.blocksRaycasts = false;

            if (dragIcon == null)
            {
                dragIcon = new GameObject("ShopDragIcon");
                dragIcon.transform.SetParent(canvas.transform, false);
                dragIconRect = dragIcon.AddComponent<RectTransform>();
                dragIconImage = dragIcon.AddComponent<Image>();
                dragIconImage.raycastTarget = false;
                dragIconImage.preserveAspect = true;
            }

            var item = controller?.currentShop?.shopSlots[slotIndex];
            if (item != null && !item.IsEmpty && item.data != null)
            {
                dragIconImage.sprite = item.data.itemSprite;
                dragIconImage.enabled = true;
            }
            else
            {
                dragIconImage.enabled = false;
            }

            dragIconRect.sizeDelta = rect.rect.size;
            SetDragIconScreenPosition(GetSlotCenterScreenPosition());

            if (icon != null) icon.enabled = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragIconRect == null || canvas == null) return;
            SetDragIconScreenPosition(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

            GameObject pointerObject = eventData.pointerCurrentRaycast.gameObject;
            ItemSlotUI playerSlot = pointerObject != null ? pointerObject.GetComponentInParent<ItemSlotUI>() : null;
            ShopSlotUI destShopSlot = pointerObject != null ? pointerObject.GetComponentInParent<ShopSlotUI>() : null;

            if (playerSlot != null)
            {
                controller.BuyFromShopToPlayer(slotIndex, playerSlot.slotIndex);
            }
            else if (destShopSlot != null && destShopSlot != this)
            {
                var shop = controller.currentShop;
                InventoryItem a = shop.Peek(slotIndex);
                InventoryItem b = shop.Peek(destShopSlot.slotIndex);
                shop.shopSlots[slotIndex] = b;
                shop.shopSlots[destShopSlot.slotIndex] = a;
                Debug.Log($"ShopSlotUI: swapped shop slots {slotIndex} <-> {destShopSlot.slotIndex}");
                controller.RefreshAllSlots();
            }
            else
            {
                bool insideShopUI = IsPointerOverShopRoot(eventData.position);
                if (!insideShopUI)
                {
                    controller.BuyFromShopToPlayer(slotIndex, -1);
                }
            }

            if (dragIcon != null) Destroy(dragIcon);

            controller.RefreshAllSlots();
        }

        public void OnDrop(PointerEventData eventData)
        {
            var dragged = eventData.pointerDrag;
            if (dragged == null) return;

            var srcPlayer = dragged.GetComponent<ItemSlotUI>();
            var srcShop = dragged.GetComponent<ShopSlotUI>();

            if (srcPlayer != null)
            {
                controller.SellFromPlayerToShop(srcPlayer.slotIndex, slotIndex);
                controller.RefreshAllSlots();
                Debug.Log($"ShopSlotUI: Player sold item from slot {srcPlayer.slotIndex} -> shop slot {slotIndex}");
            }
            else if (srcShop != null && srcShop != this)
            {
                var shop = controller.currentShop;
                InventoryItem a = shop.Peek(srcShop.slotIndex);
                InventoryItem b = shop.Peek(slotIndex);
                shop.shopSlots[srcShop.slotIndex] = b;
                shop.shopSlots[slotIndex] = a;
                Debug.Log($"ShopSlotUI: swapped shop slots {srcShop.slotIndex} <-> {slotIndex}");
                controller.RefreshAllSlots();
            }
        }

        Vector2 GetSlotCenterScreenPosition()
        {
            Vector3[] worldCorners = new Vector3[4];
            rect.GetWorldCorners(worldCorners);
            Vector3 bl = worldCorners[0];
            Vector3 tr = worldCorners[2];
            Vector3 center = (bl + tr) * 0.5f;
            return RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, center);
        }

        Vector2 GetSlotTopCenterScreenPosition()
        {
            Vector3[] worldCorners = new Vector3[4];
            rect.GetWorldCorners(worldCorners);
            Vector3 tr = worldCorners[2];
            Vector3 tl = worldCorners[1];
            Vector3 topCenter = (tr + tl) * 0.5f;
            return RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, topCenter);
        }

        void SetDragIconScreenPosition(Vector2 screenPos)
        {
            RectTransform canvasRect = canvas.transform as RectTransform;
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, canvas.worldCamera, out localPoint);
            if (dragIconRect != null) dragIconRect.localPosition = localPoint;
        }

        private bool IsPointerOverShopRoot(Vector2 screenPos)
        {
            if (controller == null || controller.inventoryRoot == null) return false;
            var rt = controller.inventoryRoot.GetComponent<RectTransform>();
            if (rt == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, canvas.worldCamera);
        }
    }
}
