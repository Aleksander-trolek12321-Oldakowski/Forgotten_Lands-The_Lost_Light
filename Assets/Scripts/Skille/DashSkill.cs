using System.Collections;
using UnityEngine;
using Player;

[System.Serializable]
public class DashSkill : Skill
{
    public float dashForce = 10f;
    public float dashCooldown = 2f;
    public float dashDuration = 0.2f;

    private float lastUseTime = -999f;

    private Rigidbody rb;
    private Transform ownerTransform;
    private PlayerBase playerBase;

    public void Init(
        Rigidbody rb,
        Transform ownerTransform,
        PlayerBase playerBase)
    {
        this.rb = rb;
        this.ownerTransform = ownerTransform;
        this.playerBase = playerBase;
    }

    public override void Activate()
    {
        Debug.Log("Dash Activate");

        Debug.Log("Unlocked: " + unlocked);
        Debug.Log("RB: " + rb);
        Debug.Log("PlayerBase: " + playerBase);

        if (!unlocked) return;
        if (rb == null) return;
        if (playerBase == null) return;

        if (Time.time < lastUseTime + dashCooldown)
            return;

        lastUseTime = Time.time;

        Vector3 dashDirection =
            ownerTransform.forward.normalized;

        rb.velocity = new Vector3(
            0f,
            rb.velocity.y,
            0f);

        playerBase.SetControlsEnabled(false);

        rb.AddForce(
            dashDirection * dashForce,
            ForceMode.Impulse);

        playerBase.StartCoroutine(
            DashRoutine());
    }

    IEnumerator DashRoutine()
    {
        yield return new WaitForSeconds(
            dashDuration);

        playerBase.SetControlsEnabled(true);
    }
}