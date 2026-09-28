using UnityEngine;

public class CoinReward : MonoBehaviour
{
    [Header("코인 보상")]
    [SerializeField] private int coinAmount = 5;

    private bool rewarded = false;

    //============== 코인 지급 ==============

    public void GiveCoin()
    {
        if (rewarded)
            return;

        if (coinAmount <= 0)
            return;

        if (CurrencyManager.Instance == null)
        {
            Debug.LogWarning(
                $"[{name}] CurrencyManager를 찾을 수 없어 코인을 지급할 수 없습니다."
            );

            return;
        }

        rewarded = true;

        CurrencyManager.Instance.AddCoin(coinAmount);

        Debug.Log($"[{name}] 코인 획득: +{coinAmount}");
    }
}