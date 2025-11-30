using Player;
using UnityEngine;

public class SkillTree : MonoBehaviour
{
    public int skillPoints = 1;
    public PlayerBase playerBase;

    [Header("Active Skill")]
    public DashSkill dashSkill;
    public BerserkSkill berserkSkill;

    [Header("Passive Skill")]
    public MoreDamageSkill damageSkill;
    public InstantRegenSkill instantRegenSkill;
    public LowHpBonusSkill lowHpBonusSkill;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (dashSkill != null)
        {
            dashSkill.Init(rb, transform); 
        }

        if (damageSkill != null && dashSkill != null)
        {
            damageSkill.requires = new Skill[] { dashSkill };
        }

        if (instantRegenSkill != null)
        {
            instantRegenSkill.Init(playerBase);
        }
            

        if (berserkSkill != null)
        {
            berserkSkill.Init(playerBase, this);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && dashSkill != null)
        {
            dashSkill.Activate(); 
        }

        if (instantRegenSkill != null && instantRegenSkill.unlocked)
        {
            instantRegenSkill.UpdateTick();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (berserkSkill != null)
                berserkSkill.Activate();
        }
    }

    public bool TryUnlockSkill(Skill skill)
    {
        if (skill == null) return false;
        if (!skill.CanUnlock()) return false;
        if (skillPoints <= 0) return false;

        skillPoints--;
        skill.Unlock(gameObject);
        return true;
    }
}
