using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeManager : MonoBehaviour
{
    public Image fadeImage;
    public float fadeSpeed = 2f;

    void Start()
    {
        StartCoroutine(FadeIn());
    }

    public IEnumerator FadeIn()
    {
        Color color = fadeImage.color;

        while (color.a > 0)
        {
            color.a -= Time.deltaTime * fadeSpeed;
            fadeImage.color = color;
            yield return null;
        }

        color.a = 0;
        fadeImage.color = color;
    }

    public IEnumerator FadeOut()
    {
        Color color = fadeImage.color;

        while (color.a < 1)
        {
            color.a += Time.deltaTime * fadeSpeed;
            fadeImage.color = color;
            yield return null;
        }

        color.a = 1;
        fadeImage.color = color;
    }
}