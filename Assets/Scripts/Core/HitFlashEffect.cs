using System.Collections;
using UnityEngine;

// [피격 깜빡임 효과]
// - 플레이어/보스/적 등 모든 오브젝트에 공통 사용
// - 기존 InvincibleRoutine의 시각 효과만 분리한 독립 컴포넌트
public class HitFlashEffect : MonoBehaviour
{
    [Header("깜빡임 설정")]
    [Tooltip("깜빡임 총 지속 시간(초)")]
    [SerializeField] private float flashDuration = 0.2f;
    [Tooltip("켜짐/꺼짐 전환 간격(초)")]
    [SerializeField] private float blinkInterval = 0.04f;

    private SpriteRenderer[] renderers;
    private Coroutine flashRoutine;

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    // 외부에서 호출 — duration 미지정 시 인스펙터 기본값 사용
    public void Play(float duration = -1f)
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine(duration < 0f ? flashDuration : duration));
    }

    // 깜빡임 루틴: duration 동안 blinkInterval 간격으로 켜기/끄기 반복 후 복구
    private IEnumerator FlashRoutine(float duration)
    {
        float timer = 0f;
        bool visible = true;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            visible = !visible;
            SetVisible(visible);
            yield return new WaitForSeconds(blinkInterval);
        }

        SetVisible(true);
        flashRoutine = null;
    }

    // 모든 SpriteRenderer enabled 상태 일괄 설정
    private void SetVisible(bool visible)
    {
        if (renderers == null) return;
        foreach (var sr in renderers)
            if (sr != null) sr.enabled = visible;
    }
}
