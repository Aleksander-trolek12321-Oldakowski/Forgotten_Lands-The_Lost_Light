using UnityEngine;

public class AttackController : MonoBehaviour
{

    public float attackCd = 0.5f;

    private bool canAttack = true;
    private Animator anim;

void Start()
    {
    anim = GetComponent<Animator>();
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
        anim.SetTrigger("Attack");
        Invoke(nameof(ResetAttack), attackCd);
    }

    void ResetAttack()
    {
        canAttack = true;
    }
}