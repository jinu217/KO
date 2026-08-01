using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public int coin;

    public void AddCurrency(ItemData item)
    {
        if (item == null)
            return;

        switch (item.currencyType)
        {
            case CurrencyType.Coin:

                coin += item.currencyAmount;

                break;

            case CurrencyType.Soul:

                // Soul은 DeathScene 결과창에서 지급
                break;
        }
    }
}