using UnityEngine;

// [파괴 가능 오브젝트]
// - hitCountToBreak 회 타격 시 SetActive(false)로 비활성화 (재사용 가능)
// - 물리 설정은 인스펙터에서 직접: Rigidbody2D Kinematic, Constraints는 직접 지정
//
// ▼ Unity 설정 필수 ▼
// 1. BoxCollider2D(Is Trigger ✗) 추가 → 플레이어/몬스터 통과 차단
// 2. Rigidbody2D → Body Type: Kinematic (Constraints는 인스펙터에서 직접 설정)
// 3. Edit → Project Settings → Physics 2D → Layer Collision Matrix에서
//    이 오브젝트 레이어 ↔ Player, Enemy 충돌 활성화
public class BreakableObject : MonoBehaviour
{
    [Header("내구도")]
    [Tooltip("파괴까지 필요한 타격 횟수")]
    [SerializeField] private int hitCountToBreak = 3;

    [Header("디버그")]
    [SerializeField] private bool showDebugLog = true;

    private int currentHits;
    private bool isBroken;

    // PlayerController.Attack()에서 자동 호출 (레이어/태그 무관)
    public void TakeDamage()
    {
        if (isBroken) return;

        currentHits++;

        if (showDebugLog)
            Debug.Log($"[{name}] 타격 {currentHits}/{hitCountToBreak}");

        if (currentHits >= hitCountToBreak)
            Break();
    }

    // 파괴: SetActive(false)로 비활성화 → 재활성화 후 ResetObject()로 재사용 가능
    private void Break()
    {
        isBroken = true;

        // 코인 지급
        CoinReward coinReward = GetComponent<CoinReward>();

        if (coinReward != null)
            coinReward.GiveCoin();

        if (showDebugLog)
            Debug.Log($"[{name}] 파괴 → 코인 지급 후 SetActive(false)");

        gameObject.SetActive(false);
    }

    // 재활성화 후 상태 초기화 (외부에서 SetActive(true) 뒤에 호출)
    public void ResetObject()
    {
        currentHits = 0;
        isBroken = false;
    }
}