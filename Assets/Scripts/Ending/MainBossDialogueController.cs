using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainBossDialogueController : MonoBehaviour
{
    //============== 대화 UI ==============
    [Header("기존 NPC 대화 UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("대화 중 숨길 UI")]
    [Tooltip("코인/영혼, HP/SP/BPM, 포션 가방 등 대화 중 숨길 UI 오브젝트를 연결합니다.")]
    [SerializeField] private GameObject[] hudObjectsToHide;

    //============== 플레이어 제어 ==============
    [Header("플레이어")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Rigidbody2D playerBody;

    //============== 카메라 ==============
    [Header("카메라 연출")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private CameraFollow2D cameraFollow;
    [SerializeField] private Vector3 cameraFocusOffset = new Vector3(0f, 1f, -10f);
    [SerializeField, Min(0.1f)] private float cameraZoomSize = 5f;
    [SerializeField, Min(0f)] private float cameraBlendTime = 0.35f;

    //============== 대사 설정 ==============
    [Header("대사 설정")]
    [SerializeField] private string csvResourcePath = "BossDialogue/MainBossDialogue";
    [SerializeField, Min(0f)] private float typeSpeed = 0.05f;

    [Tooltip("각 대사 출력이 끝난 뒤 다음 대사로 넘어가기 전까지 기다리는 시간(초)")]
    [SerializeField, Min(0f)] private float lineHoldTime = 1.5f;

    [Tooltip("마지막 대사 출력이 끝난 뒤 대사창이 사라지기 전까지 기다리는 시간(초)")]
    [SerializeField, Min(0f)] private float lastLineHoldTime = 2f;

    [Tooltip("카메라가 보스를 확대한 뒤, 대사창이 나타나기 전까지 기다리는 시간(초)")]
    [SerializeField, Min(0f)] private float panelShowDelay = 1f;

    //============== 라스트보스 식별 ==============
    [Header("라스트보스 식별")]
    [Tooltip("5스테이지 라스트보스의 Boss Id입니다. 해당 보스의 MainBossController > Boss Id와 같아야 합니다. " +
             "이 보스에게만 기계장비별 조우 대사 / 회유 / 처치 후 선택지가 적용되고, " +
             "스테이지 1~4 메인보스는 기존 방식(Encounter / AfterDefeat) 그대로 동작합니다.")]
    [SerializeField] private string lastBossId = "Knight5";

    //============== 처치 후 선택지 (살린다 / 죽인다) ==============
    [Header("처치 후 선택지 (시작 시 비활성, 선택 중 자동 활성화)")]
    [Tooltip("Prompt / Spare / Kill을 모두 자식으로 가진 부모 오브젝트")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private TMP_Text choicePromptText;
    [Tooltip("왼쪽 배치: 보스를 살리는 선택")]
    [SerializeField] private TMP_Text spareOptionText;
    [Tooltip("오른쪽 배치: 보스를 죽이는 선택")]
    [SerializeField] private TMP_Text killOptionText;

    [SerializeField] private string choicePrompt = "어떻게 하시겠습니까?";
    [SerializeField] private string spareLabel = "살린다";
    [SerializeField] private string killLabel = "죽인다";

    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color unselectedColor = new Color(1f, 1f, 1f, 0.45f);
    [Tooltip("선택지가 뜬 직후 오입력을 막기 위해 입력을 받지 않는 시간(초)")]
    [SerializeField, Min(0f)] private float choiceInputDelay = 0.3f;

    //============== 엔딩 씬 이동 ==============
    [Header("엔딩 씬 이동")]
    [Tooltip("회유 엔딩 / 처치 후 선택이 끝나면 이동할 씬 이름입니다. Build Settings에 등록되어 있어야 합니다.")]
    [SerializeField] private string endingSceneName = "EndingScene";
    [Tooltip("엔딩 씬으로 넘어가기 전에 페이드 아웃을 재생할 FadeManager입니다. 비워 두면 씬에서 자동으로 찾습니다.")]
    [SerializeField] private FadeManager fadeManager;

    private readonly List<DialogueLine> allLines = new List<DialogueLine>();

    private bool isPlaying;
    private bool csvLoaded;
    //============== 조우 대화 중복 방지 ==============
    // 조우 대화를 이미 재생한 보스의 InstanceID를 기억합니다.
    private readonly HashSet<int> encounterPlayedBossIds = new HashSet<int>();

    private class DialogueLine
    {
        public string bossId;
        public string phase;
        public int order;
        public string characterName;
        public string dialogue;
        public string condition;
    }

    private class CameraSnapshot
    {
        public Vector3 position;
        public float orthographicSize;
        public bool followWasEnabled;
    }

    private bool[] hudWasActive;
    private float originalGravityScale;
    private bool playerGravityCached;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (cameraFollow == null && targetCamera != null)
            cameraFollow = targetCamera.GetComponent<CameraFollow2D>();

        if (playerController == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerController = player.GetComponent<PlayerController>();

                if (playerBody == null)
                    playerBody = player.GetComponent<Rigidbody2D>();
            }
        }

        if (playerBody == null && playerController != null)
            playerBody = playerController.GetComponent<Rigidbody2D>();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        // 선택지 패널은 항상 꺼진 상태로 시작합니다.
        if (choicePanel != null)
            choicePanel.SetActive(false);

        //============== 페이드 매니저 자동 탐색 ==============
        if (fadeManager == null)
            fadeManager = FindFirstObjectByType<FadeManager>();

        LoadCsv();
    }

    //============== 외부 호출: 조우 대화 ==============
    public bool TryPlayEncounter(MainBossController boss, string bossId)
    {
        if (isPlaying || boss == null)
            return false;

        //============== 이미 조우 대화를 한 보스는 다시 재생하지 않음 ==============
        if (encounterPlayedBossIds.Contains(boss.GetInstanceID()))
        {
            Debug.Log(
                $"[MainBossDialogueController] 조우 대화 중복 요청 차단: boss={boss.name}", boss);
            return false;
        }

        //============== 조우 phase 선택 (기계장비 개수 / 히로인 선택) ==============
        // 회유 루트 = EndingManager가 '라스트보스 생존만 가능(ForceAlive)'으로 판정한 경우
        // (기계장비 3개 + 히로인 사망). 판정 기준을 EndingManager 한 곳에서만 가져옵니다.
        // 스테이지 1~4 메인보스는 기존 "Encounter" 대사를 그대로 사용합니다.
        bool isLastBoss = IsLastBoss(bossId);
        bool persuadeRoute = isLastBoss && IsPersuadeRoute();
        string encounterPhase = isLastBoss ? GetEncounterPhase(persuadeRoute) : "Encounter";

        List<DialogueLine> lines = GetLines(bossId, encounterPhase);

        // 개수별 대사가 CSV에 아직 없으면 기존 공용 "Encounter" 대사로 대체합니다.
        if (lines.Count == 0 && encounterPhase != "Encounter")
        {
            Debug.LogWarning(
                $"[MainBossDialogueController] phase='{encounterPhase}' 대사가 없어 " +
                $"'Encounter'로 대체합니다. bossId={bossId}", this);

            lines = GetLines(bossId, "Encounter");
        }

        //============== 회유 루트: 조우 대사 뒤에 회유 대사를 이어 붙임 ==============
        if (persuadeRoute)
        {
            List<DialogueLine> persuadeLines = GetLines(bossId, "Persuade");

            if (persuadeLines.Count == 0)
            {
                Debug.LogError(
                    $"[MainBossDialogueController] 회유 대사를 찾지 못했습니다. " +
                    $"CSV의 bossId='{bossId}', phase='Persuade' 항목을 확인하세요.", this);
            }

            lines.AddRange(persuadeLines);
        }

        if (lines.Count == 0)
        {
            if (persuadeRoute)
            {
                Debug.LogError(
                    "[MainBossDialogueController] 회유 루트인데 재생할 대사가 없어 " +
                    "일반 전투로 진행됩니다. CSV를 확인하세요.", this);
            }
            else
            {
                Debug.LogWarning(
                    $"[MainBossDialogueController] 조우 대사를 찾지 못해 대화 없이 진행합니다. " +
                    $"bossId='{bossId}', 시도한 phase='{encounterPhase}', " +
                    $"라스트보스 판정={isLastBoss}(Last Boss Id='{lastBossId}'), " +
                    $"EndingManager 존재={EndingManager.Instance != null}, 로드된 CSV 대사 수={allLines.Count}",
                    this);
            }

            return false;
        }

        //============== 조우 대화 시작 기록 ==============
        encounterPlayedBossIds.Add(boss.GetInstanceID());

        Debug.Log(
            $"[MainBossDialogueController] 조우 대화 시작: boss={boss.name}, " +
            $"instanceId={boss.GetInstanceID()}, bossId={bossId}, phase={encounterPhase}, " +
            $"회유루트={persuadeRoute}, 대사 수={lines.Count}",
            boss);

        isPlaying = true;
        boss.SetDialoguePaused(true);

        // 회유 루트는 전투를 하지 않으므로 대화 후 보스를 다시 움직이지 않습니다.
        StartCoroutine(PlayDialogueRoutine(boss, lines, !persuadeRoute, persuadeRoute));
        return true;
    }

    //============== 라스트보스 판별 ==============
    private bool IsLastBoss(string bossId)
    {
        return !string.IsNullOrEmpty(lastBossId)
            && string.Equals(bossId, lastBossId, StringComparison.OrdinalIgnoreCase);
    }

    //============== 조우 phase 판정 ==============
    private bool IsPersuadeRoute()
    {
        return EndingManager.Instance != null
            && EndingManager.Instance.GetLastBossRestriction()
               == EndingManager.ChoiceRestriction.ForceAlive;
    }

    private string GetEncounterPhase(bool persuadeRoute)
    {
        // EndingManager가 없는 씬에서 단독 테스트할 때는 기계장비 0개로 취급합니다.
        if (EndingManager.Instance == null)
        {
            Debug.LogWarning(
                "[MainBossDialogueController] EndingManager가 씬에 없어 기계장비 0개 기준 대사를 사용합니다.", this);
            return "Encounter_M0";
        }

        int count = Mathf.Clamp(EndingManager.Instance.MachineEquipmentCount, 0, 5);

        // 3개일 때만 히로인 상태에 따라 한 번 더 나눕니다.
        if (count == 3)
            return persuadeRoute ? "Encounter_M3_HeroineDead" : "Encounter_M3_HeroineAlive";

        return "Encounter_M" + count;
    }

    //============== 외부 호출: 처치 후 보스 대화 ==============
    public IEnumerator PlayAfterDefeat(MainBossController boss, string bossId)
    {
        if (boss == null)
        {
            Debug.LogError("[MainBossDialogueController] 처치 후 보스 대화 실패: 보스 참조가 없습니다.", this);
            yield break;
        }

        Debug.Log($"[MainBossDialogueController] 처치 후 보스 대화 요청: bossId={bossId}", this);

        //============== 테스트용: 처치 시점 엔딩 상태 로그 (라스트보스만) ==============
        if (IsLastBoss(bossId) && EndingManager.Instance != null)
            EndingManager.Instance.LogEndingState("라스트보스 처치");

        List<DialogueLine> lines = GetLines(bossId, "AfterDefeat");

        if (lines.Count == 0)
        {
            Debug.LogError(
                $"[MainBossDialogueController] 처치 후 보스 대사를 찾지 못했습니다. " +
                $"CSV의 bossId='{bossId}', phase='AfterDefeat' 항목을 확인하세요.",
                this);
            yield break;
        }

        while (isPlaying)
            yield return null;

        isPlaying = true;

        Debug.Log($"[MainBossDialogueController] 처치 후 보스 대화 시작: {bossId}, 대사 수={lines.Count}", this);

        // 라스트보스만 대화가 끝난 뒤 [살린다]/[죽인다] 선택지와 Kill/Spare 대사까지 이어서 진행합니다.
        // 스테이지 1~4 메인보스는 기존처럼 AfterDefeat 대사만 재생합니다.
        yield return PlayDialogueRoutine(boss, lines, false, false, IsLastBoss(bossId), bossId);

        Debug.Log($"[MainBossDialogueController] 처치 후 보스 대화 종료: {bossId}", this);
    }

    //============== 대화 진행 ==============
    private IEnumerator PlayDialogueRoutine(
        MainBossController boss,
        List<DialogueLine> lines,
        bool resumeBossAfterDialogue,
        bool persuadeEnding = false,
        bool offerLastBossChoice = false,
        string bossId = null)
    {
        //============== 대화 시작: 플레이어/HUD/카메라 ==============
        CameraSnapshot cameraSnapshot = CaptureCamera();
        HideHud();
        LockPlayer();

        yield return MoveCameraToBoss(boss != null ? boss.transform : null);

        //============== 대사창 표시 전 대기 ==============
        if (panelShowDelay > 0f)
            yield return new WaitForSecondsRealtime(panelShowDelay);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        //============== 대사 출력 ==============
        yield return PlayLineList(lines);

        //============== 대사창 닫기 ==============
        // 회유 엔딩은 대사창을 연 채로 페이드 아웃을 시작하므로 닫지 않습니다.
        if (!persuadeEnding && dialoguePanel != null)
            dialoguePanel.SetActive(false);

        //============== 회유 엔딩: 대사창을 띄운 채 페이드 아웃 후 엔딩 씬으로 ==============
        // HUD/카메라/플레이어 조작은 복구하지 않고 잠근 채로 씬을 넘깁니다.
        // (복구하면 엔딩 직전에 HUD가 잠깐 다시 보이고 플레이어가 움직일 수 있습니다.)
        if (persuadeEnding)
        {
            HandlePersuadeEnding(); // 회유 결과 기록만 합니다.
            yield return FadeOutAndLoadEndingScene();
            isPlaying = false;
            yield break;
        }

        //============== 처치 후 선택지 (살린다 / 죽인다) + 반응 대사 ==============
        bool lastBossChoiceMade = false;

        if (offerLastBossChoice)
        {
            bool spare = false;
            yield return ShowLastBossChoice(result => spare = result);

            RecordLastBossChoice(spare);
            lastBossChoiceMade = true;

            // 선택 결과에 따른 보스의 마지막 대사
            // 마지막 대사 뒤에는 대사창을 닫지 않고, 띄운 채로 페이드 아웃을 시작합니다.
            List<DialogueLine> reactionLines = GetLines(bossId, spare ? "Spare" : "Kill");

            if (reactionLines.Count > 0)
            {
                if (dialoguePanel != null)
                    dialoguePanel.SetActive(true);

                yield return PlayLineList(reactionLines);
            }
            else
            {
                Debug.LogWarning(
                    $"[MainBossDialogueController] 선택 후 대사를 찾지 못했습니다. " +
                    $"CSV의 bossId='{bossId}', phase='{(spare ? "Spare" : "Kill")}' 항목을 확인하세요.",
                    this);
            }
        }

        //============== 처치 후 선택 완료: 대사창을 띄운 채 페이드 아웃 후 엔딩 씬으로 ==============
        if (lastBossChoiceMade)
        {
            yield return FadeOutAndLoadEndingScene();
            isPlaying = false;
            yield break;
        }

        //============== 대화 종료 (스테이지 1~4 메인보스 등 일반 대화) ==============
        yield return RestoreCamera(cameraSnapshot);
        RestoreHud();
        UnlockPlayer();

        // 조우 대화가 끝나면 보스를 다시 움직이게 하고, 미뤄 둔 감지 처리를 실행합니다.
        // 처치 후 대화(resumeBossAfterDialogue == false)에서는 호출하지 않습니다.
        if (resumeBossAfterDialogue && boss != null)
        {
            boss.SetDialoguePaused(false);
            boss.OnEncounterDialogueFinished();
        }

        isPlaying = false;
    }

    //============== 대사 목록 출력 ==============
    private IEnumerator PlayLineList(List<DialogueLine> lines)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            DialogueLine line = lines[i];

            if (characterNameText != null)
                characterNameText.text = line.characterName;

            if (dialogueText != null)
                yield return TypeLine(line.dialogue);
            else
                yield return new WaitForSecondsRealtime(1f);

            // 마지막 대사는 더 오래 보여준 뒤 대사창을 닫습니다.
            bool isLastLine = (i == lines.Count - 1);
            yield return new WaitForSecondsRealtime(isLastLine ? lastLineHoldTime : lineHoldTime);
        }
    }

    //============== 회유 엔딩 처리 ==============
    private void HandlePersuadeEnding()
    {
        if (EndingManager.Instance == null)
        {
            Debug.LogError("[MainBossDialogueController] EndingManager가 없어 회유 결과를 저장하지 못했습니다.", this);
            return;
        }

        // 회유 = 라스트보스 '생존' 선택으로 기록합니다. (3개 + 히로인 사망 → 히든엔딩)
        EndingManager.Instance.SetLastBossChoice(LastBossEndingChoice.Alive);

        Debug.Log(
            $"[MainBossDialogueController] 회유 엔딩 판정: {EndingManager.Instance.DetermineEnding()}",
            this);
    }

    //============== 페이드 아웃 후 엔딩 씬 이동 ==============
    private IEnumerator FadeOutAndLoadEndingScene()
    {
        if (fadeManager != null)
        {
            yield return StartCoroutine(fadeManager.FadeOut());
        }
        else
        {
            Debug.LogWarning(
                "[MainBossDialogueController] FadeManager를 찾지 못해 페이드 없이 엔딩 씬으로 이동합니다.", this);
        }

        LoadEndingScene();
    }

    //============== 엔딩 씬 이동 ==============
    private void LoadEndingScene()
    {
        if (string.IsNullOrEmpty(endingSceneName))
        {
            Debug.LogError("[MainBossDialogueController] Ending Scene Name이 비어 있습니다.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(endingSceneName))
        {
            Debug.LogError(
                $"[MainBossDialogueController] 씬 '{endingSceneName}'을(를) 불러올 수 없습니다. " +
                $"File > Build Profiles(Build Settings)의 Scene List에 등록했는지, 이름이 같은지 확인하세요.", this);
            return;
        }

        Debug.Log($"[MainBossDialogueController] 엔딩 씬 이동: {endingSceneName}", this);
        SceneManager.LoadScene(endingSceneName);
    }

    //============== 처치 후 선택지 ==============
    // 좌우 방향키로 이동, F키로 확정합니다. (왼쪽 = 살린다, 오른쪽 = 죽인다)
    // EndingManager의 제한에 따라 선택 가능한 항목이 달라집니다.
    //   Free      : 둘 다 선택 가능
    //   ForceDead : [죽인다]만 표시 (기계장비 4~5개, 침식)
    //   ForceAlive: [살린다]만 표시 (정상 흐름에서는 회유 루트라 이곳에 오지 않음)
    private IEnumerator ShowLastBossChoice(Action<bool> onDone)
    {
        EndingManager.ChoiceRestriction restriction =
            EndingManager.Instance != null
                ? EndingManager.Instance.GetLastBossRestriction()
                : EndingManager.ChoiceRestriction.Free;

        bool canSpare = restriction != EndingManager.ChoiceRestriction.ForceDead;
        bool canKill = restriction != EndingManager.ChoiceRestriction.ForceAlive;

        if (choicePanel == null || choicePromptText == null
            || spareOptionText == null || killOptionText == null)
        {
            Debug.LogError("[MainBossDialogueController] 처치 후 선택지 UI가 연결되지 않았습니다.", this);
            onDone?.Invoke(canSpare);
            yield break;
        }

        choicePromptText.text = choicePrompt;
        spareOptionText.gameObject.SetActive(canSpare);
        killOptionText.gameObject.SetActive(canKill);
        choicePanel.SetActive(true);

        // 기본 선택: 가능한 항목 중 왼쪽(살린다) 우선
        bool spareSelected = canSpare;
        RefreshChoiceVisual(spareSelected);

        if (choiceInputDelay > 0f)
            yield return new WaitForSecondsRealtime(choiceInputDelay);

        while (true)
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null)
            {
                // 둘 다 선택 가능할 때만 좌우 이동
                if (canSpare && canKill)
                {
                    if (keyboard.leftArrowKey.wasPressedThisFrame && !spareSelected)
                    {
                        spareSelected = true;
                        RefreshChoiceVisual(spareSelected);
                    }
                    else if (keyboard.rightArrowKey.wasPressedThisFrame && spareSelected)
                    {
                        spareSelected = false;
                        RefreshChoiceVisual(spareSelected);
                    }
                }

                //============== F키 확정 ==============
                if (keyboard.fKey.wasPressedThisFrame)
                    break;
            }

            yield return null;
        }

        choicePanel.SetActive(false);
        onDone?.Invoke(spareSelected);
    }

    private void RefreshChoiceVisual(bool spareSelected)
    {
        spareOptionText.text = spareSelected ? spareLabel + " (F)" : spareLabel;
        killOptionText.text = !spareSelected ? killLabel + " (F)" : killLabel;

        spareOptionText.color = spareSelected ? selectedColor : unselectedColor;
        killOptionText.color = !spareSelected ? selectedColor : unselectedColor;
    }

    private void RecordLastBossChoice(bool spare)
    {
        if (EndingManager.Instance == null)
        {
            Debug.LogError("[MainBossDialogueController] EndingManager가 없어 라스트보스 선택을 저장하지 못했습니다.", this);
            return;
        }

        EndingManager.Instance.SetLastBossChoice(
            spare ? LastBossEndingChoice.Alive : LastBossEndingChoice.Dead);

        Debug.Log(
            $"[MainBossDialogueController] 처치 후 선택: {(spare ? "살린다" : "죽인다")}, " +
            $"엔딩 판정: {EndingManager.Instance.DetermineEnding()}",
            this);
    }

    private IEnumerator TypeLine(string line)
    {
        //============== 리치 텍스트 태그 대응: 전체 문자열을 넣고 보이는 글자 수만 늘림 ==============
        dialogueText.text = line;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        int totalCharacters = dialogueText.textInfo.characterCount;

        for (int i = 1; i <= totalCharacters; i++)
        {
            dialogueText.maxVisibleCharacters = i;

            if (typeSpeed > 0f)
                yield return new WaitForSecondsRealtime(typeSpeed);
            else
                yield return null;
        }

        // 다음 대사나 다른 텍스트 사용에 영향이 없도록 전체 표시로 복구합니다.
        dialogueText.maxVisibleCharacters = int.MaxValue;
    }

    //============== 플레이어 제어 ==============
    private void LockPlayer()
    {
        if (playerController != null)
            playerController.PauseKeyboardControl();

        if (playerBody != null)
        {
            originalGravityScale = playerBody.gravityScale;
            playerGravityCached = true;

            playerBody.linearVelocity = Vector2.zero;
            playerBody.angularVelocity = 0f;
            playerBody.gravityScale = 0f;
        }
    }

    private void UnlockPlayer()
    {
        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector2.zero;
            playerBody.angularVelocity = 0f;

            if (playerGravityCached)
                playerBody.gravityScale = originalGravityScale;
        }

        playerGravityCached = false;

        if (playerController != null)
            playerController.ResumeKeyboardControl();
    }

    //============== HUD 표시/복구 ==============

    private void HideHud()
    {
        if (hudObjectsToHide == null)
            return;

        hudWasActive = new bool[hudObjectsToHide.Length];

        for (int i = 0; i < hudObjectsToHide.Length; i++)
        {
            GameObject obj = hudObjectsToHide[i];

            if (obj == null)
                continue;

            hudWasActive[i] = obj.activeSelf;
            obj.SetActive(false);
        }
    }

    private void RestoreHud()
    {
        if (hudObjectsToHide == null || hudWasActive == null)
            return;

        for (int i = 0; i < hudObjectsToHide.Length; i++)
        {
            if (hudObjectsToHide[i] != null)
                hudObjectsToHide[i].SetActive(hudWasActive[i]);
        }

        hudWasActive = null;
    }

    //============== 카메라 제어 ==============

    private CameraSnapshot CaptureCamera()
    {
        if (targetCamera == null)
            return null;

        CameraSnapshot snapshot = new CameraSnapshot
        {
            position = targetCamera.transform.position,
            orthographicSize = targetCamera.orthographicSize,
            followWasEnabled = cameraFollow != null && cameraFollow.enabled
        };

        if (cameraFollow != null)
            cameraFollow.enabled = false;

        return snapshot;
    }

    private IEnumerator MoveCameraToBoss(Transform bossTransform)
    {
        if (targetCamera == null || bossTransform == null)
            yield break;

        Vector3 startPosition = targetCamera.transform.position;
        float startSize = targetCamera.orthographicSize;

        Vector3 targetPosition = bossTransform.position + cameraFocusOffset;
        targetPosition.z = cameraFocusOffset.z;

        float duration = Mathf.Max(0.01f, cameraBlendTime);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            float size = Mathf.Lerp(startSize, cameraZoomSize, t);
            Vector3 position = Vector3.Lerp(startPosition, targetPosition, t);

            position = ClampCamera(position, size);
            targetCamera.orthographicSize = size;
            targetCamera.transform.position = position;

            yield return null;
        }

        targetCamera.orthographicSize = cameraZoomSize;
        targetCamera.transform.position = ClampCamera(targetPosition, cameraZoomSize);
    }

    private IEnumerator RestoreCamera(CameraSnapshot snapshot)
    {
        if (targetCamera == null || snapshot == null)
        {
            RestoreCameraFollow(snapshot);
            yield break;
        }

        Vector3 targetPosition = snapshot.position;

        // 플레이어가 대화 중 이동하지 않으므로 현재 카메라의 원래 위치로 복귀합니다.
        float startSize = targetCamera.orthographicSize;
        Vector3 startPosition = targetCamera.transform.position;
        float duration = Mathf.Max(0.01f, cameraBlendTime);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            float size = Mathf.Lerp(startSize, snapshot.orthographicSize, t);
            Vector3 position = Vector3.Lerp(startPosition, targetPosition, t);

            position = ClampCamera(position, size);
            targetCamera.orthographicSize = size;
            targetCamera.transform.position = position;

            yield return null;
        }

        targetCamera.orthographicSize = snapshot.orthographicSize;
        targetCamera.transform.position = ClampCamera(targetPosition, snapshot.orthographicSize);

        RestoreCameraFollow(snapshot);
    }

    private void RestoreCameraFollow(CameraSnapshot snapshot)
    {
        if (cameraFollow != null && snapshot != null)
            cameraFollow.enabled = snapshot.followWasEnabled;
    }

    private Vector3 ClampCamera(Vector3 position, float orthographicSize)
    {
        if (cameraFollow != null)
            return cameraFollow.ClampToBounds(position, orthographicSize);

        return position;
    }

    //============== CSV 로드 및 파싱 ==============

    private void LoadCsv()
    {
        allLines.Clear();

        TextAsset csv = Resources.Load<TextAsset>(csvResourcePath);

        if (csv == null)
        {
            Debug.LogError(
                $"[MainBossDialogueController] CSV를 찾을 수 없습니다: " +
                $"Assets/Resources/{csvResourcePath}.csv", this);

            csvLoaded = false;
            return;
        }

        List<List<string>> rows = ParseCsv(csv.text);

        if (rows.Count < 2)
        {
            Debug.LogWarning("[MainBossDialogueController] CSV에 대사가 없습니다.", this);
            csvLoaded = true;
            return;
        }

        Dictionary<string, int> columns = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < rows[0].Count; i++)
            columns[rows[0][i].Trim()] = i;

        if (!columns.ContainsKey("bossId")
            || !columns.ContainsKey("phase")
            || !columns.ContainsKey("order")
            || !columns.ContainsKey("characterName")
            || !columns.ContainsKey("dialogue"))
        {
            Debug.LogError(
                "[MainBossDialogueController] CSV 헤더에 " +
                "bossId, phase, order, characterName, dialogue가 필요합니다.", this);

            csvLoaded = false;
            return;
        }

        for (int i = 1; i < rows.Count; i++)
        {
            List<string> row = rows[i];

            string bossId = GetColumn(row, columns, "bossId").Trim();
            string phase = GetColumn(row, columns, "phase").Trim();
            string orderText = GetColumn(row, columns, "order").Trim();
            string characterName = GetColumn(row, columns, "characterName").Trim();
            string dialogue = GetColumn(row, columns, "dialogue").Trim();
            string condition = GetColumn(row, columns, "condition").Trim();

            if (string.IsNullOrEmpty(bossId)
                || string.IsNullOrEmpty(phase)
                || string.IsNullOrEmpty(dialogue))
                continue;

            if (!int.TryParse(orderText, out int order))
                order = i;

            allLines.Add(new DialogueLine
            {
                bossId = bossId,
                phase = phase,
                order = order,
                characterName = characterName,
                dialogue = dialogue,
                condition = condition
            });
        }

        csvLoaded = true;
        Debug.Log($"[MainBossDialogueController] CSV 대사 {allLines.Count}개 로드 완료.", this);
    }

    private string GetColumn(
        List<string> row,
        Dictionary<string, int> columns,
        string columnName)
    {
        if (!columns.TryGetValue(columnName, out int index))
            return "";

        if (index < 0 || index >= row.Count)
            return "";

        return row[index];
    }

    private List<DialogueLine> GetLines(string bossId, string phase)
    {
        List<DialogueLine> result = new List<DialogueLine>();

        if (!csvLoaded)
            LoadCsv();

        foreach (DialogueLine line in allLines)
        {
            if (string.Equals(line.bossId, bossId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(line.phase, phase, StringComparison.OrdinalIgnoreCase)
                && MatchesCondition(line.condition))
            {
                result.Add(line);
            }
        }

        result.Sort((a, b) => a.order.CompareTo(b.order));
        return result;
    }

    //============== 기계장비 개수 조건 판정 ==============
    // 빈 값 / Any : 항상 재생
    // M0          : 기계장비 0개일 때만
    // M1Plus      : 기계장비 1개 이상일 때만
    // M2Minus     : 기계장비 2개 이하일 때만 (0~2개)
    // M3Plus      : 기계장비 3개 이상일 때만
    private bool MatchesCondition(string condition)
    {
        if (string.IsNullOrEmpty(condition)
            || string.Equals(condition, "Any", StringComparison.OrdinalIgnoreCase))
            return true;

        // EndingManager가 없는 씬에서 단독 테스트할 때는 기계장비 0개로 취급합니다.
        int count = EndingManager.Instance != null
            ? EndingManager.Instance.MachineEquipmentCount
            : 0;

        if (string.Equals(condition, "M0", StringComparison.OrdinalIgnoreCase))
            return count == 0;

        if (string.Equals(condition, "M1Plus", StringComparison.OrdinalIgnoreCase))
            return count >= 1;

        if (string.Equals(condition, "M2Minus", StringComparison.OrdinalIgnoreCase))
            return count <= 2;

        if (string.Equals(condition, "M3Plus", StringComparison.OrdinalIgnoreCase))
            return count >= 3;

        Debug.LogWarning(
            $"[MainBossDialogueController] 알 수 없는 condition '{condition}' → 항상 재생으로 처리합니다.", this);
        return true;
    }
    
    private List<List<string>> ParseCsv(string text)
    {
        List<List<string>> rows = new List<List<string>>();
        List<string> row = new List<string>();
        StringBuilder field = new StringBuilder();
        bool insideQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '"')
            {
                if (insideQuotes && i + 1 < text.Length && text[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (c == ',' && !insideQuotes)
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if ((c == '\n' || c == '\r') && !insideQuotes)
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    i++;

                row.Add(field.ToString());
                field.Clear();

                if (row.Exists(value => !string.IsNullOrWhiteSpace(value)))
                    rows.Add(row);

                row = new List<string>();
            }
            else
            {
                field.Append(c);
            }
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());

            if (row.Exists(value => !string.IsNullOrWhiteSpace(value)))
                rows.Add(row);
        }

        return rows;
    }
}