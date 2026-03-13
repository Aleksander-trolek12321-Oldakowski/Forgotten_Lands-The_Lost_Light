using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SkillInfoPanel : MonoBehaviour
{
    public SkillTree skillTree;
    public TextMeshProUGUI skillNameText;
    public TextMeshProUGUI skillDescriptionText;
    public Button unlockButton;

    Skill currentSkill;

    public void Init(SkillTree tree)
    {
        skillTree = tree;
        Clear();
    }

    public void ShowSkill(Skill skill, string description)
    {
        currentSkill = skill;

        skillNameText.text = skill.skillName;
        skillDescriptionText.text = description;

        unlockButton.interactable = !skill.unlocked && skill.CanUnlock();
    }

    public void OnUnlockClicked()
    {
        
        if (currentSkill == null) return;

        if (skillTree.TryUnlockSkill(currentSkill))
        {
            Debug.Log($"Unlocked skill: {currentSkill.skillName}");
            unlockButton.interactable = false;
        }
    }

    void Clear()
    {
        skillNameText.text = "";
        skillDescriptionText.text = "";
        unlockButton.interactable = false;
    }
}
