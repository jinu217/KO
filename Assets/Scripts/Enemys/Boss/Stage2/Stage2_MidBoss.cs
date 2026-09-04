using System.Collections;
using UnityEngine;

// [스테이지2 중간보스 구현]
// - 기본공격: 찌르기(약 넉백) -> 한 발 전진 -> 내려찍기(강 넉백)
// - 패턴1: 가짜 영광 버프 / 패턴2: 전방 5연격 / 패턴3: 내려찍기 -> 창 돌진
public class Stage2_MidBoss : MidBossController
{
    [Header("Stage2 버프")]
    [SerializeField] private Color buffColor = new Color(1f, 0.45f, 0.25f);
    [Tooltip("1스킬 버프 지속 시간")]
    [SerializeField] private float buffDuration = 5f;

    [Header("기본공격 - 1타 찌르기")]
    [SerializeField] private float basicFirstHitDamageMultiplier = 0.8f;
    [SerializeField] private float basicFirstHitRange = 2.2f;
    [SerializeField] private float basicFirstHitHeight = 1.2f;
    [SerializeField] private float basicFirstHitOffset = 1.3f;
    [SerializeField] private float basicFirstHitHeightOffset = 0f;

    [Header("기본공격 - 전진")]
    [SerializeField] private float basicAdvanceSpeed = 3.5f;
    [SerializeField] private float basicAdvanceDuration = 0.18f;

    [Header("기본공격 - 2타 내려찍기")]
    [SerializeField] private float basicSecondHitDamageMultiplier = 1.3f;
    [SerializeField] private float basicSecondHitRange = 1.8f;
    [SerializeField] private float basicSecondHitHeight = 2.5f;
    [SerializeField] private float basicSecondHitOffset = 1.25f;
    [SerializeField] private float basicSecondHitHeightOffset = 0f;
    [SerializeField] private float basicSecondHitKnockbackPower = 12f;
    [SerializeField] private float basicSecondHitUpPower = 4f;

    [Header("2스킬 - 전방 5연격")]
    [SerializeField] private int skill2HitCount = 5;
    [SerializeField] private float skill2HitDamageMultiplier = 0.65f;

    [Header("3스킬 - 내려찍기")]
    [SerializeField] private float skill3SlamDamageMultiplier = 1f;
    [SerializeField] private float skill3SlamRange = 2.1f;
    [SerializeField] private float skill3SlamHeight = 2.8f;
    [SerializeField] private float skill3SlamOffset = 1.35f;
    [SerializeField] private float skill3SlamHeightOffset = 0f;
    [SerializeField] private float skill3SlamToChargeDelay = 0.25f;

    [Header("3스킬 - 창 돌진")]
    [SerializeField] private float skill3ChargeDamageMultiplier = 1.5f;
    [SerializeField] private float skill3ChargeKnockbackPower = 14f;
    [SerializeField] private float skill3ChargeUpPower = 4.5f;

    [Header("Stage2 세부 범위 시각화")]
    [SerializeField] private bool showBasicFirstHitGizmo = true;
    [SerializeField] private bool showBasicSecondHitGizmo = true;
    [SerializeField] private bool showSkill3SlamGizmo = true;

    private Color originalColor;
    private Coroutine buffRoutine;

    protected override void Awake()
    {
        base.Awake();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    // 버프가 준비된 경우에만 패턴1을 우선 선택한다. 버프 중에는 2/3스킬 선택을 막지 않는다.
    protected override int GetNextAttackIndex()
    {
        if (!isBuffed && Time.time >= lastPattern1Time + GetCooldown(data.pattern1Cooldown))
            return 1;

        if (Time.time >= lastPattern2Time + GetCooldown(data.pattern2Cooldown))
            return 2;

        if (Time.time >= lastPattern3Time + GetCooldown(data.pattern3Cooldown))
            return 3;

        return 0;
    }

    protected override bool IsInSelectedAttackRange()
    {
        if (GetNextAttackIndex() == 1)
            return true;

        return base.IsInSelectedAttackRange();
    }

    protected override bool CanUsePattern1()
    {
        return !isBuffed && Time.time >= lastPattern1Time + GetCooldown(data.pattern1Cooldown);
    }

    protected override bool CanUsePattern3()
    {
        if (Time.time < lastPattern3Time + GetCooldown(data.pattern3Cooldown))
            return false;

        return IsPlayerInAttackRange(
            data.pattern3Range,
            data.pattern3Height,
            data.pattern3Offset,
            data.pattern3HeightOffset
        );
    }

    protected override IEnumerator BasicAttackRoutine()
    {
        isAttacking = true;
        StopMove();
        lastBasicAttackTime = Time.time;
        OnBasicAttackStart();

        yield return new WaitForSeconds(data.basicPreDelay);

        Debug.Log("Stage2 MidBoss 기본공격 1타: 찌르기");
        HitPlayer(
            GetFinalDamage(data.basicAttackDamage * basicFirstHitDamageMultiplier),
            basicFirstHitRange,
            basicFirstHitHeight,
            basicFirstHitOffset,
            basicFirstHitHeightOffset
        );

        yield return new WaitForSeconds(data.basicAttackInterval);
        yield return MoveForward(basicAdvanceSpeed, basicAdvanceDuration);

        Debug.Log("Stage2 MidBoss 기본공격 2타: 내려찍기");
        HitPlayerWithKnockback(
            GetFinalDamage(data.basicAttackDamage * basicSecondHitDamageMultiplier),
            basicSecondHitRange,
            basicSecondHitHeight,
            basicSecondHitOffset,
            basicSecondHitHeightOffset,
            basicSecondHitKnockbackPower,
            basicSecondHitUpPower
        );

        yield return new WaitForSeconds(data.basicPostDelay);

        OnBasicAttackEnd();
        MarkAttackEnded(BossAttackKind.Basic);
        isAttacking = false;
    }

    protected override void OnPattern1Start()
    {
        Debug.Log("Stage2 MidBoss 1스킬: 가짜 영광 버프");
    }

    protected override IEnumerator Pattern1Action()
    {
        if (isBuffed)
            yield break;

        isBuffed = true;

        if (spriteRenderer != null)
            spriteRenderer.color = buffColor;

        if (buffRoutine != null)
            StopCoroutine(buffRoutine);

        buffRoutine = StartCoroutine(BuffDurationRoutine());
        yield break;
    }

    private IEnumerator BuffDurationRoutine()
    {
        yield return new WaitForSeconds(buffDuration);

        isBuffed = false;
        buffRoutine = null;

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    protected override void OnPattern2Start()
    {
        Debug.Log("Stage2 MidBoss 2스킬: 전방 5연격");
    }

    protected override IEnumerator Pattern2Action()
    {
        int hitCount = Mathf.Max(1, skill2HitCount);

        for (int i = 0; i < hitCount; i++)
        {
            Debug.Log($"Stage2 MidBoss 2스킬 {i + 1}타");
            HitPlayer(
                GetFinalDamage(data.pattern2Damage * skill2HitDamageMultiplier),
                data.pattern2Range,
                data.pattern2Height,
                data.pattern2Offset,
                data.pattern2HeightOffset
            );

            if (i < hitCount - 1)
                yield return new WaitForSeconds(data.pattern2AttackInterval);
        }
    }

    protected override void OnPattern3Start()
    {
        Debug.Log("Stage2 MidBoss 3스킬: 기 모으기");
    }

    protected override IEnumerator Pattern3Action()
    {
        Debug.Log("Stage2 MidBoss 3스킬 1타: 내려찍기");
        HitPlayer(
            GetFinalDamage(data.pattern3Damage * skill3SlamDamageMultiplier),
            skill3SlamRange,
            skill3SlamHeight,
            skill3SlamOffset,
            skill3SlamHeightOffset
        );

        yield return new WaitForSeconds(skill3SlamToChargeDelay);

        Debug.Log("Stage2 MidBoss 3스킬 2타: 창 돌진");
        float timer = 0f;
        bool hasHitPlayer = false;

        while (timer < data.pattern3Duration)
        {
            if (IsWallInFront())
                break;

            rb.linearVelocity = new Vector2(
                lookDirection * data.pattern3MoveSpeed,
                rb.linearVelocity.y
            );

            if (!hasHitPlayer)
            {
                hasHitPlayer = HitPlayerWithKnockback(
                    GetFinalDamage(data.pattern3Damage * skill3ChargeDamageMultiplier),
                    data.pattern3Range,
                    data.pattern3Height,
                    data.pattern3Offset,
                    data.pattern3HeightOffset,
                    skill3ChargeKnockbackPower,
                    skill3ChargeUpPower
                );
            }

            timer += Time.deltaTime;
            yield return null;
        }

        StopMove();
    }

    private IEnumerator MoveForward(float speed, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            if (IsWallInFront())
                break;

            rb.linearVelocity = new Vector2(lookDirection * speed, rb.linearVelocity.y);
            timer += Time.deltaTime;
            yield return null;
        }

        StopMove();
    }

    protected override void Die()
    {
        if (buffRoutine != null)
        {
            StopCoroutine(buffRoutine);
            buffRoutine = null;
        }

        isBuffed = false;

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        base.Die();
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        int dir = lookDirection == 0 ? 1 : lookDirection;

        if (showBasicFirstHitGizmo)
            DrawAttackBox(Color.red, basicFirstHitRange, basicFirstHitHeight, basicFirstHitOffset, basicFirstHitHeightOffset, dir);

        if (showBasicSecondHitGizmo)
            DrawAttackBox(Color.blue, basicSecondHitRange, basicSecondHitHeight, basicSecondHitOffset, basicSecondHitHeightOffset, dir);

        if (showSkill3SlamGizmo)
            DrawAttackBox(Color.yellow, skill3SlamRange, skill3SlamHeight, skill3SlamOffset, skill3SlamHeightOffset, dir);
    }
}
