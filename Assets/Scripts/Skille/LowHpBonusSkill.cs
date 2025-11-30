using Player;
using UnityEngine;

[System.Serializable]
public class LowHpBonusSkill : Skill
{
    public float extraMultiplier = 0.1f; 
    public float HpActivePercent = 0.3f;
    public PlayerBase PlayerBase;
     private bool bonusApplied = false;
    

    public void Init(PlayerBase playerBase)
    {
        this.PlayerBase = playerBase;
    }

    public override void ApplyPassiveEffect(GameObject owner)
    {
       
    }

    public void UpdateTick()
    {
        float hpPercent = PlayerBase.HpPercent;

        if (hpPercent >= HpActivePercent && !bonusApplied)
        {
            PlayerBase.DamageMultiplier += extraMultiplier;
            bonusApplied = true;
        }

        else if (hpPercent < HpActivePercent && bonusApplied)
        {
            PlayerBase.DamageMultiplier -= extraMultiplier;
            bonusApplied = false;
        }
    
    }
}