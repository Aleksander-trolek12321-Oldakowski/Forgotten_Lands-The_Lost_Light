using System.Collections;
using UnityEngine;
using Player;

[System.Serializable]
public class DashSkill : Skill
{
    public float dashForce = 1f;
    public float dashDistance = 4f;
    public float dashDuration = 0.18f;
    public float dashCooldown = 2f;
    public float manaCost = 20f;
    public float wallPadding = 0.05f;

    private float lastUseTime = -999f;

    private Rigidbody rb;
    private PlayerBase playerBase;
    private bool isDashing = false;

    public void Init(
        Rigidbody rb,
        PlayerBase playerBase)
    {
        this.rb = rb;
        this.playerBase = playerBase;
    }

    public override void Activate()
    {
        if (!unlocked) return;
        if (rb == null) return;
        if (playerBase == null) return;
        if (isDashing) return;

        if (Time.time < lastUseTime + dashCooldown)
            return;

        Transform dashTransform = rb.transform;
        Vector3 dashDirection = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) dashDirection += dashTransform.forward;
        if (Input.GetKey(KeyCode.S)) dashDirection -= dashTransform.forward;
        if (Input.GetKey(KeyCode.D)) dashDirection += dashTransform.right;
        if (Input.GetKey(KeyCode.A)) dashDirection -= dashTransform.right;

        if (dashDirection.sqrMagnitude < 0.0001f)
            dashDirection = -dashTransform.forward;

        dashDirection = Vector3.ProjectOnPlane(dashDirection, Vector3.up).normalized;

        if (dashDirection.sqrMagnitude < 0.0001f)
            return;

        float distance = Mathf.Max(0.01f, dashDistance);
        float forceMultiplier = Mathf.Max(0.1f, dashForce);
        float dashLength = Mathf.Max(2f, distance * forceMultiplier);
        float finalDistance = dashLength;
        if (Physics.Raycast(rb.position, dashDirection, out RaycastHit wallHit, dashLength, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (Mathf.Abs(wallHit.normal.y) <= 0.6f)
            {
                finalDistance = Mathf.Max(0f, wallHit.distance - Mathf.Max(0f, wallPadding));
            }
        }

        if (finalDistance <= 0f)
            return;

        if (!playerBase.TryUseMP(manaCost))
            return;

        lastUseTime = Time.time;
        playerBase.StartCoroutine(DashRoutine(dashDirection, finalDistance));
    }

    private IEnumerator DashRoutine(Vector3 dashDirection, float finalDistance)
    {
        isDashing = true;
        playerBase.SetControlsEnabled(false, false);

        Vector3 startPosition = rb.position;
        Vector3 endPosition = startPosition + dashDirection * finalDistance;
        float safeDuration = Mathf.Max(0.01f, dashDuration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            Vector3 nextPosition = Vector3.Lerp(startPosition, endPosition, easedT);

            rb.MovePosition(nextPosition);
            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(endPosition);
        rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
        playerBase.SetControlsEnabled(true);
        isDashing = false;
    }
}
