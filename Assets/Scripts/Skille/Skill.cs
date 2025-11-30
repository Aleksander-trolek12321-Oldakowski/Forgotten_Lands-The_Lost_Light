using UnityEngine;

public enum SkillType
{
    Active,
    Passive
}

[System.Serializable]
public class Skill
{
    public string skillId;          
    public string skillName;
    public SkillType type;
    public bool unlocked;
    public Skill[] requires;   

    public virtual void Activate()
    {
        
    }

    public virtual void ApplyPassiveEffect(GameObject owner)
    {
        
    }

    public bool CanUnlock()
    {
        if (unlocked) return false;

        if (requires == null || requires.Length == 0)
            return true;

        foreach (var s in requires)
        {
            if (s == null || !s.unlocked)
                return false;
        }

        return true;
    }

    public void Unlock(GameObject owner)
    {
        if (unlocked) return;

        unlocked = true;

        if (type == SkillType.Passive)
        {
            ApplyPassiveEffect(owner);
        }
    }
}
