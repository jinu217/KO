using TMPro;
using UnityEngine;

public class CurrencyUI : MonoBehaviour
{
    public CurrencyManager currency;

    public TMP_Text coinText;

    public TMP_Text soulText;

    void Update()
    {
        coinText.text = currency.coin.ToString();

        if (SaveData.Instance != null)
            soulText.text = SaveData.Instance.persistent.soul.ToString();
    }
}