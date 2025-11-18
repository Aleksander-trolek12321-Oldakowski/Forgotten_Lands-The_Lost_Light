using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Item;
using Player;

[RequireComponent(typeof(Canvas))]
public class InventoryUIAutoBuilder : MonoBehaviour
{
    [Header("References")]
    public InventoryManager inventoryManager;
    public GameObject slotPrefab;
    public TooltipUI tooltipPrefab;

    [Header("Backpack layout")]
    public int backpackColumns = 5;
    public int backpackRows = 4;
    public float slotSize = 120f;
    public float slotSpacing = 10f;

    [Header("Equipment layout (top)")]
    public List<ItemType> equipmentOrder = new List<ItemType>
    {
        ItemType.Helmet, ItemType.Chest, ItemType.Legs, ItemType.Boots, ItemType.Weapon, ItemType.Shield
    };
    public float equipSlotWidth = 140f;
    public float equipSlotHeight = 34f;
    public float equipSlotSpacing = 8f;

    [Header("Root sizing")]
    public bool autoSizeRootPanel = true;
    public Vector2 rootPanelSizeOverride = Vector2.zero;
    public float padding = 20f;
    public float uiScaleMultiplier = 1.0f;

    [Header("Controls")]
    public KeyCode toggleKey = KeyCode.I;
    public bool startClosed = true;

    // runtime
    GameObject rootPanel;
    RectTransform equipPanel;
    RectTransform backpackPanel;
    List<ItemSlotUI> backpackSlotsUI = new List<ItemSlotUI>();
    List<ItemSlotUI> equipSlotsUI = new List<ItemSlotUI>();
    PlayerBase player;
    TooltipUI tooltipInstance;

    private void Awake()
    {
        if (inventoryManager == null)
            inventoryManager = FindObjectOfType<InventoryManager>();

        if (inventoryManager == null)
        {
            Debug.LogError("InventoryUIAutoBuilder: InventoryManager not found in scene. Please add InventoryManager.");
            enabled = false;
            return;
        }

        player = inventoryManager.player ?? FindObjectOfType<PlayerBase>();
        tooltipInstance = tooltipPrefab ?? FindObjectOfType<TooltipUI>();

        BuildUI();

        inventoryManager.OnInventoryChanged += RefreshAllSlots;
        RefreshAllSlots();

        if (rootPanel != null)
        {
            rootPanel.SetActive(!startClosed);
            if (startClosed) CloseInventory();
        }
    }

    private void OnDestroy()
    {
        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged -= RefreshAllSlots;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory();
        }
    }

    void BuildUI()
    {
        Canvas rootCanvas = GetComponent<Canvas>();
        if (rootCanvas == null)
            rootCanvas = gameObject.AddComponent<Canvas>();

        Transform existing = transform.Find("InventoryRoot");
        if (existing != null) DestroyImmediate(existing.gameObject);

        rootPanel = new GameObject("InventoryRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rootPanel.transform.SetParent(transform, false);
        RectTransform rpRt = rootPanel.GetComponent<RectTransform>();
        rpRt.anchorMin = new Vector2(0.5f, 0.5f);
        rpRt.anchorMax = new Vector2(0.5f, 0.5f);
        rpRt.pivot = new Vector2(0.5f, 0.5f);

        Image bg = rootPanel.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.6f);

        float scaledSlot = slotSize * uiScaleMultiplier;
        float scaledSpacing = slotSpacing * uiScaleMultiplier;
        float scaledEquipW = equipSlotWidth * uiScaleMultiplier;
        float scaledEquipH = equipSlotHeight * uiScaleMultiplier;
        float scaledEquipSpacing = equipSlotSpacing * uiScaleMultiplier;

        float equipWidth = equipmentOrder.Count * scaledEquipW + Mathf.Max(0, equipmentOrder.Count - 1) * scaledEquipSpacing + padding * 2f;
        float backpackWidth = backpackColumns * scaledSlot + Mathf.Max(0, backpackColumns - 1) * scaledSpacing + padding * 2f;
        float contentWidth = Mathf.Max(equipWidth, backpackWidth);

        float equipHeight = scaledEquipH + padding;
        float backpackHeight = backpackRows * scaledSlot + Mathf.Max(0, backpackRows - 1) * scaledSpacing + padding * 2f;
        float totalHeight = equipHeight + backpackHeight + padding;

        Vector2 computedRoot = new Vector2(contentWidth, totalHeight);

        if (rootPanelSizeOverride != Vector2.zero)
            rpRt.sizeDelta = rootPanelSizeOverride * uiScaleMultiplier;
        else if (autoSizeRootPanel)
            rpRt.sizeDelta = computedRoot;
        else
            rpRt.sizeDelta = new Vector2(900f, 600f) * uiScaleMultiplier;

        GameObject eq = new GameObject("EquipPanel", typeof(RectTransform));
        eq.transform.SetParent(rootPanel.transform, false);
        equipPanel = eq.GetComponent<RectTransform>();
        equipPanel.anchorMin = new Vector2(0.5f, 1f);
        equipPanel.anchorMax = new Vector2(0.5f, 1f);
        equipPanel.pivot = new Vector2(0.5f, 1f);
        equipPanel.anchoredPosition = new Vector2(0f, -padding / 2f);

        HorizontalLayoutGroup hlg = eq.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.UpperCenter;
        hlg.spacing = scaledEquipSpacing;
        hlg.padding = new RectOffset((int)padding, (int)padding, (int)(padding / 2f), 0);

        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        equipSlotsUI.Clear();
        for (int i = 0; i < equipmentOrder.Count; i++)
        {
            ItemType type = equipmentOrder[i];
            if (slotPrefab == null)
            {
                Debug.LogError("InventoryUIAutoBuilder: slotPrefab is not assigned.");
                continue;
            }

            GameObject s = Instantiate(slotPrefab, eq.transform);
            s.name = "EqSlot_" + type.ToString();

            RectTransform srt = s.GetComponent<RectTransform>();
            if (srt != null)
                srt.sizeDelta = new Vector2(scaledEquipW, scaledEquipH);

            LayoutElement le = s.GetComponent<LayoutElement>() ?? s.AddComponent<LayoutElement>();
            le.preferredWidth = scaledEquipW;
            le.preferredHeight = scaledEquipH;
            le.minWidth = scaledEquipW;
            le.minHeight = scaledEquipH;
            le.flexibleWidth = 0;
            le.flexibleHeight = 0;

            ItemSlotUI slot = s.GetComponent<ItemSlotUI>();
            if (slot == null)
            {
                Debug.LogError("InventoryUIAutoBuilder: slotPrefab does not contain ItemSlotUI component.");
                continue;
            }
            slot.isEquipmentSlot = true;
            slot.slotType = type;

            if (slot.icon == null)
            {
                var icon = s.transform.Find("Icon")?.GetComponent<Image>();
                if (icon != null) slot.icon = icon;
            }

            equipSlotsUI.Add(slot);
        }

        GameObject bp = new GameObject("BackpackPanel", typeof(RectTransform));
        bp.transform.SetParent(rootPanel.transform, false);
        backpackPanel = bp.GetComponent<RectTransform>();
        backpackPanel.anchorMin = new Vector2(0.5f, 0f);
        backpackPanel.anchorMax = new Vector2(0.5f, 0f);
        backpackPanel.pivot = new Vector2(0.5f, 0f);

        float equipRowHeight = scaledEquipH + padding / 2f;
        backpackPanel.anchoredPosition = new Vector2(0f, - (equipRowHeight + 10f));

        GridLayoutGroup grid = bp.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(scaledSlot, scaledSlot);
        grid.spacing = new Vector2(scaledSpacing, scaledSpacing);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = backpackColumns;
        grid.childAlignment = TextAnchor.LowerCenter;
        grid.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);

        backpackSlotsUI.Clear();
        int total = backpackColumns * backpackRows;
        int index = 0;
        for (int r = 0; r < backpackRows; r++)
        {
            for (int c = 0; c < backpackColumns; c++)
            {
                GameObject s = Instantiate(slotPrefab, bp.transform);
                s.name = "BP_Slot_" + index;

                RectTransform srt = s.GetComponent<RectTransform>();
                if (srt != null) srt.sizeDelta = new Vector2(scaledSlot, scaledSlot);

                LayoutElement le = s.GetComponent<LayoutElement>() ?? s.AddComponent<LayoutElement>();
                le.preferredWidth = scaledSlot;
                le.preferredHeight = scaledSlot;
                le.minWidth = scaledSlot;
                le.minHeight = scaledSlot;
                le.flexibleWidth = 0;
                le.flexibleHeight = 0;

                ItemSlotUI slot = s.GetComponent<ItemSlotUI>();
                if (slot == null)
                {
                    Debug.LogError("InventoryUIAutoBuilder: slotPrefab does not contain ItemSlotUI component.");
                    continue;
                }

                slot.isEquipmentSlot = false;
                slot.slotIndex = index;

                if (slot.icon == null)
                {
                    var icon = s.transform.Find("Icon")?.GetComponent<Image>();
                    if (icon != null) slot.icon = icon;
                }

                backpackSlotsUI.Add(slot);
                index++;
            }
        }

        if (tooltipInstance == null)
            tooltipInstance = FindObjectOfType<TooltipUI>();

        Debug.Log("InventoryUIAutoBuilder: UI built. Backpack slots: " + backpackSlotsUI.Count + ", Equip slots: " + equipSlotsUI.Count);
    }

    public void RefreshAllSlots()
    {
        if (inventoryManager == null) return;

        for (int i = 0; i < backpackSlotsUI.Count; i++)
        {
            if (i < inventoryManager.backpackSlots.Count)
                backpackSlotsUI[i].Refresh(inventoryManager.backpackSlots[i]);
            else
                backpackSlotsUI[i].Refresh(new InventoryItem(null, 0));
        }

        for (int i = 0; i < equipSlotsUI.Count; i++)
        {
            ItemType t = equipmentOrder[i];
            if (inventoryManager.equipment.TryGetValue(t, out InventoryItem it))
                equipSlotsUI[i].Refresh(it);
            else
                equipSlotsUI[i].Refresh(new InventoryItem(null, 0));
        }
    }

    void ToggleInventory()
    {
        if (rootPanel == null) return;
        bool isOpen = rootPanel.activeSelf;
        if (isOpen) CloseInventory(); else OpenInventory();
    }

    void OpenInventory()
    {
        if (rootPanel == null) return;
        rootPanel.SetActive(true);
        if (player != null) player.SetControlsEnabled(false);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        RefreshAllSlots();
    }

    void CloseInventory()
    {
        if (rootPanel == null) return;
        rootPanel.SetActive(false);
        if (player != null) player.SetControlsEnabled(true);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
