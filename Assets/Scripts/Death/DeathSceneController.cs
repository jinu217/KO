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
            deathCount = SaveData.Instance.persistent.deathCount;
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

        // --------------------
        // 사망 처리
        // --------------------

        // 사망 횟수 증가
        SaveData.Instance.persistent.deathCount++;

        // TODO : 이번 회차에서 획득한 소울 추가
        // SaveData.Instance.persistent.soul += rewardSoul;

        // 이번 회차 데이터 초기화
        //SaveData.Instance.ResetRunData();

        // 다음 이어하기 시작 위치를 Stage1로 변경
        SaveData.Instance.persistent.nextStage = "Stage1-1";
        SaveData.Instance.run.hp = SaveData.Instance.run.maxHp > 0f ? SaveData.Instance.run.maxHp : 50f;
        SaveData.Instance.run.sp = 100f;
        SaveData.Instance.run.bpm = 91f;
        SaveData.Instance.run.spawnId = string.Empty;

        // 사망 후 자동 저장
        SaveManager.Instance.Save(SaveData.Instance.currentSlot);

        // 새 게임 시작
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
