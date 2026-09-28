using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

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

            //============== 코인 저장 ==============

            if (SaveData.Instance != null)
                SaveData.Instance.run.coin = coin;
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

    //============== 시작 시 데이터 불러오기 ==============

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
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

    //============== 아이템을 통한 재화 획득 ==============

    public void AddCurrency(ItemData item)
    {
        if (item == null)
            return;

        switch (item.currencyType)
        {
            case CurrencyType.Coin:

                AddCoin(item.currencyAmount);

                break;

            case CurrencyType.Soul:

                // Soul은 DeathScene 결과창에서 지급
                break;
        }
    }

    //============== 코인 직접 획득 ==============

    public void AddCoin(int amount)
    {
        if (amount <= 0)
            return;

        coin += amount;

        if (SaveData.Instance != null)
            SaveData.Instance.run.coin = coin;
    }

    //============== Soul 저장 ==============

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
            AddCoin(100);
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            soul += 1;
        }
    }*/
}