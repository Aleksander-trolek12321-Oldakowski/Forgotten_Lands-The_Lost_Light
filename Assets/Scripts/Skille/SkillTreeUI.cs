using UnityEngine;
using Inventory;
using Player;

public class SkillTreeUI : MonoBehaviour
{
    public GameObject skillTreePanel;
    public SkillTree skillTree;

    bool isOpen;
    float previousTimeScale = 1f;
    bool hasCapturedTimeScale = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
            Toggle();
    }

    void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    void Open()
    {
        if (isOpen) return;
        isOpen = true;

        if (skillTreePanel != null)
            skillTreePanel.SetActive(true);

        PlayerBase player = ResolvePlayer();
        InputBlocker.Block(player);

        if (!hasCapturedTimeScale)
        {
            previousTimeScale = Time.timeScale;
            hasCapturedTimeScale = true;
        }

        Time.timeScale = 0f;

        Debug.Log("SkillTree UI: OPEN");
    }

    void Close()
    {
        if (!isOpen) return;
        isOpen = false;

        if (skillTreePanel != null)
            skillTreePanel.SetActive(false);

        PlayerBase player = ResolvePlayer();
        InputBlocker.Restore(player);

        Time.timeScale = hasCapturedTimeScale ? previousTimeScale : 1f;
        hasCapturedTimeScale = false;

        Debug.Log("SkillTree UI: CLOSED");
    }

    PlayerBase ResolvePlayer()
    {
        if (skillTree != null && skillTree.playerBase != null)
            return skillTree.playerBase;

        if (skillTree == null)
            skillTree = FindObjectOfType<SkillTree>();

        if (skillTree != null && skillTree.playerBase != null)
            return skillTree.playerBase;

        return FindObjectOfType<PlayerBase>();
    }

    void OnDisable()
    {
        if (!isOpen)
            return;

        Close();
    }
}
