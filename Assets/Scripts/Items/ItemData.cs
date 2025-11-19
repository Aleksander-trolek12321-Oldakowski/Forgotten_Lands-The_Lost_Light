using UnityEngine;

namespace Item
{
    public enum ItemType
    {
        None,
        Helmet,
        Chest,
        Legs,
        Boots,
        Weapon,
        Shield,
        Ring,
        Consumable,
        Misc
    }
    [CreateAssetMenu(fileName = "NewItem", menuName = "ItemData")]
    public class ItemData : ScriptableObject
    {
        public string itemName = "New Item";
        public Sprite itemSprite;
        public ItemType itemType = ItemType.Misc;

        [Header("Stats (applied when equipped)")]
        public float HP;
        public float Mana;
        public float Damage;
        public float Defense;
        public float Speed;

        [Header("Inventory")]
        public int stackSize = 1;

    }
}
