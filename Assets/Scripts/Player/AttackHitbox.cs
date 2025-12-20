using System.Collections.Generic;
using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    [Header("Damage")]
    public float damage = 30f;

    [Header("Allowed Target Tag")]
    public string targetTag = "Enemy";   // Póki co na tagu

    [Header("State")]
    public bool hitboxActive = false;

    private HashSet<Collider> alreadyHit = new HashSet<Collider>();

    public void EnableHitbox()
    {
        hitboxActive = true;
        alreadyHit.Clear();  
    }

    public void DisableHitbox()
    {
        hitboxActive = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hitboxActive) return;

        if (!other.CompareTag(targetTag)) return;

        // Zapobieganie wielokrotnym trafieniom podczas jednego zamachu, ewentualnie można zmienić
        if (alreadyHit.Contains(other)) return;

        alreadyHit.Add(other);

        DoDmg dmg = other.GetComponentInParent<DoDmg>();
        if (dmg != null)
        {
            dmg.TakeDmg(damage);
            Debug.Log($"Trafiono obiekt: {other.name} tag: {other.tag} za {damage} dmg.");
        }
        else
        {
            Debug.Log($"Trafiono tag {targetTag}, ale nie ma DoDmg: {other.name}");
        }
    }
}
