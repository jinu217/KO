using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public int coin;

    public int soul;

    void Start()
    {
        LoadSoul();
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
        PlayerPrefs.SetInt("SOUL", soul);
    }

    public void LoadSoul()
    {
        soul = PlayerPrefs.GetInt("SOUL", 0);
    }
    //테스트용 코드
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
    }
}