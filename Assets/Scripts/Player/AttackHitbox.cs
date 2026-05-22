using System.Collections.Generic;
using UnityEngine;
using System;

public class AttackHitbox : MonoBehaviour
{
    public static event Action<Vector3, Vector3> AttackWindowOpened;
    public static float LastAttackWindowOpenTime { get; private set; } = -999f;
    public static Vector3 LastAttackOrigin { get; private set; }
    public static Vector3 LastAttackForward { get; private set; }

    [Header("Damage")]
    public float damage = 30f;

    [Header("State")]
    public bool hitboxActive = false;

    private HashSet<Collider> alreadyHit = new HashSet<Collider>();
    private Player.PlayerBase ownerPlayer;

    private void Awake()
    {
        ownerPlayer = GetComponentInParent<Player.PlayerBase>();
    }

    public void EnableHitbox()
    {
        hitboxActive = true;
        alreadyHit.Clear();

        LastAttackWindowOpenTime = Time.time;
        LastAttackOrigin = transform.position;
        LastAttackForward = transform.forward;
        AttackWindowOpened?.Invoke(LastAttackOrigin, LastAttackForward);

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
            if (ownerPlayer == null)
                ownerPlayer = GetComponentInParent<Player.PlayerBase>();

            if (ownerPlayer != null)
                ownerPlayer.RegisterCombatActivity();

            enemy.TakeDamage(damage);
            Debug.Log($"Hit enemy: {other.name} for {damage}");
        }
    }
}
