using UnityEngine;

public class HpPotion : MonoBehaviour
{
    private PlayerBase player;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerBase>();
    }

    public void OnLeftClick()
    {
        if (player != null)
        {
            player.UseHpPotion();
        }
        else
        {
            Debug.LogWarning("Player not found in scene!");
        }
    }
}