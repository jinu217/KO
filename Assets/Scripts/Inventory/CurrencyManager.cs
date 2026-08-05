using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public int coin;

    public int soul;

    public bool CanAfford(CurrencyType currencyType, int amount)
    {
        if (amount < 0)
            return false;

        return currencyType == CurrencyType.Coin
            ? coin >= amount
            : soul >= amount;
    }

    public bool TrySpend(CurrencyType currencyType, int amount)
    {
        if (!CanAfford(currencyType, amount))
            return false;

        if (currencyType == CurrencyType.Coin)
        {
            coin -= amount;
        }
        else
        {
            soul -= amount;
            SaveSoul();

            if (SaveData.Instance != null)
                SaveData.Instance.persistent.soul = soul;
        }

        return true;
    }

    void Start()
    {
        LoadSoul();

        if (SaveData.Instance != null && !SaveData.Instance.run.initialized)
        {
            coin = Mathf.Max(0, SaveData.Instance.persistent.startingCoinBonus);
            SaveData.Instance.run.coin = coin;
        }
    }

    public void AddCurrency(ItemData item)
    {
        switch (item.currencyType)
        {
            case CurrencyType.Coin:

                coin += item.currencyAmount;

                break;

            case CurrencyType.Soul:

                soul += item.currencyAmount;

                SaveSoul();

                break;
        }
    }

    public void SaveSoul()
    {
        if (SaveData.Instance != null)
            SaveData.Instance.persistent.soul = soul;

        PlayerPrefs.SetInt("SOUL", soul);
    }

    public void LoadSoul()
    {
        soul = SaveData.Instance != null
            ? SaveData.Instance.persistent.soul
            : PlayerPrefs.GetInt("SOUL", 0);
    }
    //테스트용 코드
    /*
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            coin += 100;
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            soul += 1;
        }
    }*/
}
