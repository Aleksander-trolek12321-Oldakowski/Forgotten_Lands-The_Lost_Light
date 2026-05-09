using UnityEngine;
using UnityEngine.UI;
using Item;
using System.Text;

namespace Inventory
{
    public class TooltipUI : MonoBehaviour
    {
        public GameObject panel;
        public Text titleText;
        public Text statsText;
        [Header("Position")]
        [Tooltip("If enabled, tooltip will be positioned near hovered slot. If disabled, it stays where you place it in UI.")]
        public bool followHoveredSlot = false;
        public Vector2 offset = new Vector2(0f, 8f);

        private RectTransform panelRect;
        private Canvas canvas;
        private CanvasGroup panelCanvasGroup;
        private bool initialized;

        private void Awake()
        {
            InitializeIfNeeded();
            Hide();
        }

        private void OnEnable()
        {
            // In case the tooltip gets enabled later (e.g., inventory UI toggled).
            InitializeIfNeeded();
            Hide();
        }

        private void InitializeIfNeeded()
        {
            if (initialized) return;
            initialized = true;

            if (panel == null) panel = gameObject;

            panelRect = panel != null ? panel.GetComponent<RectTransform>() : null;
            canvas = GetComponentInParent<Canvas>();

            // Prevent tooltip from stealing pointer raycasts (causes hover flicker on slots).
            if (panel != null)
            {
                panelCanvasGroup = panel.GetComponent<CanvasGroup>() ?? panel.AddComponent<CanvasGroup>();
                panelCanvasGroup.blocksRaycasts = false;
                panelCanvasGroup.interactable = false;

                foreach (var g in panel.GetComponentsInChildren<Graphic>(true))
                    g.raycastTarget = false;
            }

            if (titleText == null)
                titleText = GetComponentInChildren<Text>(true);

            if (statsText == null)
            {
                var allTexts = GetComponentsInChildren<Text>(true);
                if (allTexts != null && allTexts.Length > 1)
                    statsText = allTexts[allTexts.Length - 1];
            }
        }

        public void Show(ItemData data, Vector2 screenPosition, float buyPrice = -1f, float sellPrice = -1f)
        {
            InitializeIfNeeded();
            if (data == null || panel == null) return;

            if (!panel.activeSelf) panel.SetActive(true);
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1f;

            if (titleText != null) titleText.text = data.itemName ?? "Item";

            StringBuilder sb = new StringBuilder();
            if (Mathf.Abs(data.HP) > 0.0001f) sb.AppendLine($"HP: {data.HP}");
            if (Mathf.Abs(data.Mana) > 0.0001f) sb.AppendLine($"Mana: {data.Mana}");
            if (Mathf.Abs(data.Damage) > 0.0001f) sb.AppendLine($"DMG: {data.Damage}");
            if (Mathf.Abs(data.Defense) > 0.0001f) sb.AppendLine($"DEF: {data.Defense}");
            if (Mathf.Abs(data.Speed) > 0.0001f) sb.AppendLine($"SPD: {data.Speed}");

            if (buyPrice >= 0f)
            {
                if (sellPrice < 0f)
                {
                    sellPrice = Mathf.Round((buyPrice / 3f) * 10f) / 10f;
                }

                if (sb.Length > 0) sb.AppendLine();

                sb.AppendLine($"Buy: {buyPrice:F1}");
                sb.AppendLine($"Sell: {sellPrice:F1}");
            }

            if (statsText != null)
                statsText.text = sb.Length > 0 ? sb.ToString().TrimEnd() : "No stats";

            if (followHoveredSlot && canvas != null && panelRect != null)
            {
                RectTransform canvasRect = canvas.transform as RectTransform;
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition + offset, canvas.worldCamera, out localPoint);
                panelRect.localPosition = localPoint;
            }
            else if (followHoveredSlot && panelRect != null)
            {
                panelRect.position = screenPosition + offset;
            }
        }

        public void Hide()
        {
            InitializeIfNeeded();
            if (panel == null) return;

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                panelCanvasGroup.blocksRaycasts = false;
                panelCanvasGroup.interactable = false;
            }

            if (panel.activeSelf) panel.SetActive(false);
        }
    }
}
