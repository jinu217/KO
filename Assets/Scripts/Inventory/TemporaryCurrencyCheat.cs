using UnityEngine;
using UnityEngine.InputSystem;

// 임시 테스트 기능: 제거할 때 이 파일과 .meta 파일만 삭제하면 됩니다.
public sealed class TemporaryCurrencyCheat : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<TemporaryCurrencyCheat>() != null)
            return;

        var host = new GameObject(nameof(TemporaryCurrencyCheat));
        host.AddComponent<TemporaryCurrencyCheat>();
        DontDestroyOnLoad(host);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null ||
            (!keyboard.digit0Key.wasPressedThisFrame && !keyboard.numpad0Key.wasPressedThisFrame))
            return;

        CurrencyManager currency = CurrencyManager.Instance;
        if (currency == null)
            return;

        currency.AddCoin(500);
        currency.soul += 500;
        currency.SaveSoul();
        Debug.Log("[테스트] 코인 +500, 소울 +500");
    }
}
