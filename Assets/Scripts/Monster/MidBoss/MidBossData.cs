using UnityEngine;

[CreateAssetMenu(fileName = "NewMidBossData", menuName = "ScriptableObjects/MidBossData")]
public class MidBossData : ScriptableObject
{
    [Header("기본 스탯")]
    public float maxHp = 300f;
    public float chaseSpeed = 2.5f;

    [Header("인식/추적 범위")]
    public float detectRange = 8f;
    public float chaseRange = 12f;

    [Header("기본 공격")]
    public float basicAttackDamage = 15f;
    public float basicAttackCooldown = 1.5f;
    public float basicAttackRange = 2f;
    public float basicAttackHeight = 2f;
    public float basicAttackOffset = 1.2f;
    public float basicAttackHeightOffset = 0f;
    public float basicPreDelay = 0.35f;
    public float basicPostDelay = 0.45f;

    [Header("패턴 시작 대기")]
    public float patternStartDelay = 5f;

    [Header("패턴 1")]
    public float pattern1Cooldown = 5f;
    public float pattern1PreDelay = 0.5f;
    public float pattern1PostDelay = 0.7f;
    public float pattern1Damage = 20f;
    public float pattern1Range = 3f;
    public float pattern1Height = 2.5f;
    public float pattern1Offset = 1.6f;
    public float pattern1HeightOffset = 0f;

    [Header("패턴 2")]
    public float pattern2Cooldown = 8f;
    public float pattern2PreDelay = 0.7f;
    public float pattern2PostDelay = 0.9f;
    public float pattern2Damage = 25f;
    public float pattern2Range = 4f;
    public float pattern2Height = 3f;
    public float pattern2Offset = 2f;
    public float pattern2HeightOffset = 0f;

    [Header("패턴 3")]
    public float pattern3Cooldown = 12f;
    public float pattern3PreDelay = 1f;
    public float pattern3PostDelay = 1.2f;
    public float pattern3Damage = 35f;
    public float pattern3Range = 5f;
    public float pattern3Height = 3.5f;
    public float pattern3Offset = 2.5f;
    public float pattern3HeightOffset = 0f;
}