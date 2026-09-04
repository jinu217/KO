using UnityEngine;

// 중간보스와 같은 데이터 실행기를 사용하고 체력 기반 페이즈와 메인보스 UI만 확장한다.
public class MainBossController : MidBossController
{
    protected MainBossData MainData => data as MainBossData;

    protected override void UpdatePhase()
    {
        if (MainData == null || MainData.maxHp <= 0f) { currentPhase = 1; return; }
        float hpRate = currentHp / MainData.maxHp;
        if (hpRate <= MainData.phase3HpPercent) currentPhase = 3;
        else if (hpRate <= MainData.phase2HpPercent) currentPhase = 2;
        else currentPhase = 1;
    }

    protected override void ShowHealthUI() => MainBossHealthUI.Instance?.Show(name, currentHp, data.maxHp);
    protected override void UpdateHealthUI() => MainBossHealthUI.Instance?.UpdateHP(currentHp);
    protected override void HideHealthUI() => MainBossHealthUI.Instance?.Hide();
    protected override void RecordKill()
    {
        if (SaveData.Instance != null) SaveData.Instance.run.mainBossKillCount++;
    }
}
