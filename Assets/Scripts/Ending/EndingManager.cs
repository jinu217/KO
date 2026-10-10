using UnityEngine;

public class EndingManager : MonoBehaviour
{
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

    //============== 기계장비 획득 ==============
    public void AddMachineEquipment()
    {
        if (SaveData.Instance == null)
            return;

        SaveData.Instance.run.machineEquipmentCount++;

        Debug.Log(
            $"[EndingManager] 기계장비 획득 " +
            $"현재 개수 : {SaveData.Instance.run.machineEquipmentCount}"
        );
    }

    //============== 영애 생존/사망 선택 ==============
    public void SetHeroineChoice(HeroineEndingChoice choice)
    {
        if (SaveData.Instance == null)
            return;

        SaveData.Instance.run.heroineChoice = choice;

        Debug.Log(
            $"[EndingManager] 영애 선택 : {choice}"
        );
    }

    //============== 라스트보스 생존/사망 선택 ==============
    public void SetLastBossChoice(LastBossEndingChoice choice)
    {
        if (SaveData.Instance == null)
            return;

        SaveData.Instance.run.lastBossChoice = choice;

        Debug.Log(
            $"[EndingManager] 라스트보스 선택 : {choice}"
        );
    }

    //============== 엔딩 판정 ==============
    public EndingType DetermineEnding()
    {
        if (SaveData.Instance == null)
        {
            Debug.LogWarning("[EndingManager] SaveData가 없습니다.");
            return EndingType.None;
        }

        RunData run = SaveData.Instance.run;

        if (run == null)
        {
            Debug.LogWarning("[EndingManager] RunData가 없습니다.");
            return EndingType.None;
        }

        int machineCount = run.machineEquipmentCount;

        bool heroineAlive =
            run.heroineChoice == HeroineEndingChoice.Alive;

        bool heroineDead =
            run.heroineChoice == HeroineEndingChoice.Dead;

        bool lastBossAlive =
            run.lastBossChoice == LastBossEndingChoice.Alive;

        bool lastBossDead =
            run.lastBossChoice == LastBossEndingChoice.Dead;


        //============== 진엔딩 ==============
        if (machineCount == 0 &&
            heroineAlive &&
            lastBossAlive)
        {
            return EndingType.TrueEnding;
        }


        //============== 히든엔딩 ==============
        if (machineCount == 3 &&
            heroineDead &&
            lastBossAlive)
        {
            return EndingType.HiddenEnding;
        }


        //============== 배드엔딩 ==============
        if (machineCount == 5 &&
            heroineDead &&
            lastBossDead)
        {
            return EndingType.BadEnding;
        }


        //============== 노말 엔딩 1 ==============
        if (heroineAlive &&
            lastBossDead)
        {
            return EndingType.NormalEnding1;
        }


        //============== 노말 엔딩 2 ==============
        if (heroineDead &&
            lastBossAlive)
        {
            return EndingType.NormalEnding2;
        }


        //============== 해피엔딩 ==============
        if (heroineAlive &&
            lastBossAlive)
        {
            return EndingType.HappyEnding;
        }


        Debug.LogWarning(
            $"[EndingManager] 엔딩 조건에 해당하지 않습니다.\n" +
            $"기계장비 : {machineCount}\n" +
            $"영애 : {run.heroineChoice}\n" +
            $"라스트보스 : {run.lastBossChoice}"
        );

        return EndingType.None;
    }
}