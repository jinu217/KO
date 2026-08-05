using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PersistentData
{
    public float playTime;

    public int deathCount;

    // 이어하기 시 시작할 스테이지
    public string nextStage;

    // 영구 재화
    public int soul;

    public float bonusAttackDamage;
    public float bonusMaxHp;
    public float bonusAttackSpeed;
    public int bonusDashCount;
    public float dashCooldownReduction;
    public int startingCoinBonus;

    public int attackDamageUpgradeCount;
    public int maxHpUpgradeCount;
    public int attackSpeedUpgradeCount;
    public int dashCountUpgradeCount;
    public int dashCooldownUpgradeCount;
    public int startingCoinUpgradeCount;
    public int equipmentRerollBonus;
    public int equipmentRerollUpgradeCount;

}
