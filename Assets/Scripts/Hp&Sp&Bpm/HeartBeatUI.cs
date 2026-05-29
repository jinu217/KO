using UnityEngine;
using System.Collections;

public class HeartBeatUI : MonoBehaviour
{
    public StatusUI statusUI;

    RectTransform rect;

    Vector3 defaultScale;

    bool isBeating = false;

    void Start()
    {
        rect = GetComponent<RectTransform>();

        defaultScale = rect.localScale;
    }

    void Update()
    {
        if (statusUI == null)
            return;

        // D단계 흔들림
        if (statusUI.GetBPMRank() == "D")
        {
            float shake = Mathf.Sin(Time.time * 45f) * 8f;

            rect.localRotation =
                Quaternion.Euler(0, 0, shake);
        }
        else
        {
            rect.localRotation = Quaternion.identity;
        }

        if (!isBeating)
        {
            StartCoroutine(Heartbeat());
        }
    }

    IEnumerator Heartbeat()
    {
        isBeating = true;

        float power = 1f;
        float delay = 1f;

        switch (statusUI.GetBPMRank())
        {
            // 안정
            case "A":
                power = 0.12f;
                delay = 0.85f;
                break;

            // 긴장
            case "B":
                power = 0.20f;
                delay = 0.55f;
                break;

            // 위험
            case "C":
                power = 0.32f;
                delay = 0.32f;
                break;

            // 폭주
            case "D":
                power = 0.50f;
                delay = 0.16f;
                break;
        }

        // 강한 첫 박동
        yield return Beat(power, 0.05f);

        // 짧은 텀
        yield return new WaitForSeconds(0.03f);

        // 두 번째 박동
        yield return Beat(power * 0.7f, 0.04f);

        // D단계는 불규칙 박동 추가
        if (statusUI.GetBPMRank() == "D")
        {
            if (Random.value > 0.5f)
            {
                yield return new WaitForSeconds(0.02f);

                yield return Beat(power * 0.9f, 0.03f);
            }
        }

        yield return new WaitForSeconds(delay);

        isBeating = false;
    }

    IEnumerator Beat(float power, float duration)
    {
        Vector3 big =
            defaultScale * (1f + power);

        Vector3 compress =
            defaultScale * (1f - power * 0.15f);

        float timer = 0f;

        // 순간 팽창
        while (timer < duration)
        {
            timer += Time.deltaTime;

            rect.localScale = Vector3.Lerp(
                defaultScale,
                big,
                timer / duration
            );

            yield return null;
        }

        timer = 0f;

        // 순간 압축
        while (timer < duration * 0.6f)
        {
            timer += Time.deltaTime;

            rect.localScale = Vector3.Lerp(
                big,
                compress,
                timer / (duration * 0.6f)
            );

            yield return null;
        }

        timer = 0f;

        // 원상복귀
        while (timer < duration)
        {
            timer += Time.deltaTime;

            rect.localScale = Vector3.Lerp(
                compress,
                defaultScale,
                timer / duration
            );

            yield return null;
        }

        rect.localScale = defaultScale;
    }
}