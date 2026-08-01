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

    [Header("Result UI")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text killCountText;
    [SerializeField] private TMP_Text midBossKillCountText;
    [SerializeField] private TMP_Text mainBossKillCountText;
    [SerializeField] private TMP_Text soulRewardText;

    [Header("Continue UI")]
    [SerializeField] private TMP_Text continueText;

    private int deathCount = 0;
    private bool canContinue = false;
    private bool isWaitingForContinue = false;

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

        if (resultPanel != null)
        resultPanel.SetActive(false);

        StartCoroutine(DeathRoutine());
    }

    private void Update()
    {
        if (!isWaitingForContinue)
            return;

        if (Input.GetKeyDown(KeyCode.Return))
        {
            canContinue = true;
        }
    }

    //=============== 회차 결과창(소울획득) =====================
    private void ShowResult()
    {
        RunData run = SaveData.Instance.run;

        int killCount = run.killCount;
        int midBossKillCount = run.midBossKillCount;
        int mainBossKillCount = run.mainBossKillCount;

        // 보상 기준
        int normalKillReward = 1;       // 일반몹
        int midBossReward = 50;         // 미드보스
        int mainBossReward = 100;       // 메인보스

        int rewardSoul =
            killCount * normalKillReward
            + midBossKillCount * midBossReward
            + mainBossKillCount * mainBossReward;

        killCountText.text = $"일반 몬스터 처치 : {killCount}";
        midBossKillCountText.text = $"미드보스 처치 : {midBossKillCount}";
        mainBossKillCountText.text = $"메인보스 처치 : {mainBossKillCount}";
        soulRewardText.text = $"획득 Soul : {rewardSoul}";

        // 영구 재화에 Soul 추가
        SaveData.Instance.persistent.soul += rewardSoul;

        resultPanel.SetActive(true);
    }

    private IEnumerator DeathRoutine()
    {
        //================== 로드로 진입한 경우 ====================

        if (SaveData.Instance.isLoadIntro)
        {
            // 로드 시에는 결과창을 보여주지 않음
            resultPanel.SetActive(false);

            // 사망 문구 표시
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
            StartCoroutine(Fade(titleGroup, 1, 0));
            StartCoroutine(Fade(countGroup, 1, 0));

            yield return new WaitForSeconds(fadeTime);

            // 로드 인트로 플래그 초기화
            SaveData.Instance.isLoadIntro = false;

            // 저장된 위치로 이동
            SceneManager.LoadScene(
                SaveData.Instance.persistent.nextStage
            );

            yield break;
        }


        //================== 사망으로 진입한 경우 ====================

        // 처음에는 Enter 입력 안내 숨김
        continueText.gameObject.SetActive(false);

        // 결과 계산 및 결과창 표시
        ShowResult();

        // 결과창 표시 후 Enter 안내 표시
        continueText.text = "ENTER를 눌러 계속";
        continueText.gameObject.SetActive(true);

        // Enter 입력 대기
        canContinue = false;

        while (!canContinue)
        {
            yield return null;
        }

        // Enter 입력 비활성화
        isWaitingForContinue = false;

        // Enter 안내 숨김
        continueText.gameObject.SetActive(false);

        // 결과창 숨김
        resultPanel.SetActive(false);


        //================== 사망 문구 ====================

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
        StartCoroutine(Fade(titleGroup, 1, 0));
        StartCoroutine(Fade(countGroup, 1, 0));

        yield return new WaitForSeconds(fadeTime);


        //================== 다음 회차 시작 ====================

        // 사망 후 새 런은 Stage1-1부터 시작
        SaveData.Instance.persistent.nextStage = "Stage1-1";

        // 이번 런 데이터 초기화
        SaveData.Instance.ResetRun();

        // 사망 후 자동 저장
        SaveManager.Instance.Save(
            SaveData.Instance.currentSlot
        );

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
