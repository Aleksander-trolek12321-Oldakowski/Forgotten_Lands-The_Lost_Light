using System.Collections;
using UnityEngine;

public class AttackController : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackCd = 0.6f;

    [Header("Hitbox Timing")]
    public float hitboxStart = 0.1f;
    public float hitboxEnd = 0.4f;

    [Header("References")]
    public AttackHitbox hitbox;

    private bool canAttack = true;
    private Animator anim;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (Input.GetButtonDown("Fire1") && canAttack)
        {
            Attack();
        }
    }

    void Attack()
    {
        canAttack = false;

        int attackIndex = Random.Range(0, 3);
        anim.SetInteger("AttackIndex", attackIndex);
        anim.SetTrigger("Attack");

        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        yield return new WaitForSeconds(hitboxStart);

        hitbox.EnableHitbox();

        yield return new WaitForSeconds(hitboxEnd - hitboxStart);

        hitbox.DisableHitbox();

        yield return new WaitForSeconds(attackCd);

        canAttack = true;
    }
}