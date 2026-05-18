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

        if (playerBase == null)
        {
            playerBase = GetComponent<PlayerBase>();
        }

        if (dashSkill != null)
        {
            dashSkill.Init(
                rb,
                transform,
                playerBase
            );
        }

        if (damageSkill != null && dashSkill != null)
        {
            damageSkill.requires =
                new Skill[] { dashSkill };
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
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            Debug.Log("SHIFT");

            if (dashSkill != null)
            {
                Debug.Log("Dash exists");

                dashSkill.Activate();
            }
        }

        if (instantRegenSkill != null &&
            instantRegenSkill.unlocked)
        {
            instantRegenSkill.UpdateTick();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (berserkSkill != null)
            {
                berserkSkill.Activate();
            }
        }

        // TEST skill pointów
        if (Input.GetKeyDown(KeyCode.P))
        {
            skillPoints++;

            Debug.Log(
                "Added skill point: " +
                skillPoints);
        }
    }

    public bool TryUnlockSkill(Skill skill)
    {
        if (skill == null)
            return false;

        if (!skill.CanUnlock())
            return false;

        if (skillPoints <= 0)
        {
            Debug.Log("No skill points");

            return false;
        }

        skillPoints--;

        skill.Unlock(gameObject);

        Debug.Log(
            "Unlocked: " +
            skill.skillName);

        return true;
    }
}