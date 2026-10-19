using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HeroineDialogueController : MonoBehaviour
{
    private class Line
    {
        public readonly string name;
        public readonly string text;
        public readonly bool machine;   // 침식된 기계의 목소리 여부

        public Line(string name, string text, bool machine = false)
        {
            this.name = name;
            this.text = text;
            this.machine = machine;
        }
    }

    private class CameraSnapshot
    {
        public Vector3 position;
        public float orthographicSize;
        public bool followWasEnabled;
    }

    //============== 대화 UI ==============
    [Header("대화 UI (시작 시 비활성, 대화 중 자동 활성화)")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("대화 중 숨길 UI")]
    [Tooltip("코인/영혼, HP/SP/BPM, 포션 가방 등")]
    [SerializeField] private GameObject[] hudObjectsToHide;

    //============== 플레이어 / 히로인 ==============
    [Header("플레이어 / 히로인")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Rigidbody2D playerBody;
    [Tooltip("히로인(Daughter) 위치. 비워 두면 이 오브젝트의 위치를 사용합니다.")]
    [SerializeField] private Transform heroineTransform;

    //============== 카메라 ==============
    [Header("카메라 연출 (플레이어와 히로인 중간을 비춤)")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private CameraFollow2D cameraFollow;
    [SerializeField] private Vector3 cameraFocusOffset = new Vector3(0f, 1f, -10f);
    [SerializeField, Min(0.1f)] private float minZoomSize = 4f;
    [Tooltip("두 캐릭터 바깥쪽으로 확보할 여백")]
    [SerializeField, Min(0f)] private float pairPadding = 2.5f;
    [SerializeField, Min(0f)] private float cameraBlendTime = 0.5f;

    //============== 대사 설정 ==============
    [Header("대사 설정")]
    [SerializeField, Min(0f)] private float typeSpeed = 0.05f;
    [Tooltip("대사 출력 후 다음 대사까지 대기 시간 (F키로 건너뛰기 가능)")]
    [SerializeField, Min(0f)] private float lineHoldTime = 1.5f;
    [Tooltip("카메라 이동 후 대사창이 나타나기 전 대기 시간")]
    [SerializeField, Min(0f)] private float panelShowDelay = 0.5f;

    //============== 선택지 UI ==============
    [Header("선택지 (시작 시 비활성, 선택 중 자동 활성화)")]
    [Tooltip("Prompt / Alive / Dead를 모두 자식으로 가진 부모 오브젝트")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private TMP_Text choicePromptText;
    [Tooltip("왼쪽 배치: 영애를 살리는 선택")]
    [SerializeField] private TMP_Text aliveOptionText;
    [Tooltip("오른쪽 배치: 영애를 죽이는 선택")]
    [SerializeField] private TMP_Text deadOptionText;

    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color unselectedColor = new Color(1f, 1f, 1f, 0.45f);
    [Tooltip("선택지가 뜬 직후 오입력을 막기 위해 입력을 받지 않는 시간(초)")]
    [SerializeField, Min(0f)] private float choiceInputDelay = 0.3f;

    //============== 침식 연출: 카메라 떨림 ==============
    [Header("침식 연출 - 카메라 떨림 (기계 목소리가 나올 때)")]
    [Tooltip("기계장비 개수별 떨림 세기 (월드 단위)")]
    [SerializeField, Min(0f)] private float cameraShake3 = 0.015f;
    [SerializeField, Min(0f)] private float cameraShake4 = 0.03f;
    [SerializeField, Min(0f)] private float cameraShake5 = 0.07f;

    //============== 침식 연출: 비네트 ==============
    [Header("침식 연출 - 붉은 비네트")]
    [Tooltip("화면 전체를 덮는 Image. 비워 두면 비네트 없이 진행합니다.")]
    [SerializeField] private Image vignetteImage;
    [SerializeField] private Color vignetteColor = new Color(0.8f, 0f, 0f, 1f);
    [Tooltip("4개: 선택지가 뜰 때만 약하게")]
    [SerializeField, Range(0f, 1f)] private float vignetteAlpha4 = 0.25f;
    [Tooltip("5개: 대화 시작부터 진하게 맥동")]
    [SerializeField, Range(0f, 1f)] private float vignetteAlpha5 = 0.7f;
    [SerializeField, Min(0f)] private float vignetteFadeTime = 0.8f;

    //============== 침식 연출: 글씨 ==============
    [Header("침식 연출 - 글씨")]
    [Tooltip("기계 목소리 대사의 글자색 / 이름색")]
    [SerializeField] private Color machineTextColor = new Color(0.9f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color machineNameColor = new Color(0.7f, 0.1f, 0.1f, 1f);
    [Tooltip("5개일 때 기계 대사의 깨진 글씨가 추가로 남아 있는 시간(초)")]
    [SerializeField, Min(0f)] private float glitchLingerTime5 = 1.2f;
    [Tooltip("4개일 때 선택지 경고 문구 글자 크기")]
    [SerializeField, Min(1)] private int warningFontSize = 8;
    [Tooltip("5개일 때 선택지 글자가 흔들리는 세기 (UI 픽셀)")]
    [SerializeField, Min(0f)] private float optionShakeAmount = 3f;

    //============== 이름 / 선택지 문구 ==============
    private const string HeroineName = "영애";
    private const string MachineName = "???";   // 침식된 기계의 목소리

    private const string ChoicePrompt = "영애를 어떻게 하시겠습니까?";
    private const string AliveOptionLabel = "손을 내민다";
    private const string DeadOptionLabel = "손을 놓는다";
    private const string ErosionAliveLabel = "손을 내…밀…다";
    private const string ErosionDeadLabel = "…손이 멋대로\n움직인다";
    private const string WarningText = "…손이 떨린다. 내 의지로 선택할 수 있는 건 이번 한 번뿐인가...";

    private const string GlitchChars = "#@%&?!/|*+~";

    private bool isPlaying;
    private bool finished;
    private bool[] hudWasActive;
    private float originalGravityScale;
    private bool playerGravityCached;

    private Color dialogueDefaultColor = Color.white;
    private Color nameDefaultColor = Color.white;
    private Color promptDefaultColor = Color.white;

    private Coroutine shakeRoutine;
    private Vector3 shakeBasePosition;
    private float shakeAmplitude;
    private Coroutine vignetteRoutine;
    private Coroutine optionShakeRoutine;
    private Coroutine promptGlitchRoutine;

    // 방금 출력한 대사에서 계속 깨진 채로 남는 글자 위치 (5개 전용)
    private bool[] lineDamage;

    private void Awake()
    {
        if (heroineTransform == null)
            heroineTransform = transform;

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

        // 원래 글자색 기억 (기계 대사 이후 되돌리기용)
        if (dialogueText != null) dialogueDefaultColor = dialogueText.color;
        if (characterNameText != null) nameDefaultColor = characterNameText.color;
        if (choicePromptText != null) promptDefaultColor = choicePromptText.color;

        // 비네트 준비: 스프라이트가 없으면 코드로 생성
        if (vignetteImage != null)
        {
            if (vignetteImage.sprite == null)
                vignetteImage.sprite = CreateVignetteSprite();

            vignetteImage.raycastTarget = false;
            SetVignetteAlpha(0f);
            vignetteImage.gameObject.SetActive(false);
        }

        // 두 패널은 항상 꺼진 상태로 시작합니다.
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);
    }

    //============== 외부 호출 (HeroineDialogueTrigger) ==============
    public bool TryPlay()
    {
        if (isPlaying || finished)
            return false;

        // 이번 런에서 이미 히로인 선택을 했다면 다시 재생하지 않습니다.
        if (SaveData.Instance != null &&
            SaveData.Instance.run.heroineChoice != HeroineEndingChoice.None)
        {
            finished = true;
            return false;
        }

        isPlaying = true;
        StartCoroutine(PlayRoutine());
        return true;
    }

    //============== 테스트용: 다시 재생 가능하게 ==============
    [ContextMenu("테스트: 다시 재생 가능하게 초기화")]
    private void ResetForTest()
    {
        finished = false;
        isPlaying = false;

        if (SaveData.Instance != null)
            SaveData.Instance.run.heroineChoice = HeroineEndingChoice.None;

        Debug.Log("[HeroineDialogueController] 테스트 초기화 완료", this);
    }

    //============== 대사 정의 ==============
    //============== 히로인 첫 대사 ==============
    private static Line[] BuildIntroLines()
    {
        return new[]
        {
            new Line(HeroineName, "…왔구나."),
            new Line(HeroineName, "여기까지 오는 길, 쉽지 않았을 거야."),
            new Line(HeroineName, "그래도 결국 여기까지 왔네. 이제 진실을 알게 됐구나."),
            new Line(HeroineName, "한 가지만 물을게. 나를… 어떻게 할 거야?"),
        };
    }

    //============== 기계장비 3개: 복수를 부추기는 속삭임 ==============
    private static Line[] BuildWhisperLines()
    {
        return new[]
        {
            new Line(MachineName, "잊지 마라. 네 가족을 몰살한 원흉을.", true),
            new Line(MachineName, "그자에게 복수할 기회가 눈앞에 있다.", true),
            new Line(MachineName, "그런데도 망설일 셈인가? 저 아이 때문에?", true),
        };
    }

    //============== 기계장비 4~5개: 복수심에 의한 침식 ==============
    private static Line[] BuildErosionLines(int machineCount)
    {
        if (machineCount >= 5)
        {
            return new[]
            {
                new Line(MachineName, "복수를 완성해라.", true),
                new Line(MachineName, "네 가족의 원수를 갚아라. 다른 모든 것은 버려라.", true),
                new Line(MachineName, "망설임은 필요 없다. 네 손으로 끝내라.", true),
            };
        }

        return new[]
        {
            new Line(MachineName, "네 가족을 몰살한 원흉을 떠올려라.", true),
            new Line(MachineName, "복수를 눈앞에 두고 물러설 셈인가?", true),
        };
    }

    //============== 히로인을 살렸을 때 ==============
    private static Line[] BuildAliveReactionLines()
    {
        return new[]
        {
            new Line(HeroineName, "…정말 날 살려 주는 거야?"),
            new Line(HeroineName, "고마워. 이번엔 끝까지 네 곁에 있을게."),
        };
    }
    
    //============== 히로인을 죽였을 때 ==============
    private static Line[] BuildDeadReactionLines(int machineCount)
    {
        // 기계장비 3개: 히든엔딩으로 이어지는 복선
        if (machineCount == 3)
        {
            return new[]
            {
                new Line(HeroineName, "…부탁이야. 그 사람을 구해 줘..."),
            };
        }
        // 기계장비 5개: 기계에 침식된 주인공과 영애의 마지막 대화
        if (machineCount >= 5)
        {
            return new[]
            {
                new Line(HeroineName, "……잠깐만."),
                new Line(HeroineName, "왜 그런 눈으로 나를 보는 거야……?"),
                new Line(HeroineName, "너잖아. 나를 구하러 여기까지 온 사람은…… 너잖아."),
                new Line(HeroineName, "그런데 왜…… 네 손이 네 뜻대로 움직이지 않는 것처럼……."),
                new Line(HeroineName, "……아. 그렇구나. 너는 아직 그 안에 있는 거구나."),
                new Line(HeroineName, "괜찮아…… 네가 아니라고 믿고 싶어."),
                new Line(HeroineName, "……미안해. 끝까지 네 손을 잡아 주지 못해서."),
            };
        }
        // 기계장비 4개: 기존 대사 유지
        return new[]
        {
            new Line(HeroineName, "…괜찮아. 널 원망하지 않을게."),
        };
    }

    //============== 전체 흐름 ==============
    private IEnumerator PlayRoutine()
    {
        CameraSnapshot snapshot = CaptureCamera();
        HideHud();
        LockPlayer();

        yield return MoveCameraToPair();

        int machineCount = EndingManager.Instance != null
            ? EndingManager.Instance.MachineEquipmentCount
            : 0;

        // 5개: 대화 시작부터 진한 비네트가 맥동합니다.
        if (machineCount >= 5)
            ShowVignette(vignetteAlpha5, true);

        if (panelShowDelay > 0f)
            yield return new WaitForSecondsRealtime(panelShowDelay);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        //============== 대사 ==============
        yield return PlayLines(BuildIntroLines(), machineCount);

        if (machineCount >= 4)
            yield return PlayLines(BuildErosionLines(machineCount), machineCount);
        else if (machineCount == 3)
            yield return PlayLines(BuildWhisperLines(), machineCount);

        // 5개는 선택지까지 떨림이 이어지고, 3~4개는 기계 대사가 끝나면 멈춥니다.
        if (machineCount < 5)
            StopShake();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        //============== 선택지 ==============
        bool alive = true;
        yield return ShowChoice(machineCount, result => alive = result);

        StopShake();

        if (EndingManager.Instance != null)
        {
            EndingManager.Instance.SetHeroineChoice(
                alive ? HeroineEndingChoice.Alive : HeroineEndingChoice.Dead);
        }
        
        //============== 선택 후 죽음 연출 ==============
        if (!alive && machineCount >= 5)
        {
            // 기계의 강제 명령이 실행되는 순간 붉은 화면과 충격을 강조합니다.
            ShowVignette(0.95f, true);
            StartShake(cameraShake5 * 2.5f);

            yield return new WaitForSecondsRealtime(0.35f);

            StopShake();

            // 영애의 마지막 대사가 나오기 전 짧은 정적
            yield return new WaitForSecondsRealtime(0.4f);
        }

        //============== 선택 후 반응 대사 ==============
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        yield return PlayLines(
            alive
                ? BuildAliveReactionLines()
                : BuildDeadReactionLines(machineCount),
            machineCount);

        //============== 영애의 죽음 이후 여운 ==============
        if (!alive && machineCount >= 5)
        {
            yield return new WaitForSecondsRealtime(0.8f);
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        //============== 종료 ==============
        StopShake();
        HideVignette();

        yield return RestoreCamera(snapshot);
        RestoreHud();
        UnlockPlayer();

        finished = true;
        isPlaying = false;
    }

    //============== 대사 출력 ==============
    private IEnumerator PlayLines(Line[] lines, int machineCount)
    {
        if (lines == null)
            yield break;

        foreach (Line line in lines)
        {
            ApplyLineStyle(line);

            float speed = typeSpeed;
            float hold = lineHoldTime;
            int glitchLevel = 0;   // 0=없음, 1=4개(출력 중 깨짐), 2=5개(심하게 깨지고 오래 남음)

            if (line.machine)
            {
                // 기계 목소리가 나오는 동안 카메라가 약하게 떨립니다. (3개 이상)
                if (machineCount >= 3)
                    StartShake(GetMachineShake(machineCount));

                if (machineCount >= 5)
                {
                    speed = typeSpeed * 0.35f;
                    hold = lineHoldTime * 0.6f + glitchLingerTime5;
                    glitchLevel = 2;
                }
                else
                {
                    speed = typeSpeed * 1.6f;
                    glitchLevel = machineCount >= 4 ? 1 : 0;
                }
            }
            else
            {
                StopShake();
            }

            if (characterNameText != null)
                characterNameText.text = line.name;

            yield return TypeTo(dialogueText, line.text, speed, glitchLevel);

            // 5개: 대사가 다 나온 뒤에도 글자가 깨진 채 일렁이며 남습니다.
            if (glitchLevel >= 2)
                yield return HoldWithGlitch(dialogueText, line.text, hold);
            else
                yield return WaitOrSkip(hold);
        }
    }

    private float GetMachineShake(int machineCount)
    {
        if (machineCount >= 5) return cameraShake5;
        if (machineCount == 4) return cameraShake4;
        return cameraShake3;
    }

    // 기계 대사는 글자색/이름색을 바꾸고, 영애 대사는 원래대로 되돌립니다.
    private void ApplyLineStyle(Line line)
    {
        if (dialogueText != null)
            dialogueText.color = line.machine ? machineTextColor : dialogueDefaultColor;

        if (characterNameText != null)
            characterNameText.color = line.machine ? machineNameColor : nameDefaultColor;
    }

    // glitchLevel: 0=깨짐 없음 / 1=출력 중에만 깨짐(끝나면 정상) / 2=심하게 깨지고 일부는 끝까지 남음
    private IEnumerator TypeTo(TMP_Text target, string line, float speed, int glitchLevel)
    {
        if (target == null)
            yield break;

        target.text = "";

        lineDamage = glitchLevel >= 2 ? RollDamage(line, 0.2f) : null;

        int tailCount = glitchLevel >= 2 ? 6 : 3;
        float tailChance = glitchLevel >= 2 ? 0.65f : 0.4f;
        float anyChance = glitchLevel >= 2 ? 0.08f : 0f;

        for (int i = 0; i < line.Length; i++)
        {
            string shown = line.Substring(0, i + 1);

            if (glitchLevel > 0)
                shown = CorruptText(shown, lineDamage, tailCount, tailChance, anyChance);

            target.text = shown;

            if (speed > 0f)
                yield return new WaitForSecondsRealtime(speed);
            else
                yield return null;
        }

        // 1단계는 정상 글자로 정리, 2단계는 일부가 깨진 채로 남깁니다.
        target.text = glitchLevel >= 2
            ? CorruptText(line, lineDamage, 0, 0f, 0f)
            : line;
    }

    // 출력이 끝난 뒤에도 깨진 글자가 계속 바뀌며 남아 있게 합니다. F키로 건너뛸 수 있습니다.
    private IEnumerator HoldWithGlitch(TMP_Text target, string line, float seconds)
    {
        if (target == null)
            yield break;

        bool[] damage = lineDamage;
        float elapsed = 0f;
        float tick = 0f;

        while (elapsed < seconds)
        {
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                yield break;

            tick -= Time.unscaledDeltaTime;

            if (tick <= 0f)
            {
                target.text = CorruptText(line, damage, 0, 0f, 0.12f);
                tick = 0.07f;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    // 선택지 질문 등을 계속 일렁이는 깨진 글씨로 유지합니다. (5개 전용, 코루틴을 멈출 때까지 반복)
    private IEnumerator GlitchLoop(TMP_Text target, string line, bool[] damage)
    {
        while (true)
        {
            target.text = CorruptText(line, damage, 0, 0f, 0.1f);
            yield return new WaitForSecondsRealtime(0.07f);
        }
    }

    // 글자 위치마다 지속적으로 깨질 칸을 미리 정합니다.
    private static bool[] RollDamage(string line, float chance)
    {
        bool[] mask = new bool[line.Length];

        for (int i = 0; i < line.Length; i++)
            mask[i] = line[i] != ' ' && UnityEngine.Random.value < chance;

        return mask;
    }

    // damaged에 표시된 글자는 항상, 끝 tailCount글자는 tailChance 확률로, 나머지는 anyChance 확률로 깨뜨립니다.
    private static string CorruptText(
        string text,
        bool[] damaged,
        int tailCount,
        float tailChance,
        float anyChance)
    {
        char[] chars = text.ToCharArray();
        int tailStart = Mathf.Max(0, chars.Length - tailCount);

        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] == ' ' || chars[i] == '\n')
                continue;

            bool corrupt = false;

            if (damaged != null && i < damaged.Length && damaged[i])
                corrupt = true;
            else if (tailCount > 0 && i >= tailStart && UnityEngine.Random.value < tailChance)
                corrupt = true;
            else if (anyChance > 0f && UnityEngine.Random.value < anyChance)
                corrupt = true;

            if (corrupt)
                chars[i] = GlitchChars[UnityEngine.Random.Range(0, GlitchChars.Length)];
        }

        return new string(chars);
    }

    private IEnumerator WaitOrSkip(float seconds)
    {
        float elapsed = 0f;

        while (elapsed < seconds)
        {
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                yield break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    //============== 선택지 ==============
    // 좌우 방향키로 이동, F키로 확정합니다.
    // 기계장비 5개(침식)면 두 선택지 모두 변형되고, 어느 쪽을 확정해도 결과는 사망입니다.
    private IEnumerator ShowChoice(int machineCount, Action<bool> onDone)
    {
        if (choicePanel == null || choicePromptText == null
            || aliveOptionText == null || deadOptionText == null)
        {
            Debug.LogError("[HeroineDialogueController] 선택지 UI가 연결되지 않았습니다.", this);
            onDone?.Invoke(true);
            yield break;
        }

        EndingManager.ChoiceRestriction restriction =
            EndingManager.Instance != null
                ? EndingManager.Instance.GetHeroineRestriction()
                : EndingManager.ChoiceRestriction.Free;

        bool erosion = restriction == EndingManager.ChoiceRestriction.ForceDead;
        bool canDead = restriction != EndingManager.ChoiceRestriction.ForceAlive;

        // 4개: 선택지가 뜰 때 약한 붉은 비네트
        if (machineCount == 4)
            ShowVignette(vignetteAlpha4, false);

        choicePromptText.text = "";
        choicePromptText.color = erosion ? machineTextColor : promptDefaultColor;
        aliveOptionText.gameObject.SetActive(false);
        deadOptionText.gameObject.SetActive(false);
        choicePanel.SetActive(true);

        int promptGlitch = machineCount >= 5 ? 2 : (machineCount == 4 ? 1 : 0);
        yield return TypeTo(choicePromptText, ChoicePrompt, typeSpeed, promptGlitch);

        // 4개: 질문 아래에 붉은 경고 문구(작은 글씨)를 덧붙입니다.
        if (machineCount == 4)
        {
            choicePromptText.text =
                ChoicePrompt +
                "\n<size=" + warningFontSize + "><color=#D02020>" + WarningText + "</color></size>";
        }

        // 5개: 질문 글씨가 계속 깨진 채로 일렁입니다.
        if (promptGlitch >= 2)
            promptGlitchRoutine = StartCoroutine(GlitchLoop(choicePromptText, ChoicePrompt, lineDamage));

        bool aliveSelected = true;
        aliveOptionText.gameObject.SetActive(true);
        deadOptionText.gameObject.SetActive(canDead);
        RefreshChoiceVisual(aliveSelected, erosion);

        // 5개: 두 선택지 글자가 계속 흔들립니다.
        Vector2 aliveBase = aliveOptionText.rectTransform.anchoredPosition;
        Vector2 deadBase = deadOptionText.rectTransform.anchoredPosition;

        if (erosion && optionShakeAmount > 0f)
            optionShakeRoutine = StartCoroutine(OptionShakeRoutine(aliveBase, deadBase));

        if (choiceInputDelay > 0f)
            yield return new WaitForSecondsRealtime(choiceInputDelay);

        
        while (true)
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null)
            {
                if (canDead)
                {
                    //============== 기계장비 5개: 살리려는 선택을 강제로 되돌림 ==============
                    if (erosion && keyboard.leftArrowKey.wasPressedThisFrame)
                    {
                        if (!aliveSelected)
                        {
                            // 잠시 살리는 선택이 된 것처럼 보입니다.
                            aliveSelected = true;
                            RefreshChoiceVisual(aliveSelected, erosion);

                            // 기계가 저항을 억누르는 듯 화면을 강하게 흔듭니다.
                            StartShake(cameraShake5 * 1.8f);

                            yield return new WaitForSecondsRealtime(0.25f);

                            // 기계가 강제로 죽이는 선택으로 되돌립니다.
                            aliveSelected = false;
                            RefreshChoiceVisual(aliveSelected, erosion);

                            StartShake(cameraShake5 * 2.2f);
                            yield return new WaitForSecondsRealtime(0.12f);
                        }
                    }
                    //============== 일반 선택지 또는 오른쪽 선택 ==============
                    else if (keyboard.leftArrowKey.wasPressedThisFrame && !erosion && !aliveSelected)
                    {
                        aliveSelected = true;
                        RefreshChoiceVisual(aliveSelected, erosion);
                    }
                    else if (keyboard.rightArrowKey.wasPressedThisFrame && aliveSelected)
                    {
                        aliveSelected = false;
                        RefreshChoiceVisual(aliveSelected, erosion);
                    }
                }

                //============== F키 확정 ==============
                if (keyboard.fKey.wasPressedThisFrame)
                {
                    // 기계장비 5개: 살리는 선택을 확정하려 해도 죽이는 선택으로 강제 변경
                    if (erosion && aliveSelected)
                    {
                        aliveSelected = false;
                        RefreshChoiceVisual(aliveSelected, erosion);

                        StartShake(cameraShake5 * 2.2f);
                        yield return new WaitForSecondsRealtime(0.3f);
                    }
                    break;
                }
            }
            yield return null;
        }


        // 5개: 확정하면 선택한 글자가 깜빡입니다.
        if (erosion)
            yield return BlinkOption(aliveSelected ? aliveOptionText : deadOptionText);

        if (optionShakeRoutine != null)
        {
            StopCoroutine(optionShakeRoutine);
            optionShakeRoutine = null;
        }

        if (promptGlitchRoutine != null)
        {
            StopCoroutine(promptGlitchRoutine);
            promptGlitchRoutine = null;
        }

        aliveOptionText.rectTransform.anchoredPosition = aliveBase;
        deadOptionText.rectTransform.anchoredPosition = deadBase;
        aliveOptionText.enabled = true;
        deadOptionText.enabled = true;

        choicePanel.SetActive(false);

        // 4개의 비네트는 선택이 끝나면 사라집니다. (5개는 대화가 끝날 때 사라짐)
        if (machineCount == 4)
            HideVignette();

        // 침식 상태에서는 무엇을 골라도 사망으로 확정됩니다.
        onDone?.Invoke(erosion ? false : aliveSelected);
    }

    private void RefreshChoiceVisual(bool aliveSelected, bool erosion)
    {
        if (erosion)
        {
            aliveOptionText.text = aliveSelected ? ErosionAliveLabel + " (F)" : ErosionAliveLabel;
            deadOptionText.text = !aliveSelected ? ErosionDeadLabel + " (F)" : ErosionDeadLabel;

            Color bright = machineTextColor;
            Color dim = new Color(machineTextColor.r, machineTextColor.g, machineTextColor.b, 0.45f);

            aliveOptionText.color = aliveSelected ? bright : dim;
            deadOptionText.color = !aliveSelected ? bright : dim;
            return;
        }

        aliveOptionText.text = aliveSelected ? AliveOptionLabel + " (F)" : AliveOptionLabel;
        deadOptionText.text = !aliveSelected ? DeadOptionLabel + " (F)" : DeadOptionLabel;

        aliveOptionText.color = aliveSelected ? selectedColor : unselectedColor;
        deadOptionText.color = !aliveSelected ? selectedColor : unselectedColor;
    }

    private IEnumerator OptionShakeRoutine(Vector2 aliveBase, Vector2 deadBase)
    {
        while (true)
        {
            aliveOptionText.rectTransform.anchoredPosition =
                aliveBase + UnityEngine.Random.insideUnitCircle * optionShakeAmount;
            deadOptionText.rectTransform.anchoredPosition =
                deadBase + UnityEngine.Random.insideUnitCircle * optionShakeAmount;

            yield return new WaitForSecondsRealtime(0.03f);
        }
    }

    private IEnumerator BlinkOption(TMP_Text target)
    {
        for (int i = 0; i < 6; i++)
        {
            target.enabled = !target.enabled;
            yield return new WaitForSecondsRealtime(0.06f);
        }

        target.enabled = true;
    }

    //============== 카메라 떨림 ==============
    private void StartShake(float amplitude)
    {
        shakeAmplitude = amplitude;

        // 이미 떨리는 중이면 세기만 갱신합니다.
        if (shakeRoutine != null)
            return;

        if (targetCamera == null || amplitude <= 0f)
            return;

        shakeBasePosition = targetCamera.transform.position;
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        while (true)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * shakeAmplitude;
            targetCamera.transform.position =
                shakeBasePosition + new Vector3(offset.x, offset.y, 0f);

            yield return null;
        }
    }

    private void StopShake()
    {
        if (shakeRoutine == null)
            return;

        StopCoroutine(shakeRoutine);
        shakeRoutine = null;

        if (targetCamera != null)
            targetCamera.transform.position = shakeBasePosition;
    }

    //============== 붉은 비네트 ==============
    private void ShowVignette(float targetAlpha, bool pulse)
    {
        if (vignetteImage == null)
            return;

        if (vignetteRoutine != null)
            StopCoroutine(vignetteRoutine);

        vignetteImage.gameObject.SetActive(true);
        vignetteRoutine = StartCoroutine(VignetteRoutine(targetAlpha, pulse));
    }

    private void HideVignette()
    {
        if (vignetteImage == null || !vignetteImage.gameObject.activeSelf)
            return;

        if (vignetteRoutine != null)
            StopCoroutine(vignetteRoutine);

        vignetteRoutine = StartCoroutine(VignetteFadeOut());
    }

    private IEnumerator VignetteRoutine(float targetAlpha, bool pulse)
    {
        float startAlpha = vignetteImage.color.a;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, vignetteFadeTime);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetVignetteAlpha(Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetVignetteAlpha(targetAlpha);

        // 5개: 심장이 뛰듯 천천히 맥동합니다.
        while (pulse)
        {
            float wave = 0.8f + 0.2f * Mathf.Sin(Time.unscaledTime * 3f);
            SetVignetteAlpha(targetAlpha * wave);
            yield return null;
        }
    }

    private IEnumerator VignetteFadeOut()
    {
        float startAlpha = vignetteImage.color.a;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, vignetteFadeTime);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetVignetteAlpha(Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetVignetteAlpha(0f);
        vignetteImage.gameObject.SetActive(false);
        vignetteRoutine = null;
    }

    private void SetVignetteAlpha(float alpha)
    {
        if (vignetteImage == null)
            return;

        Color c = vignetteColor;
        c.a = alpha;
        vignetteImage.color = c;
    }

    // 중앙은 투명, 가장자리로 갈수록 불투명한 그라데이션 스프라이트를 코드로 만듭니다.
    private static Sprite CreateVignetteSprite()
    {
        const int size = 128;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2((size - 1) / 2f, (size - 1) / 2f);
        float maxDistance = center.magnitude;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / maxDistance;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, d));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f));
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

    //============== HUD 숨김/복구 ==============
    private void HideHud()
    {
        if (hudObjectsToHide == null)
            return;

        hudWasActive = new bool[hudObjectsToHide.Length];

        for (int i = 0; i < hudObjectsToHide.Length; i++)
        {
            GameObject obj = hudObjectsToHide[i];
            if (obj == null) continue;

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

    //============== 카메라 ==============
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

    // 플레이어와 히로인의 중간 지점, 두 캐릭터가 모두 화면에 들어오는 줌
    private void GetPairFocus(out Vector3 position, out float size)
    {
        Vector3 playerPos = playerController != null
            ? playerController.transform.position
            : transform.position;
        Vector3 heroinePos = heroineTransform.position;
        Vector3 mid = (playerPos + heroinePos) * 0.5f;

        position = new Vector3(
            mid.x + cameraFocusOffset.x,
            mid.y + cameraFocusOffset.y,
            cameraFocusOffset.z);

        float aspect = targetCamera != null ? targetCamera.aspect : 16f / 9f;
        float halfWidth = Mathf.Abs(playerPos.x - heroinePos.x) * 0.5f + pairPadding;
        float halfHeight = Mathf.Abs(playerPos.y - heroinePos.y) * 0.5f + pairPadding;

        size = Mathf.Max(minZoomSize, halfWidth / aspect, halfHeight);
    }

    private IEnumerator MoveCameraToPair()
    {
        if (targetCamera == null)
            yield break;

        GetPairFocus(out Vector3 targetPosition, out float targetSize);
        yield return BlendCamera(targetPosition, targetSize);
    }

    private IEnumerator RestoreCamera(CameraSnapshot snapshot)
    {
        if (targetCamera == null || snapshot == null)
        {
            RestoreCameraFollow(snapshot);
            yield break;
        }

        yield return BlendCamera(snapshot.position, snapshot.orthographicSize);
        RestoreCameraFollow(snapshot);
    }

    private IEnumerator BlendCamera(Vector3 targetPosition, float targetSize)
    {
        Vector3 startPosition = targetCamera.transform.position;
        float startSize = targetCamera.orthographicSize;
        float duration = Mathf.Max(0.01f, cameraBlendTime);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            float size = Mathf.Lerp(startSize, targetSize, t);
            Vector3 position = Vector3.Lerp(startPosition, targetPosition, t);

            targetCamera.orthographicSize = size;
            targetCamera.transform.position = ClampCamera(position, size);

            yield return null;
        }

        targetCamera.orthographicSize = targetSize;
        targetCamera.transform.position = ClampCamera(targetPosition, targetSize);
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
}