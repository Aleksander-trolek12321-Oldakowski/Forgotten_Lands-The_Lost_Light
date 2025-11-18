using UnityEngine;

namespace Item
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "ItemData")]
    public class ItemData : ScriptableObject
    {
        public Sprite itemSprite;
        public float HP;
        public float Mana;
        public float Damage;
        public float Defense;
        public float Speed;

    }
}
