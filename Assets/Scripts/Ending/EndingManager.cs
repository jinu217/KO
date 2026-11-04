using UnityEngine;

public class EndingManager : MonoBehaviour
{
    //============== 테스트용 (에디터 전용) ==============
    [Header("테스트용 (에디터에서만 적용)")]
    [Tooltip("-1이면 실제 장비 개수 사용. 0~5로 두면 그 개수로 동작")]
    [SerializeField, Range(-1, 5)] private int debugMachineCount = -1;

    [Tooltip("체크하면 아래 '테스트 히로인 선택'을 실제 선택 대신 사용합니다. 테스트 후 반드시 해제하세요.")]
    [SerializeField] private bool useDebugHeroineChoice = false;

    [Tooltip("useDebugHeroineChoice가 켜져 있을 때 강제로 기록할 히로인 선택")]
    [SerializeField] private HeroineEndingChoice debugHeroineChoice = HeroineEndingChoice.Alive;

    //============== 선택지 제한 종류 ==============
    public enum ChoiceRestriction
    {
        Free,        // 자의로 생존/사망 모두 선택 가능
        ForceAlive,  // 생존(회유)만 가능
        ForceDead    // 사망만 가능 (침식)
    }

    public static EndingManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ApplyDebugHeroineChoice();
    }

    //============== 테스트용: 히로인 선택 강제 적용 (에디터 전용) ==============
    // 5스테이지 씬을 단독으로 테스트할 때 히로인 선택이 None이라
    // 엔딩 판정이 실패하는 것을 막습니다. 빌드에서는 아무 동작도 하지 않습니다.
    private void ApplyDebugHeroineChoice()
    {
#if UNITY_EDITOR
        if (!useDebugHeroineChoice)
            return;

        if (SaveData.Instance == null || SaveData.Instance.run == null)
        {
            Debug.LogWarning("[EndingManager] 테스트용 히로인 선택을 적용하지 못했습니다. SaveData가 없습니다.");
            return;
        }

        if (SaveData.Instance.run.heroineChoice == debugHeroineChoice)
            return;

        SaveData.Instance.run.heroineChoice = debugHeroineChoice;
        Debug.Log($"[EndingManager] (테스트) 히로인 선택 강제 적용 : {debugHeroineChoice}");
#endif
    }

    //============== 테스트용: 현재 상태 로그 (에디터 전용) ==============
    // 보스 처치 직후 MainBossDialogueController에서 자동으로 호출됩니다.
    // 플레이 중 EndingManager 컴포넌트 우측 ⋮ 메뉴에서 직접 실행할 수도 있습니다.
    public void LogEndingState(string label)
    {
#if UNITY_EDITOR
        if (SaveData.Instance == null || SaveData.Instance.run == null)
        {
            Debug.LogWarning($"[EndingManager] ({label}) SaveData 또는 RunData가 없습니다.");
            return;
        }

        ApplyDebugHeroineChoice();

        RunData run = SaveData.Instance.run;
        ChoiceRestriction restriction = GetLastBossRestriction();

        string available;
        switch (restriction)
        {
            case ChoiceRestriction.ForceDead:  available = "[죽인다]만 표시"; break;
            case ChoiceRestriction.ForceAlive: available = "[살린다]만 표시 (회유 루트)"; break;
            default:                           available = "[살린다] / [죽인다] 모두 표시"; break;
        }

        Debug.Log(
            $"[EndingManager] ===== {label} =====\n" +
            $"기계장비: {MachineEquipmentCount}\n" +
            $"영애 선택: {run.heroineChoice}\n" +
            $"라스트보스 선택: {run.lastBossChoice}\n" +
            $"라스트보스 선택 제한: {restriction}\n" +
            $"선택지: {available}");
#endif
    }

    [ContextMenu("현재 엔딩 상태 로그 출력")]
    private void LogCurrentEndingState()
    {
        LogEndingState("수동 확인");
    }

    //============== 장착 중인 기계장비 개수 ==============
    // EquipmentManager가 RunData에 저장해 둔 슬롯별 아이템 ID로 직접 셉니다.
    public int MachineEquipmentCount
    {
        get
        {
    #if UNITY_EDITOR
            if (debugMachineCount >= 0)
                return debugMachineCount;
    #endif
            if (SaveData.Instance == null || SaveData.Instance.run == null)
                return 0;

            RunData run = SaveData.Instance.run;

            return CountMechanization(run.weaponItemId)
                + CountMechanization(run.handItemId)
                + CountMechanization(run.headItemId)
                + CountMechanization(run.bodyItemId)
                + CountMechanization(run.footItemId);
        }
    }

    private int CountMechanization(int itemId)
    {
        if (itemId < 0)
            return 0;

        ItemData item = SaveData.Instance.GetItemById(itemId);

        return item != null && item.rarity == ItemRarity.Mechanization ? 1 : 0;
    }

    //============== 히로인 선택지 제한 ==============
    public ChoiceRestriction GetHeroineRestriction()
    {
        // 5개: 침식으로 강제 사망
        if (MachineEquipmentCount >= 5)
            return ChoiceRestriction.ForceDead;

        // 0~4개: 자의로 선택 (4개일 때 이 선택이 '견뎌내는 1회'가 됩니다)
        return ChoiceRestriction.Free;
    }

    //============== 라스트보스 선택지 제한 ==============
    public ChoiceRestriction GetLastBossRestriction()
    {
        // 테스트용 히로인 선택이 켜져 있으면 판정 직전에 한 번 더 적용합니다.
        ApplyDebugHeroineChoice();

        int count = MachineEquipmentCount;

        // 4~5개: 침식으로 강제 사망 (4개는 히로인 선택에서 이미 한 번 견딘 상태)
        if (count >= 4)
            return ChoiceRestriction.ForceDead;

        // 3개: 히로인을 죽였다면 라스트보스는 회유만 가능 → 히든엔딩
        if (count == 3 && SaveData.Instance != null &&
            SaveData.Instance.run.heroineChoice == HeroineEndingChoice.Dead)
            return ChoiceRestriction.ForceAlive;

        return ChoiceRestriction.Free;
    }

    //============== 영애 생존/사망 선택 ==============
    public void SetHeroineChoice(HeroineEndingChoice choice)
    {
        if (SaveData.Instance == null)
            return;

        SaveData.Instance.run.heroineChoice = choice;

        Debug.Log($"[EndingManager] 영애 선택 : {choice}");
    }

    //============== 라스트보스 생존/사망 선택 ==============
    public void SetLastBossChoice(LastBossEndingChoice choice)
    {
        if (SaveData.Instance == null)
            return;

        SaveData.Instance.run.lastBossChoice = choice;

        Debug.Log($"[EndingManager] 라스트보스 선택 : {choice}");
    }

    //============== 엔딩 판정 ==============
    public EndingType DetermineEnding()
    {
        if (SaveData.Instance == null || SaveData.Instance.run == null)
        {
            Debug.LogWarning("[EndingManager] SaveData 또는 RunData가 없습니다.");
            return EndingType.None;
        }

        // 테스트용 히로인 선택이 켜져 있으면 판정 직전에 한 번 더 적용합니다.
        ApplyDebugHeroineChoice();

        RunData run = SaveData.Instance.run;
        int machineCount = MachineEquipmentCount;

        //============== 선택 여부 확인 ==============
        if (run.heroineChoice == HeroineEndingChoice.None ||
            run.lastBossChoice == LastBossEndingChoice.None)
        {
            Debug.LogWarning("[EndingManager] 영애 또는 라스트보스 선택이 완료되지 않았습니다.");
            return EndingType.None;
        }

        bool heroineAlive = run.heroineChoice == HeroineEndingChoice.Alive;
        bool lastBossAlive = run.lastBossChoice == LastBossEndingChoice.Alive;

        if (heroineAlive && lastBossAlive)
        {
            //============== 생존 / 생존 ==============
            if (machineCount == 0) return EndingType.TrueEnding;    // 0개
            if (machineCount <= 3) return EndingType.HappyEnding;   // 1~3개
        }
        else if (heroineAlive && !lastBossAlive)
        {
            //============== 생존 / 사망 ==============
            if (machineCount <= 4) return EndingType.NormalEnding1; // 0~4개
        }
        else if (!heroineAlive && lastBossAlive)
        {
            //============== 사망 / 생존 ==============
            if (machineCount <= 2) return EndingType.NormalEnding2; // 0~2개
            if (machineCount == 3) return EndingType.HiddenEnding;  // 3개
        }
        else
        {
            //============== 사망 / 사망 ==============
            return EndingType.BadEnding;                            // 0~5개
        }

        Debug.LogWarning(
            $"[EndingManager] 해당하는 엔딩이 없습니다.\n" +
            $"기계장비: {machineCount}\n" +
            $"영애: {run.heroineChoice}\n" +
            $"라스트보스: {run.lastBossChoice}");

        return EndingType.None;
    }
}