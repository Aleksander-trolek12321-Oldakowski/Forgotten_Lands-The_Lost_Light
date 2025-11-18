namespace Item
{
    [System.Serializable]
    public class InventoryItem
    {
        public ItemData data;
        public int quantity;

        public InventoryItem(ItemData d, int qty = 1)
        {
            data = d;
            quantity = qty;
        }

        public bool IsEmpty => data == null;
    }
}
