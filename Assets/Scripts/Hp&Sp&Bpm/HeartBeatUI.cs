using UnityEngine;

public class HeartBeatUI : MonoBehaviour
{
    public StatusUI statusUI;

    RectTransform rect;

    Vector3 defaultScale;

    float timer;

    void Start()
    {
        rect = GetComponent<RectTransform>();

        defaultScale = rect.localScale;
    }

    void Update()
    {
        if (statusUI == null)
            return;

        float beatSpeed = 1f;
        float beatPower = 1f;

        // BPM 단계별 설정
        switch (statusUI.GetBPMRank())
        {
            case "A":
                beatSpeed = 1.5f;
                beatPower = 0.05f;
                break;

            case "B":
                beatSpeed = 2.5f;
                beatPower = 0.08f;
                break;

            case "C":
                beatSpeed = 4f;
                beatPower = 0.12f;
                break;

            case "D":
                beatSpeed = 6f;
                beatPower = 0.18f;
                break;
        }

        // 심장 박동
        timer += Time.deltaTime * beatSpeed;

        float scale = 1 + Mathf.Sin(timer) * beatPower;

        rect.localScale = defaultScale * scale;
    }
}