using UnityEngine;

public class HitboxForwarder : MonoBehaviour
{
    public AttackHitbox hitbox;

    public void EnableHitbox()
    {
        hitbox.EnableHitbox();
    }

    public void DisableHitbox()
    {
        hitbox.DisableHitbox();
    }
}