using UnityEngine;
using UnityEngine.UI;

public class SkillButtonUI : MonoBehaviour
{
    public string skillId;               // "dash"
    [TextArea]
    public string skillDescription;      // opis skilla

    public SkillTree skillTree;
    public SkillInfoPanel infoPanel;

    Skill skill;

    void Start()
    {
        skill = GetSkillFromTree();
        if (skill == null)
        {
            Debug.LogError("SkillButtonUI: skill not found: " + skillId);
        }
    }

    Skill GetSkillFromTree()
    {
        foreach (var field in skillTree.GetType().GetFields())
        {
            if (typeof(Skill).IsAssignableFrom(field.FieldType))
            {
                Skill s = field.GetValue(skillTree) as Skill;
                if (s != null && s.skillId == skillId)
                    return s;
            }
        }
        return null;
    }

    public void OnClicked()
    {
        if (skill == null) return;

        Debug.Log("Selected skill: " + skill.skillName);
        infoPanel.ShowSkill(skill, skillDescription);
    }
}
