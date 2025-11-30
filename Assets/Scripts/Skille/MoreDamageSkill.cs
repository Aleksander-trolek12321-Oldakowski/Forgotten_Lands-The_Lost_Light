using Player;
using UnityEngine;

[System.Serializable]
public class MoreDamageSkill : Skill
{
    public float extraMultiplier = 0.2f; 

    public override void ApplyPassiveEffect(GameObject owner)
    {
        var stats = owner.GetComponent<PlayerBase>();
        if (stats == null) return;

        stats.DamageMultiplier += extraMultiplier;
    }
}