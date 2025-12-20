using UnityEngine;

public class EnemyHP : MonoBehaviour, DoDmg
{
    public float hp = 10f;

    public void TakeDmg(float dmg)
    {
        hp -= dmg;

        Debug.Log($"Enemy HP: {hp}");

        if (hp <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Killed");
        Destroy(gameObject);
    }
}