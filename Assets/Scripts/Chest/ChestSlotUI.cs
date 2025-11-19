using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Item;
using Inventory;

namespace chest
{
    [RequireComponent(typeof(RectTransform))]
    public class ChestSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public Image icon;
        public int slotIndex = -1;
        TooltipUI tooltip;
        public ChestUIController controller;

        RectTransform rect;
        Canvas canvas;
        GameObject dragIcon;
        RectTransform dragIconRect;
        Image dragIconImage;
        CanvasGroup canvasGroup;

        public void Initialize(ChestUIController owner, int index, TooltipUI tooltipInstance = null)
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
                if (icon.sprite != invItem.data.itemSprite)
                    icon.sprite = invItem.data.itemSprite;
                icon.enabled = true;
                icon.SetAllDirty();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var item = controller?.currentChest?.Peek(slotIndex);
            if (item != null && !item.IsEmpty)
            {
                Vector2 screenPos = GetSlotTopCenterScreenPosition();
                tooltip?.Show(item.data, screenPos);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tooltip?.Hide();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (canvasGroup != null) canvasGroup.blocksRaycasts = false;

            // create ghost
            if (dragIcon == null)
            {
                dragIcon = new GameObject("ChestDragIcon");
                dragIcon.transform.SetParent(canvas.transform, false);
                dragIconRect = dragIcon.AddComponent<RectTransform>();
                dragIconImage = dragIcon.AddComponent<Image>();
                dragIconImage.raycastTarget = false;
                dragIconImage.preserveAspect = true;
            }

            var item = controller?.currentChest?.Peek(slotIndex);
            if (item != null && !item.IsEmpty)
            {
                dragIconImage.sprite = item.data.itemSprite;
                dragIconImage.enabled = true;
            }
            else
            {
                dragIconImage.enabled = false;
            }

            dragIconRect.sizeDelta = rect.rect.size;
            dragIconRect.localScale = new Vector3(0.5f, 0.5f, 1f);
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
            ChestSlotUI destChestSlot = pointerObject != null ? pointerObject.GetComponentInParent<ChestSlotUI>() : null;
            ItemSlotUI playerSlot = pointerObject != null ? pointerObject.GetComponentInParent<ItemSlotUI>() : null;

            if (destChestSlot != null || playerSlot != null)
            {
            }
            else
            {
                bool insideChestUI = IsPointerOverChestRoot(eventData.position);
                if (!insideChestUI)
                {
                    bool moved = controller.TransferFromChestToPlayer(slotIndex, -1);
                    if (!moved)
                        Debug.Log("ChestSlotUI: Could not move item to player - player's inventory may be full.");
                }
                else
                {
                }
            }

            if (dragIcon != null)
            {
                Destroy(dragIcon);
                dragIcon = null;
                dragIconRect = null;
                dragIconImage = null;
            }

            if (controller != null) controller.RefreshAllSlots();
        }

        public void OnDrop(PointerEventData eventData)
        {
            var dragged = eventData.pointerDrag;
            if (dragged == null) return;

            var srcPlayer = dragged.GetComponent<ItemSlotUI>();
            var srcChest = dragged.GetComponent<ChestSlotUI>();

            if (srcPlayer != null)
            {
                controller.TransferFromPlayerToChest(srcPlayer.slotIndex, slotIndex);
                controller.RefreshAllSlots();
                Debug.Log($"ChestSlotUI: Received item from player slot {srcPlayer.slotIndex} -> chest slot {slotIndex}");
            }
            else if (srcChest != null && srcChest != this)
            {
                // swap chest slots
                var chest = controller.currentChest;
                InventoryItem a = chest.Peek(srcChest.slotIndex);
                InventoryItem b = chest.Peek(slotIndex);
                chest.chestSlots[srcChest.slotIndex] = b;
                chest.chestSlots[slotIndex] = a;
                Debug.Log($"ChestSlotUI: swapped chest slots {srcChest.slotIndex} <-> {slotIndex}");
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

        private bool IsPointerOverChestRoot(Vector2 screenPos)
        {
            if (controller == null || controller.inventoryRoot == null) return false;
            var rt = controller.inventoryRoot.GetComponent<RectTransform>();
            if (rt == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, canvas.worldCamera);
        }
    }
}
