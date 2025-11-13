using UnityEngine;

public class MpPotion : MonoBehaviour
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
            player.UseMpPotion();
        }
        else
        {
            Debug.LogWarning("Player not found in scene!");
        }
    }
}