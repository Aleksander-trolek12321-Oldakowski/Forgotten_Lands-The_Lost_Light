using UnityEngine;
using Player;

public class EnemyAttack : MonoBehaviour
{
    public EnemyBase enemyStats;
    public float attackRange = 2f;
    public LayerMask playerLayer;

    public void DealDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, playerLayer);

        foreach (Collider hit in hits)
        {
            PlayerBase player = hit.GetComponent<PlayerBase>();

            if (player != null)
            {
                player.TakeDMG(enemyStats.attackDamage);
            }
        }
    }
}