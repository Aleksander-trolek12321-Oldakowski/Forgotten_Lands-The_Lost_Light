using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBarUI : MonoBehaviour
{
    [Header("Root")]
    public GameObject root;

    [Header("UI")]
    public Image healthFill;
    public TextMeshProUGUI bossNameText;

    private EnemyBase trackedBoss;

    private void Awake()
    {
        Hide();
    }

    private void Update()
    {
        if (trackedBoss == null || trackedBoss.IsDead)
        {
            Hide();
            return;
        }

        Refresh();
    }

    public void Show(EnemyBase boss, string displayName)
    {
        trackedBoss = boss;

        if (root != null)
            root.SetActive(true);

        if (bossNameText != null)
            bossNameText.text = string.IsNullOrWhiteSpace(displayName) ? "BOSS" : displayName;

        Refresh();
    }

    public void Hide()
    {
        trackedBoss = null;

        if (root != null)
            root.SetActive(false);
    }

    private void Refresh()
    {
        if (trackedBoss == null) return;
        if (healthFill == null) return;

        float maxHp = Mathf.Max(1f, trackedBoss.MaxHp);
        healthFill.fillAmount = Mathf.Clamp01(trackedBoss.CurrentHp / maxHp);
    }
}
