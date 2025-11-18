using UnityEngine;
using UnityEngine.UI;
using Item;
using System.Text;

public class TooltipUI : MonoBehaviour
{
    public GameObject panel;
    public Text titleText;
    public Text statsText;
    public Vector2 offset = new Vector2(0f, 8f);

    private RectTransform panelRect;
    private Canvas canvas;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        panelRect = panel != null ? panel.GetComponent<RectTransform>() : null;
        canvas = GetComponentInParent<Canvas>();
    }

    public void Show(ItemData data, Vector2 screenPosition)
    {
        if (data == null || panel == null) return;
        panel.SetActive(true);

        titleText.text = data.itemName ?? "Item";

        StringBuilder sb = new StringBuilder();
        if (Mathf.Abs(data.HP) > 0.0001f) sb.AppendLine($"HP: {data.HP}");
        if (Mathf.Abs(data.Mana) > 0.0001f) sb.AppendLine($"Mana: {data.Mana}");
        if (Mathf.Abs(data.Damage) > 0.0001f) sb.AppendLine($"DMG: {data.Damage}");
        if (Mathf.Abs(data.Defense) > 0.0001f) sb.AppendLine($"DEF: {data.Defense}");
        if (Mathf.Abs(data.Speed) > 0.0001f) sb.AppendLine($"SPD: {data.Speed}");

        statsText.text = sb.Length > 0 ? sb.ToString().TrimEnd() : "No stats";

        if (canvas != null && panelRect != null)
        {
            RectTransform canvasRect = canvas.transform as RectTransform;
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition + offset, canvas.worldCamera, out localPoint);
            panelRect.localPosition = localPoint;
        }
        else if (panelRect != null)
        {
            panelRect.position = screenPosition + offset;
        }
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }
}
