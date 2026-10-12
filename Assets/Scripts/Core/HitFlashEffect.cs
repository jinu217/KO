using System.Collections;
using UnityEngine;

// [피격 깜빡임 효과]
// - 플레이어/보스/적 등 모든 오브젝트에 공통 사용
// - 기존 InvincibleRoutine의 시각 효과만 분리한 독립 컴포넌트
public class HitFlashEffect : MonoBehaviour
{

    private SpriteRenderer[] renderers;
    private Coroutine flashRoutine;
    private bool isFlashing;

    public bool IsFlashing => isFlashing;

    private void OnDisable()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = null;
        isFlashing = false;
        SetVisible(true);
    }

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    // 지속 시간을 지정하지 않으면 플레이어 설정을 사용한다.
    public void Play()
    {
        if (GameManager.Instance == null) return;
        Play(GameManager.Instance.hitFlashDuration);
    }

    public void Play(float duration)
    {
        if (!isActiveAndEnabled || GameManager.Instance == null) return;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        duration = Mathf.Max(0f, duration);
        float interval = Mathf.Max(0.001f, GameManager.Instance.hitFlashInterval);
        isFlashing = duration > 0f;
        flashRoutine = StartCoroutine(FlashRoutine(duration, interval));
    }

    // 공통 설정의 간격으로 깜빡이고, 지정된 시간이 지나면 렌더러를 복구한다.
    private IEnumerator FlashRoutine(float duration, float interval)
    {
        float endTime = Time.time + duration;
        bool visible = true;

        while (Time.time < endTime)
        {
            visible = !visible;
            SetVisible(visible);
            yield return new WaitForSeconds(Mathf.Min(interval, endTime - Time.time));
        }

        SetVisible(true);
        isFlashing = false;
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
