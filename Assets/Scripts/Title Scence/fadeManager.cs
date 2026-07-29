using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeManager : MonoBehaviour
{
    public Image fadeImage;
    public float fadeSpeed = 2f;

    void Start()
    {
        // 시작할 때 FadeImage는 비활성화 상태
        fadeImage.gameObject.SetActive(false);
    }

    public IEnumerator FadeIn()
    {
        // FadeImage 활성화
        fadeImage.gameObject.SetActive(true);

        Color color = fadeImage.color;

        // 완전히 검은 상태에서 시작
        color.a = 1f;
        fadeImage.color = color;

        while (color.a > 0)
        {
            color.a -= Time.deltaTime * fadeSpeed;
            fadeImage.color = color;

            yield return null;
        }

        // 완전히 투명
        color.a = 0;
        fadeImage.color = color;

        // 페이드 인이 끝나면 다시 비활성화
        fadeImage.gameObject.SetActive(false);
    }

    public IEnumerator FadeOut()
    {
        // FadeImage 활성화
        fadeImage.gameObject.SetActive(true);

        Color color = fadeImage.color;

        // 투명한 상태에서 시작
        color.a = 0f;
        fadeImage.color = color;

        while (color.a < 1)
        {
            color.a += Time.deltaTime * fadeSpeed;
            fadeImage.color = color;

            yield return null;
        }

        // 완전히 검은 상태
        color.a = 1;
        fadeImage.color = color;
    }
}