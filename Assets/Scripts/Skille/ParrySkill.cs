using UnityEngine;

[System.Serializable]
public class ParrySkill : Skill
{
    [Header("Input")]
    public KeyCode ParryKey = KeyCode.Mouse1;

    [Header("Block")]
    [Range(0f, 1f)]
    public float blockDamageMultiplier = 0.35f;

    [Header("Parry")]
    public float parryWindow = 0.25f;
    public float parryCooldown = 0.8f;

    private bool isBlocking;
    private float blockStartTime;
    private float lastParryTime = -999f;

    public bool IsBlocking => unlocked && isBlocking;

    public override void Activate()
    {
        if (!unlocked) return;

        if (Input.GetKeyDown(ParryKey))
        {
            isBlocking = true;
            blockStartTime = Time.time;
        }

        if (Input.GetKeyUp(ParryKey))
        {
            isBlocking = false;
        }
    }

    public float ModifyIncomingDamage(float damage)
    {
        if (!unlocked)
            return damage;

        if (!isBlocking)
            return damage;

        bool inParryWindow = Time.time <= blockStartTime + parryWindow;
        bool parryReady = Time.time >= lastParryTime + parryCooldown;

        if (inParryWindow && parryReady)
        {
            lastParryTime = Time.time;
            Debug.Log("PARRY!");
            return 0f;
        }

        Debug.Log("BLOCK!");
        return damage * blockDamageMultiplier;
    }
}