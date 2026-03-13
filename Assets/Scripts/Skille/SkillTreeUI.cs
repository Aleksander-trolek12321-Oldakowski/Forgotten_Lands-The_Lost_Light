using UnityEngine;

public class SkillTreeUI : MonoBehaviour
{
    public GameObject skillTreePanel;
    public SkillTree skillTree;

    bool isOpen;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
            Toggle();
    }

    void Toggle()
    {
        isOpen = !isOpen;
        skillTreePanel.SetActive(isOpen);

        Cursor.visible = isOpen;
        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;

        skillTree.playerBase.SetControlsEnabled(!isOpen);

        Debug.Log("SkillTree UI: " + (isOpen ? "OPEN" : "CLOSED"));
    }
}
