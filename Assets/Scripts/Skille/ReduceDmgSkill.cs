using Player;
using UnityEngine;

[System.Serializable]
public class ReduceDmgSkill : Skill
{
    public float extraMultiplier = 0.2f; 

    public override void ApplyPassiveEffect(GameObject owner)
    {
        var stats = owner.GetComponent<PlayerBase>();
        if (stats == null) return;

        stats.PercentDmgTaken -= extraMultiplier;
    }
}