using System.Collections;
using UnityEngine;

// [함정 데미지 트리거 — 자식 오브젝트 전용]
// - Is Trigger ✓ Collider2D가 붙은 자식 오브젝트에 부착
// - 플레이어가 트리거 안에 있는 동안 tickInterval마다 TakeDamage 호출
// - 물리 콜라이더(루트)와 데미지 범위(이 컴포넌트)를 완전히 분리
[RequireComponent(typeof(Collider2D))]
public class TrapDamageTrigger : MonoBehaviour
{
    // Trap.cs에서 Init으로 주입받는 설정값
    private float damage;
    private float tickInterval = 0.5f;

    private bool playerInside;
    private Coroutine tickRoutine;
    private IDamageable cachedPlayer;   // 피격 대상 캐시

    // Trap 루트에서 데미지/틱 설정 주입
    public void Init(float dmg, float interval)
    {
        damage = dmg;
        tickInterval = Mathf.Max(0.05f, interval);
    }

    // 플레이어 진입: 틱 데미지 코루틴 시작
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target == null) return;

        cachedPlayer = target;
        playerInside = true;

        if (tickRoutine != null)
            StopCoroutine(tickRoutine);

        tickRoutine = StartCoroutine(TickRoutine());
    }

    // 플레이어 이탈: 틱 데미지 코루틴 중단
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;
        cachedPlayer = null;

        if (tickRoutine != null)
        {
            StopCoroutine(tickRoutine);
            tickRoutine = null;
        }
    }

    // 플레이어가 안에 있는 동안 tickInterval마다 데미지 적용
    private IEnumerator TickRoutine()
    {
        while (playerInside)
        {
            bool tutorialKnockback = TutorialManager.Instance != null && TutorialManager.Instance.IsActive;
            cachedPlayer?.TakeDamage(damage, transform.position, tutorialKnockback, false);

            yield return new WaitForSeconds(tickInterval);
        }

        tickRoutine = null;
    }

    private void OnDisable()
    {
        playerInside = false;
        cachedPlayer = null;

        if (tickRoutine != null)
        {
            StopCoroutine(tickRoutine);
            tickRoutine = null;
        }
    }
}
