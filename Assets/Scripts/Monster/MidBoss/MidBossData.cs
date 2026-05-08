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

    [Header("패턴 시작 대기")]
    public float patternStartDelay = 5f;

    [Header("기본 공격")]
    public float basicAttackCooldown = 1.5f;
    public float basicPreDelay = 0.25f;
    public float basicPostDelay = 0.45f;
    public int basicAttackCount = 1;
    public float basicAttackInterval = 0.15f;
    public float basicAttackDamage = 10f;
    public float basicAttackRange = 2f;
    public float basicAttackHeight = 1.5f;
    public float basicAttackOffset = 1.2f;
    public float basicAttackHeightOffset = 0f;

    [Header("패턴 1")]
    public float pattern1Cooldown = 6f;
    public float pattern1PreDelay = 0.4f;
    public float pattern1PostDelay = 0.6f;
    public int pattern1AttackCount = 1;
    public float pattern1AttackInterval = 0.2f;
    public float pattern1Damage = 20f;
    public float pattern1Range = 4f;
    public float pattern1Height = 1.8f;
    public float pattern1Offset = 2f;
    public float pattern1HeightOffset = 0f;
    public float pattern1MoveSpeed = 10f;
    public float pattern1Duration = 0.35f;

    [Header("패턴 2")]
    public float pattern2Cooldown = 8f;
    public float pattern2PreDelay = 0.5f;
    public float pattern2PostDelay = 0.7f;
    public int pattern2AttackCount = 3;
    public float pattern2AttackInterval = 0.25f;
    public float pattern2Damage = 14f;
    public float pattern2Range = 2.2f;
    public float pattern2Height = 2.2f;
    public float pattern2Offset = 2.3f;
    public float pattern2HeightOffset = 0f;
    public float pattern2MoveSpeed = 0f;
    public float pattern2Duration = 0f;

    [Header("패턴 3")]
    public float pattern3Cooldown = 12f;
    public float pattern3PreDelay = 0.6f;
    public float pattern3PostDelay = 0.5f;
    public int pattern3AttackCount = 0;
    public float pattern3AttackInterval = 0f;
    public float pattern3Damage = 0f;
    public float pattern3Range = 0f;
    public float pattern3Height = 0f;
    public float pattern3Offset = 0f;
    public float pattern3HeightOffset = 0f;
    public float pattern3MoveSpeed = 0f;
    public float pattern3Duration = 5f;

    [Header("패턴 3 버프용 공통 값")]
    public float buffMoveSpeedMultiplier = 1.25f;
    public float buffDamageMultiplier = 1.35f;
    public float buffCooldownMultiplier = 0.75f;

    [Header("벽 감지")]
    public float wallCheckDistance = 0.5f;
}