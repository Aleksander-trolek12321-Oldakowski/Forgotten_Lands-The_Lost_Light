using UnityEngine;
using Player;

[System.Serializable]
public class InstantRegenSkill : Skill
{
    public float MinHpPercent = 0.15f;
    public float HealAmount = 25f;

    public float cooldown = 180f;

    private PlayerBase playerBase;
    private float lastUseTime = -1f;

    private float previousHpPercent = 1f;

    public void Init(PlayerBase playerBase)
    {
        this.playerBase = playerBase;
        
        if (playerBase != null)
        {
            previousHpPercent = playerBase.HpPercent;
        }
    }

    public override void ApplyPassiveEffect(GameObject owner)
    {
        
    }

    public void UpdateTick()
    {
        if (!unlocked || playerBase == null) return;

        float currentHpPercent = playerBase.HpPercent;

        bool offCooldown = Time.time >= lastUseTime + cooldown;

        bool justDroppedBelowThreshold = previousHpPercent >= MinHpPercent && currentHpPercent < MinHpPercent;

        if (offCooldown && justDroppedBelowThreshold)
        {
            playerBase.Heal(HealAmount);
            lastUseTime = Time.time;
        }

        previousHpPercent = currentHpPercent;
    }
}
