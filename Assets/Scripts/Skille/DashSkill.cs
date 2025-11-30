using UnityEngine;

[System.Serializable]
public class DashSkill : Skill
{
    public float dashForce = 10f;
    public float dashCooldown = 2f;

    private float lastUseTime = -1f;
    private Transform ownerTranform;

    private Rigidbody rb; 

    public void Init(Rigidbody rb, Transform ownerTranform)
    {
        this.rb = rb;
        this.ownerTranform = ownerTranform;
    }

    public override void Activate()
    {
        if (!unlocked) return;
        if (rb == null) return;

        if (Time.time < lastUseTime + dashCooldown) return;

        lastUseTime = Time.time;

        Vector3 backDirection = -ownerTranform.forward;
        rb.AddForce(backDirection * dashForce, ForceMode.Impulse);
    }
}
