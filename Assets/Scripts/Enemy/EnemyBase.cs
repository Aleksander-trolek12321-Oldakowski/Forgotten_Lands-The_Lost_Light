using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    public float MaxHp = 20f;
    public float Attack = 5f;
    public float Defense = 2f;
    public float Speed = 3f;

    float currentHp;

    private void Start()
    {
        currentHp = MaxHp;
    }

    public void TakeDamage(float damage)
    {
        float reducedDamage = damage - Defense;

        float finalDamage = Mathf.Max(1f, reducedDamage);

        currentHp -= finalDamage;

        Debug.Log($"{name} took {finalDamage} damage");

        if (currentHp <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"{name} died");

        Destroy(gameObject);
    }
}