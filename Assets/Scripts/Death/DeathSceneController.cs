using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathSceneController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text countText;

    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private CanvasGroup countGroup;

    [Header("Animation")]
    [SerializeField] private float fadeTime = 1.5f;
    [SerializeField] private float waitTime = 2f;

    private int deathCount = 0;

    private void Start()
    {
        if (SaveData.Instance != null)
        {
            deathCount = SaveData.Instance.deathCount;
            Debug.Log("DeathScene에서 deathCount = " + deathCount);
        }
        else
        {
            Debug.Log("DeathScene에서 SaveData가 없습니다.");
        }

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        titleText.text = "영애님을 구하는";
        countText.text = $"{deathCount + 1}번째 시도";

        titleGroup.alpha = 0;
        countGroup.alpha = 0;

        yield return new WaitForSeconds(0.5f);

        // 첫 번째 문장
        yield return Fade(titleGroup, 0, 1);

        yield return new WaitForSeconds(0.5f);

        // 두 번째 문장
        yield return Fade(countGroup, 0, 1);

        yield return new WaitForSeconds(waitTime);

        // 동시에 사라짐
        Coroutine titleFade = StartCoroutine(Fade(titleGroup, 1, 0));
        Coroutine countFade = StartCoroutine(Fade(countGroup, 1, 0));

        yield return titleFade;
        yield return countFade;

        SceneManager.LoadScene("Stage1-1");
    }

    private IEnumerator Fade(CanvasGroup group, float from, float to)
    {
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, t / fadeTime);
            yield return null;
        }

        group.alpha = to;
    }
}