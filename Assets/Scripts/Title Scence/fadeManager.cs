
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeManager : MonoBehaviour
{
    [Header("페이드 설정")]
    public Image fadeImage;
    public float fadeSpeed = 2f;

    private void Start()
    {
        if (fadeImage == null)
        {
            Debug.LogError("[FadeManager] FadeImage가 연결되지 않았습니다.");
            return;
        }

        //============== 씬 시작 페이드 인 ==============
        StartCoroutine(FadeIn());
    }

    //============== 페이드 인 ==============
    public IEnumerator FadeIn()
    {
        if (fadeImage == null)
            yield break;

        fadeImage.gameObject.SetActive(true);

        Color color = fadeImage.color;
        color.a = 1f;
        fadeImage.color = color;

        while (color.a > 0f)
        {
            color.a = Mathf.MoveTowards(
                color.a, 0f, fadeSpeed * Time.unscaledDeltaTime);

            fadeImage.color = color;
            yield return null;
        }

        color.a = 0f;
        fadeImage.color = color;
        fadeImage.gameObject.SetActive(false);
    }

    //============== 페이드 아웃 ==============
    public IEnumerator FadeOut()
    {
        if (fadeImage == null)
            yield break;

        fadeImage.gameObject.SetActive(true);

        Color color = fadeImage.color;
        color.a = 0f;
        fadeImage.color = color;

        while (color.a < 1f)
        {
            color.a = Mathf.MoveTowards(
                color.a, 1f, fadeSpeed * Time.unscaledDeltaTime);

            fadeImage.color = color;
            yield return null;
        }

        color.a = 1f;
        fadeImage.color = color;
    }
}
