using System.Collections.Generic;
using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    [Header("Damage")]
    public float damage = 30f;

    [Header("State")]
    public bool hitboxActive = false;

    private HashSet<Collider> alreadyHit = new HashSet<Collider>();

    public void EnableHitbox()
    {
        hitboxActive = true;
        alreadyHit.Clear();
        Debug.Log("Hitbox ON");
    }

    public void DisableHitbox()
    {
        hitboxActive = false;
        Debug.Log("Hitbox OFF");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hitboxActive) return;

        if (alreadyHit.Contains(other)) return;

        alreadyHit.Add(other);

        EnemyBase enemy = other.GetComponentInParent<EnemyBase>();

        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Debug.Log($"Hit enemy: {other.name} for {damage}");
        }
    }
}