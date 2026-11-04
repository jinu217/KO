using UnityEngine;
using System.Collections;

public class HeartBeatUI : MonoBehaviour
{
    public PlayerStatus playerStatus;
    RectTransform rect;
    Vector3 defaultScale;
    bool isBeating = false;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        defaultScale = rect.localScale;
    }

    //============== 오브젝트가 꺼질 때 상태 초기화 (추가) ==============
    // 기계장비 5개가 되어 이 조각이 비활성화되면 코루틴이 중간에 멈추고
    // isBeating이 true로 남아, 다시 켜졌을 때 박동이 시작되지 않는 문제를 막습니다.
    void OnDisable()
    {
        StopAllCoroutines();
        isBeating = false;

        if (rect != null)
        {
            rect.localScale = defaultScale;
            rect.localRotation = Quaternion.identity;
        }
    }
    //====================================================================

    void Update()
    {
        if (playerStatus == null)
            return;

        // D단계 흔들림
        if (playerStatus.GetBPMRank() == "D")
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

        switch (playerStatus.GetBPMRank())
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
        if (playerStatus.GetBPMRank() == "D")
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