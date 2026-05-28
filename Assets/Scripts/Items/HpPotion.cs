using UnityEngine;
using Player;
using UnityEngine.UI;

namespace potions
{
    public class HpPotion : MonoBehaviour
    {
        [SerializeField] private Image potionIcon;
        private PlayerBase player;

        private void Start()
        {
            if (potionIcon == null)
                potionIcon = GetComponent<Image>();

            player = FindFirstObjectByType<PlayerBase>();
            if (player != null && potionIcon != null)
                player.RegisterHpPotionIcon(potionIcon);
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
}
