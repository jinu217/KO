using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossArmorUI : MonoBehaviour
{
    [Header("Armor UI")]
    [SerializeField] private RectTransform armor100;
    [SerializeField] private RectTransform armor75;
    [SerializeField] private RectTransform armor50;
    [SerializeField] private RectTransform armor25;

    [Header("Animation")]
    [SerializeField] private float shakeDuration = 0.35f;
    [SerializeField] private float shakeAmount = 8f;
    [SerializeField] private float fallDuration = 0.6f;
    [SerializeField] private float fallDistance = 80f;
    [SerializeField] private float rotateAmount = 90f;

    private Vector2 armor100StartPos;
    private Vector2 armor75StartPos;
    private Vector2 armor50StartPos;
    private Vector2 armor25StartPos;

    private bool armor100Broken;
    private bool armor75Broken;
    private bool armor50Broken;
    private bool armor25Broken;

    private void Awake()
    {
        SaveStartPositions();
    }

    private void SaveStartPositions()
    {
        if (armor100 != null)
            armor100StartPos = armor100.anchoredPosition;

        if (armor75 != null)
            armor75StartPos = armor75.anchoredPosition;

        if (armor50 != null)
            armor50StartPos = armor50.anchoredPosition;

        if (armor25 != null)
            armor25StartPos = armor25.anchoredPosition;
    }

    // 보스 전투 시작 시 호출
    public void ResetArmor()
    {
        StopAllCoroutines();

        ResetArmorObject(armor100, armor100StartPos);
        ResetArmorObject(armor75, armor75StartPos);
        ResetArmorObject(armor50, armor50StartPos);
        ResetArmorObject(armor25, armor25StartPos);

        armor100Broken = false;
        armor75Broken = false;
        armor50Broken = false;
        armor25Broken = false;
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
        // HP 75% 이하
        // 100% 갑옷 제거
        if (hpPercent <= 0.75f && !armor100Broken)
        {
            armor100Broken = true;

            if (armor100 != null)
                StartCoroutine(BreakArmor(armor100));
        }

        // HP 50% 이하
        // 75% 갑옷 제거
        if (hpPercent <= 0.50f && !armor75Broken)
        {
            armor75Broken = true;

            if (armor75 != null)
                StartCoroutine(BreakArmor(armor75));
        }

        // HP 25% 이하
        // 50% 갑옷 제거
        if (hpPercent <= 0.25f && !armor50Broken)
        {
            armor50Broken = true;

            if (armor50 != null)
                StartCoroutine(BreakArmor(armor50));
        }

        // HP 0%
        // 25% 갑옷 제거
        if (hpPercent <= 0f && !armor25Broken)
        {
            armor25Broken = true;

            if (armor25 != null)
                StartCoroutine(BreakArmor(armor25));
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