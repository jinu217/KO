using UnityEngine;

public class PlayerItemManager : MonoBehaviour
{
    public Inventory inventory;

    public PotionBag potionBag;

    public CurrencyManager currencyManager;

    public void AddItem(ItemData item)
    {
        switch (item.itemType)
        {
            // 장비
            case ItemType.Equipment:

                inventory.AddItem(item);

                Debug.Log("장비 획득 : " + item.itemName);

                break;

            // 포션
            case ItemType.Potion:

                potionBag.AddPotion(item);

                Debug.Log("포션 획득 : " + item.itemName);

                break;

            // 재화
            case ItemType.Currency:

                currencyManager.AddCurrency(item);

                Debug.Log("재화 획득 : " + item.itemName);

                break;
        }
    }
}