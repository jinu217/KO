using System.Collections;
using UnityEngine;
using UnityEngine.UI;

//============== 보스 갑옷 UI ==============

public class BossArmorUI : MonoBehaviour
{
    [Header("Armor UI")]
    [SerializeField] private RectTransform armor100;
    [SerializeField] private RectTransform armor80;
    [SerializeField] private RectTransform armor60;
    [SerializeField] private RectTransform armor40;

    [Header("Animation")]
    [SerializeField] private float shakeDuration = 0.35f;
    [SerializeField] private float shakeAmount = 8f;
    [SerializeField] private float fallDuration = 0.6f;
    [SerializeField] private float fallDistance = 80f;
    [SerializeField] private float rotateAmount = 90f;

    private Vector2 armor100StartPos;
    private Vector2 armor80StartPos;
    private Vector2 armor60StartPos;
    private Vector2 armor40StartPos;

    private bool armor100Broken;
    private bool armor80Broken;
    private bool armor60Broken;
    private bool armor40Broken;

    private void Awake()
    {
        SaveStartPositions();
    }

    private void SaveStartPositions()
    {
        if (armor100 != null)
            armor100StartPos = armor100.anchoredPosition;

        if (armor80 != null)
            armor80StartPos = armor80.anchoredPosition;

        if (armor60 != null)
            armor60StartPos = armor60.anchoredPosition;

        if (armor40 != null)
            armor40StartPos = armor40.anchoredPosition;
    }

    // 보스 전투 시작 시 호출
    public void ResetArmor()
    {
        StopAllCoroutines();

        ResetArmorObject(armor100, armor100StartPos);
        ResetArmorObject(armor80, armor80StartPos);
        ResetArmorObject(armor60, armor60StartPos);
        ResetArmorObject(armor40, armor40StartPos);

        armor100Broken = false;
        armor80Broken = false;
        armor60Broken = false;
        armor40Broken = false;
    }

    private void ResetArmorObject(RectTransform armor, Vector2 startPos)
    {
        if (armor == null)
            return;

        armor.gameObject.SetActive(true);
        armor.anchoredPosition = startPos;
        armor.localRotation = Quaternion.identity;

        CanvasGroup canvasGroup = armor.GetComponent<CanvasGroup>();

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    // 현재 HP 비율을 기준으로 갑옷 파괴 확인
    public void UpdateArmor(float hpPercent)
    {
        // HP 80% 이하
        // 첫 번째 갑옷 제거
        if (hpPercent <= 0.80f && !armor100Broken)
        {
            armor100Broken = true;

            if (armor100 != null)
                StartCoroutine(BreakArmor(armor100));
        }

        // HP 60% 이하
        // 두 번째 갑옷 제거
        if (hpPercent <= 0.60f && !armor80Broken)
        {
            armor80Broken = true;

            if (armor80 != null)
                StartCoroutine(BreakArmor(armor80));
        }

        // HP 40% 이하
        // 세 번째 갑옷 제거
        if (hpPercent <= 0.40f && !armor60Broken)
        {
            armor60Broken = true;

            if (armor60 != null)
                StartCoroutine(BreakArmor(armor60));
        }

        // HP 20% 이하
        // 네 번째 갑옷 제거
        if (hpPercent <= 0.20f && !armor40Broken)
        {
            armor40Broken = true;

            if (armor40 != null)
                StartCoroutine(BreakArmor(armor40));
        }
    }

    private IEnumerator BreakArmor(RectTransform armor)
    {
        Vector2 startPos = armor.anchoredPosition;

        CanvasGroup canvasGroup = armor.GetComponent<CanvasGroup>();

        // CanvasGroup이 없으면 자동으로 추가
        if (canvasGroup == null)
            canvasGroup = armor.gameObject.AddComponent<CanvasGroup>();

        float timer = 0f;

        // 1. 갑옷 흔들림
        while (timer < shakeDuration)
        {
            timer += Time.deltaTime;

            float progress = timer / shakeDuration;

            float shakeX = Mathf.Sin(progress * Mathf.PI * 6f) * shakeAmount;

            armor.anchoredPosition = startPos + new Vector2(shakeX, 0f);

            yield return null;
        }

        // 흔들림 종료
        armor.anchoredPosition = startPos;

        // 2. 갑옷 떨어짐
        timer = 0f;

        while (timer < fallDuration)
        {
            timer += Time.deltaTime;

            float progress = timer / fallDuration;

            // 부드럽게 떨어짐
            float fallY = Mathf.Lerp(0f, -fallDistance, progress);

            armor.anchoredPosition = startPos + new Vector2(0f, fallY);

            // 떨어지면서 회전
            float rotation = Mathf.Lerp(0f, rotateAmount, progress);

            armor.localRotation = Quaternion.Euler(0f, 0f, rotation);

            // 페이드 아웃
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);

            yield return null;
        }

        // 완전히 제거
        armor.gameObject.SetActive(false);
    }
}