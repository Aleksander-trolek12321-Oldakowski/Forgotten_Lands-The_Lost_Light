using System.Collections.Generic;
using System.Reflection;
using GameSave;
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

    [Header("Unlocked Skill UI Images")]
    public GameObject[] unlockedSkillImages;

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

        SyncLocalSkillPointsFromPlayer();
        RefreshUnlockedSkillImages();
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
            if (playerBase != null)
                playerBase.AddSkillPoints(1);
            else
                skillPoints++;

            Debug.Log(
                "Added skill point: " +
                GetAvailableSkillPoints());
        }
    }

    public bool TryUnlockSkill(Skill skill)
    {
        if (skill == null)
            return false;

        if (!skill.CanUnlock())
            return false;

        if (GetAvailableSkillPoints() <= 0)
        {
            Debug.Log("No skill points");

            return false;
        }

        if (!TryConsumeSkillPoint())
            return false;

        skill.Unlock(gameObject);

        Debug.Log(
            "Unlocked: " +
            skill.skillName);

        RefreshUnlockedSkillImages();

        return true;
    }

    public SavedSkillTreeState CreateSaveSnapshot()
    {
        SavedSkillTreeState state = new SavedSkillTreeState
        {
            skillPoints = GetAvailableSkillPoints(),
            skills = new List<SavedSkillState>()
        };

        List<Skill> skills = GetAllSkills();
        for (int i = 0; i < skills.Count; i++)
        {
            Skill skill = skills[i];
            string id = GetSkillSaveId(skill);
            if (string.IsNullOrWhiteSpace(id))
                continue;

            state.skills.Add(new SavedSkillState
            {
                skillId = id,
                unlocked = skill.unlocked
            });
        }

        return state;
    }

    public void ApplySaveSnapshot(SavedSkillTreeState state)
    {
        if (state == null)
            return;

        bool hasSkillEntries = state.skills != null && state.skills.Count > 0;
        if (!hasSkillEntries && state.skillPoints == 0)
            return;

        SetAvailableSkillPoints(state.skillPoints);

        Dictionary<string, bool> unlockedById = new Dictionary<string, bool>();
        if (state.skills != null)
        {
            for (int i = 0; i < state.skills.Count; i++)
            {
                SavedSkillState saved = state.skills[i];
                if (saved == null || string.IsNullOrWhiteSpace(saved.skillId))
                    continue;

                unlockedById[saved.skillId] = saved.unlocked;
            }
        }

        List<Skill> skills = GetAllSkills();
        for (int i = 0; i < skills.Count; i++)
        {
            Skill skill = skills[i];
            string id = GetSkillSaveId(skill);
            if (string.IsNullOrWhiteSpace(id))
                continue;

            if (unlockedById.TryGetValue(id, out bool unlocked))
            {
                skill.unlocked = unlocked;
            }
        }

        RefreshUnlockedSkillImages();
    }

    private int GetAvailableSkillPoints()
    {
        return playerBase != null ? playerBase.SkillPoints : skillPoints;
    }

    private void SetAvailableSkillPoints(int points)
    {
        points = Mathf.Max(0, points);

        if (playerBase != null)
        {
            int current = playerBase.SkillPoints;
            if (points > current)
            {
                playerBase.AddSkillPoints(points - current);
            }
            else
            {
                int toConsume = current - points;
                for (int i = 0; i < toConsume; i++)
                {
                    if (!playerBase.TryConsumeSkillPoint())
                        break;
                }
            }
        }

        skillPoints = points;
    }

    private bool TryConsumeSkillPoint()
    {
        if (playerBase != null)
        {
            if (!playerBase.TryConsumeSkillPoint())
                return false;

            skillPoints = playerBase.SkillPoints;
            return true;
        }

        if (skillPoints <= 0)
            return false;

        skillPoints--;
        return true;
    }

    private void SyncLocalSkillPointsFromPlayer()
    {
        if (playerBase != null)
            skillPoints = playerBase.SkillPoints;
    }

    private List<Skill> GetAllSkills()
    {
        List<Skill> result = new List<Skill>();
        FieldInfo[] fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo field = fields[i];
            if (!typeof(Skill).IsAssignableFrom(field.FieldType))
                continue;

            Skill skill = field.GetValue(this) as Skill;
            if (skill == null)
                continue;

            if (!result.Contains(skill))
                result.Add(skill);
        }

        return result;
    }

    private string GetSkillSaveId(Skill skill)
    {
        if (skill == null)
            return "";

        if (!string.IsNullOrWhiteSpace(skill.skillId))
            return skill.skillId;

        return skill.skillName ?? "";
    }

    private void RefreshUnlockedSkillImages()
    {
        if (unlockedSkillImages == null || unlockedSkillImages.Length == 0)
            return;

        int unlockedCount = 0;
        List<Skill> skills = GetAllSkills();
        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] != null && skills[i].unlocked)
                unlockedCount++;
        }

        for (int i = 0; i < unlockedSkillImages.Length; i++)
        {
            GameObject imageObject = unlockedSkillImages[i];
            if (imageObject == null)
                continue;

            imageObject.SetActive(i < unlockedCount);
        }
    }
}
