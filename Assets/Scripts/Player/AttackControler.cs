using UnityEngine;

public class AttackController : MonoBehaviour
{
    public Animator anim;
    public float AttackCd = 0.5f;

    private bool CanAttack = true;

    void Update()
    {
        if (Input.GetButtonDown("Fire1") && CanAttack)
        {
            Attack();
        }
    }

    void Attack()
    {
        CanAttack = false;
        anim.SetTrigger("Attack");
        Invoke(nameof(ResetAttack), AttackCd);
    }

    void ResetAttack()
    {
        CanAttack = true;
    }
}
