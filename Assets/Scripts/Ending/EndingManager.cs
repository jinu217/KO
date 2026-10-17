using UnityEngine;

public class EndingManager : MonoBehaviour
{
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

    //============== 장착 중인 기계장비 개수 ==============
    // EquipmentManager가 RunData에 저장해 둔 슬롯별 아이템 ID로 직접 셉니다.
    public int MachineEquipmentCount
    {
        get
        {
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