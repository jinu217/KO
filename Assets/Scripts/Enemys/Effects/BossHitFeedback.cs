using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class BossHitFeedback : MonoBehaviour
{
    //============== 설정값 ==============
    [Header("피격 흰색 깜빡임")]
    [SerializeField, Min(0.02f)] private float flashDuration = 0.14f;   // 깜빡임 전체 시간
    [SerializeField, Min(1)] private int flashCount = 1;                // 깜빡이는 횟수
    [SerializeField, Tooltip("비워두면 내장 셰이더로 흰색 머티리얼을 자동 생성합니다. 직접 만든 머티리얼을 쓰고 싶을 때만 연결하세요.")]
    private Material flashMaterialOverride;

    [Header("히트스톱 (보스 애니메이션만 잠깐 멈춤)")]
    [SerializeField] private bool useHitStop = true;
    [SerializeField, Min(0f)] private float hitStopDuration = 0.04f;

    [Header("연결 (비워두면 자동으로 찾음)")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer[] spriteRenderers;
    //============== 설정값 끝 ==============

    //============== 내부 변수 ==============
    private Material runtimeFlashMaterial;   // 코드로 만든 흰색 머티리얼
    private Material[] originalMaterials;
    private Color[] originalColors;
    private float savedAnimSpeed = 1f;
    private bool animFrozen;
    private bool flashShown;
    private Coroutine feedbackRoutine;
    //============== 내부 변수 끝 ==============

    private Material FlashMaterial => flashMaterialOverride != null ? flashMaterialOverride : runtimeFlashMaterial;

    //============== 초기화 ==============
    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (spriteRenderers == null || spriteRenderers.Length == 0)
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        originalMaterials = new Material[spriteRenderers.Length];
        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null) continue;
            originalMaterials[i] = spriteRenderers[i].sharedMaterial;
            originalColors[i] = spriteRenderers[i].color;
        }

        // 흰색 머티리얼 자동 생성 (GUI/Text Shader: 스프라이트 알파 + 지정 색)
        if (flashMaterialOverride == null)
        {
            Shader shader = Shader.Find("GUI/Text Shader");
            if (shader != null)
            {
                runtimeFlashMaterial = new Material(shader);
                runtimeFlashMaterial.color = Color.white;
            }
            else
            {
                Debug.LogWarning($"[{name}] 'GUI/Text Shader'를 찾지 못했습니다. Flash Material Override를 직접 연결하세요.", this);
            }
        }
    }

    private void OnDestroy()
    {
        if (runtimeFlashMaterial != null)
            Destroy(runtimeFlashMaterial);
    }
    //============== 초기화 끝 ==============

    //============== 외부 호출 ==============
    // allowHitStop: 공격 중/페이즈 진입 중에는 false로 넘겨 히트스톱만 건너뜁니다.
    public void Play(bool allowHitStop = true)
    {
        if (!isActiveAndEnabled) return;

        // 연속으로 맞으면 이전 연출을 끊고 처음부터 다시 재생
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
            RestoreState();
        }
        feedbackRoutine = StartCoroutine(FeedbackRoutine(allowHitStop));
    }

    // 사망 등으로 연출을 즉시 끊고 원래 상태로 되돌립니다.
    public void Cancel()
    {
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }
        RestoreState();
    }
    //============== 외부 호출 끝 ==============

    //============== 연출 코루틴 ==============
    private IEnumerator FeedbackRoutine(bool allowHitStop)
    {
        // 1) 맞는 순간 바로 흰색 적용
        ShowFlash();

        // 2) 히트스톱: 흰색 상태로 보스 애니메이션만 잠깐 멈춤 (Time.timeScale은 건드리지 않음)
        if (useHitStop && allowHitStop && animator != null && animator.isActiveAndEnabled)
        {
            savedAnimSpeed = animator.speed;
            animator.speed = 0f;
            animFrozen = true;
            yield return new WaitForSecondsRealtime(hitStopDuration);
            ReleaseAnimator();
        }

        // 3) 깜빡임: 흰색 <-> 원래 모습 반복 (첫 흰색은 이미 적용된 상태)
        float half = flashDuration / (flashCount * 2f);
        for (int n = 0; n < flashCount; n++)
        {
            if (n > 0) ShowFlash();
            yield return new WaitForSecondsRealtime(half);

            HideFlash();
            yield return new WaitForSecondsRealtime(half);
        }

        feedbackRoutine = null;
    }
    //============== 연출 코루틴 끝 ==============

    //============== 머티리얼/상태 복구 ==============
    private void ShowFlash()
    {
        Material flash = FlashMaterial;
        if (flash == null) return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null) continue;
            spriteRenderers[i].sharedMaterial = flash;

            // 이 셰이더는 SpriteRenderer.color를 곱하므로 순백이 되도록 흰색으로 맞춘다 (알파는 유지)
            Color white = Color.white;
            white.a = originalColors[i].a;
            spriteRenderers[i].color = white;
        }
        flashShown = true;
    }

    private void HideFlash()
    {
        if (!flashShown) return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null) continue;
            spriteRenderers[i].sharedMaterial = originalMaterials[i];
            spriteRenderers[i].color = originalColors[i];
        }
        flashShown = false;
    }

    private void ReleaseAnimator()
    {
        if (!animFrozen) return;
        animFrozen = false;
        if (animator != null)
            animator.speed = savedAnimSpeed;
    }

    private void RestoreState()
    {
        HideFlash();
        ReleaseAnimator();
    }

    private void OnDisable()
    {
        // 연출 도중 보스가 비활성화돼도 머티리얼/색/속도가 남지 않게 복구
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }
        if (spriteRenderers != null && originalMaterials != null)
            RestoreState();
    }
    //============== 머티리얼/상태 복구 끝 ==============
}