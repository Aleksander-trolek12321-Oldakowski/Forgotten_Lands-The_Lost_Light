using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Item;

namespace Inventory
{
    [RequireComponent(typeof(RectTransform))]
    public class ItemSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
    {
        public Image icon;
        public int slotIndex = -1;
        public ItemType slotType = ItemType.Misc;
        public bool isEquipmentSlot = false;

        RectTransform rect;
        Canvas canvas;
        CanvasGroup canvasGroup;
        TooltipUI tooltip;
        InventoryManager manager;

        Vector3 originalLocalPos;

        GameObject dragIcon;
        RectTransform dragIconRect;
        Image dragIconImage;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            canvasGroup = gameObject.GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            tooltip = FindObjectOfType<TooltipUI>();
            manager = FindObjectOfType<InventoryManager>();
            originalLocalPos = rect.localPosition;
        }

        public void Refresh(InventoryItem invItem)
        {
            if (invItem == null || invItem.IsEmpty || invItem.data == null)
            {
                if (icon != null) { icon.enabled = false; icon.sprite = null; }
            }
            else
            {
                if (icon != null) { icon.enabled = true; icon.sprite = invItem.data.itemSprite; }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            InventoryItem item = isEquipmentSlot ? manager.equipment[slotType] : (slotIndex >= 0 ? manager.backpackSlots[slotIndex] : null);
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
            canvasGroup.blocksRaycasts = false;

            manager?.NotifyInventoryChanged();

            if (dragIcon == null)
            {
                dragIcon = new GameObject("DragIcon");
                dragIcon.transform.SetParent(canvas.transform, false);
                dragIconRect = dragIcon.AddComponent<RectTransform>();
                dragIconImage = dragIcon.AddComponent<Image>();
                dragIconImage.raycastTarget = false;
            }

            InventoryItem item = isEquipmentSlot ? manager.equipment[slotType] : (slotIndex >= 0 ? manager.backpackSlots[slotIndex] : null);
            if (item != null && !item.IsEmpty && item.data != null)
            {
                dragIconImage.sprite = item.data.itemSprite;
                dragIconImage.enabled = true;
            }
            else
            {
                dragIconImage.enabled = false;
            }

            Vector2 size = rect.sizeDelta;
            dragIconRect.sizeDelta = size;
            dragIconRect.localScale = new Vector3(0.5f, 0.5f, 1f);
            Vector2 slotCenter = GetSlotCenterScreenPosition();
            SetDragIconScreenPosition(slotCenter);

            if (icon != null) icon.enabled = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragIconRect == null || canvas == null) return;
            SetDragIconScreenPosition(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;

            GameObject pointerObject = eventData.pointerCurrentRaycast.gameObject;
            ItemSlotUI destSlot = pointerObject != null ? pointerObject.GetComponentInParent<ItemSlotUI>() : null;

            if (destSlot != null)
            {
            }
            else
            {
                bool insideInventory = IsPointerOverInventoryRoot(eventData.position);
                if (!insideInventory)
                {
                    if (!isEquipmentSlot && slotIndex >= 0)
                    {
                        Debug.Log("ItemSlotUI: Dropping backpack item to world.");
                        manager?.DropFromBackpack(slotIndex);
                    }
                    else if (isEquipmentSlot)
                    {
                        Debug.Log("ItemSlotUI: Dropping equipped item to world.");
                        manager?.DropEquipmentSlot(slotType);
                    }
                }
                else
                {
                    Debug.Log("ItemSlotUI: Dropped inside inventory but not on a slot - snapping back.");
                }
            }

            if (icon != null) icon.enabled = true;

            if (dragIcon != null)
            {
                Destroy(dragIcon);
                dragIcon = null;
                dragIconRect = null;
                dragIconImage = null;
            }

            manager?.NotifyInventoryChanged();

            rect.localPosition = originalLocalPos;
        }

        public void OnDrop(PointerEventData eventData)
        {
            var dragged = eventData.pointerDrag;
            if (dragged == null) return;
            var src = dragged.GetComponent<ItemSlotUI>();
            if (src == null) return;

            if (!isEquipmentSlot && !src.isEquipmentSlot)
            {
                manager?.SwapBackpackSlots(src.slotIndex, slotIndex);
                Debug.Log($"ItemSlotUI: Swapped backpack slots {src.slotIndex} <-> {slotIndex}");
            }
            else if (isEquipmentSlot && !src.isEquipmentSlot)
            {
                if (manager?.EquipFromBackpack(src.slotIndex) == true)
                    Debug.Log($"ItemSlotUI: Equipped item from backpack slot {src.slotIndex} to equipment slot {slotType}");
            }
            else if (!isEquipmentSlot && src.isEquipmentSlot)
            {
                if (manager?.Unequip(src.slotType) == true)
                    Debug.Log($"ItemSlotUI: Unequipped {src.slotType} to backpack");
            }

            manager?.NotifyInventoryChanged();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.clickCount == 2)
            {
                if (!isEquipmentSlot)
                {
                    manager?.EquipFromBackpack(slotIndex);
                    Debug.Log($"ItemSlotUI: Double-click equip from backpack {slotIndex}");
                }
                else
                {
                    manager?.Unequip(slotType);
                    Debug.Log($"ItemSlotUI: Double-click unequip {slotType}");
                }
                manager?.NotifyInventoryChanged();
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
            dragIconRect.localPosition = localPoint;
        }

        private bool IsPointerOverInventoryRoot(Vector2 screenPos)
        {
            if (canvas == null) return false;
            Transform root = canvas.transform.Find("InventoryRoot");
            if (root == null) return false;
            RectTransform rt = root as RectTransform;
            if (rt == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, canvas.worldCamera);
        }
    }
}
