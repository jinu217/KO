using System.Collections;
using UnityEngine;

// [스테이지2 메인보스 구현]
// - 기본공격: 강한 올려치기
// - 1스킬: 제자리 내려찍기 -> 대각선 점프 내려찍기
// - 2스킬: 몸통 돌진 -> 올려치기 -> 내려찍기
// - 3스킬: 대각선 점프 내려찍기 -> 전방 돌 5개 순차 생성
public class Stage2_MainBoss : MainBossController
{
    [Header("디버그")]
    [SerializeField] private bool showSkillDebugLog = true;

    [Header("일반공격 - 올려치기")]
    [SerializeField] private float basicRange = 2.5f;
    [SerializeField] private float basicHeight = 2.6f;
    [SerializeField] private float basicOffset = 1.4f;
    [Tooltip("약한 X축 넉백")]
    [SerializeField] private float basicKnockbackX = 3f;
    [Tooltip("강한 Y축 넉백")]
    [SerializeField] private float basicKnockbackY = 12f;

    [Header("1스킬 - 1타 내려찍기")]
    [SerializeField] private float skill1FirstDamageMultiplier = 1f;
    [SerializeField] private float skill1FirstRange = 2.2f;
    [SerializeField] private float skill1FirstHeight = 2.8f;
    [SerializeField] private float skill1FirstOffset = 1.35f;

    [Header("1스킬 - 2타 점프 내려찍기")]
    [SerializeField] private float skill1SecondDamageMultiplier = 1.35f;
    [SerializeField] private float skill1SecondRange = 3f;
    [SerializeField] private float skill1SecondHeight = 2.2f;
    [SerializeField] private float skill1SecondOffset = 1.2f;
    [SerializeField] private float skill1ComboInterval = 0.3f;
    [SerializeField] private float skill1JumpXSpeed = 5f;
    [SerializeField] private float skill1JumpYSpeed = 10f;

    [Header("2스킬 - 1타 몸통 돌진")]
    [SerializeField] private float skill2DashSpeed = 12f;
    [SerializeField] private float skill2DashDuration = 0.45f;
    [SerializeField] private float skill2DashRange = 2.2f;
    [SerializeField] private float skill2DashHeight = 1.8f;
    [SerializeField] private float skill2DashOffset = 1.25f;
    [SerializeField] private float skill2DashDamageMultiplier = 0.8f;

    [Header("2스킬 - 2타 올려치기")]
    [SerializeField] private float skill2UppercutRange = 2.4f;
    [SerializeField] private float skill2UppercutHeight = 2.7f;
    [SerializeField] private float skill2UppercutOffset = 1.35f;
    [SerializeField] private float skill2UppercutDamageMultiplier = 1f;

    [Header("2스킬 - 3타 내려찍기")]
    [SerializeField] private float skill2SlamRange = 2.7f;
    [SerializeField] private float skill2SlamHeight = 2.8f;
    [SerializeField] private float skill2SlamOffset = 1.45f;
    [SerializeField] private float skill2SlamDamageMultiplier = 1.25f;
    [SerializeField] private float skill2ComboInterval = 0.25f;

    [Header("3스킬 - 점프 내려찍기")]
    [SerializeField] private float skill3SlamDamage = 30f;
    [SerializeField] private float skill3SlamRange = 3.2f;
    [SerializeField] private float skill3SlamHeight = 2.3f;
    [SerializeField] private float skill3SlamOffset = 1.1f;
    [SerializeField] private float skill3JumpXSpeed = 6f;
    [SerializeField] private float skill3JumpYSpeed = 11f;

    [Header("점프 공통")]
    [SerializeField] private float slamDownSpeed = 16f;
    [SerializeField] private float landingCheckDistance = 0.18f;
    [SerializeField] private float jumpTimeout = 2.5f;

    [Header("3스킬 - 돌 파동")]
    [SerializeField] private Stage2_BossRock rockPrefab;
    [Tooltip("첫 돌 생성 기준점. 없으면 보스 위치 사용")]
    [SerializeField] private Transform rockSpawnPoint;
    [SerializeField] private int rockCount = 5;
    [SerializeField] private float rockSpacing = 1.25f;
    [SerializeField] private float rockSpawnInterval = 0.12f;
    [SerializeField] private float rockDamage = 15f;
    [SerializeField] private Vector2 rockHitboxSize = new Vector2(1f, 2f);
    [SerializeField] private float rockRiseHeight = 2f;
    [SerializeField] private float rockRiseDuration = 0.18f;
    [SerializeField] private float rockHoldDuration = 0.35f;
    [SerializeField] private float rockFallDuration = 0.2f;

    [Header("범위 시각화")]
    [SerializeField] private bool showStage2AttackGizmos = true;

    protected override IEnumerator BasicAttackAction()
    {
        LogSkill("일반공격: 아래에서 위로 올려치기");
        HitPlayerWithDirectionalKnockback(
            data.basicHorizontalDamage,
            basicRange,
            basicHeight,
            basicOffset,
            basicKnockbackX,
            basicKnockbackY
        );
        yield break;
    }

    protected override IEnumerator Skill1Action()
    {
        LogSkill("1스킬 1타: 크게 내려찍기");
        HitPlayer(
            data.skill1VerticalDamage * skill1FirstDamageMultiplier,
            skill1FirstRange,
            skill1FirstHeight,
            skill1FirstOffset
        );

        yield return new WaitForSeconds(GetAttackDelay(skill1ComboInterval));

        LogSkill("1스킬 2타: 대각선 점프 내려찍기");
        yield return JumpAndLand(skill1JumpXSpeed, skill1JumpYSpeed);
        HitPlayer(
            data.skill1VerticalDamage * skill1SecondDamageMultiplier,
            skill1SecondRange,
            skill1SecondHeight,
            skill1SecondOffset
        );
    }

    protected override IEnumerator Skill2Action()
    {
        LogSkill("2스킬 1타: 몸통 돌진");
        yield return DashBodyAttack();

        yield return new WaitForSeconds(GetAttackDelay(skill2ComboInterval));

        LogSkill("2스킬 2타: 올려치기");
        HitPlayer(
            data.skill2Damage * skill2UppercutDamageMultiplier,
            skill2UppercutRange,
            skill2UppercutHeight,
            skill2UppercutOffset
        );

        yield return new WaitForSeconds(GetAttackDelay(skill2ComboInterval));

        LogSkill("2스킬 3타: 내려찍기");
        HitPlayer(
            data.skill2Damage * skill2SlamDamageMultiplier,
            skill2SlamRange,
            skill2SlamHeight,
            skill2SlamOffset
        );
    }

    protected override IEnumerator Skill3Action()
    {
        LogSkill("3스킬: 대각선 점프 내려찍기");
        yield return JumpAndLand(skill3JumpXSpeed, skill3JumpYSpeed);

        HitPlayer(skill3SlamDamage, skill3SlamRange, skill3SlamHeight, skill3SlamOffset);
        yield return SpawnRockWave();
    }

    // Stage2 메인보스는 설명된 일반공격과 1~3스킬만 사용한다.
    protected override bool CanUseSkill3()
    {
        return Time.time >= lastSkill3Time + data.skill3Cooldown;
    }

    protected override bool CanUseSkill4() => false;
    protected override bool CanUseSkill5() => false;
    protected override bool CanUseDesire() => false;

    protected override IEnumerator Skill4Action()
    {
        yield break;
    }

    protected override IEnumerator DesireAction()
    {
        yield break;
    }

    private IEnumerator DashBodyAttack()
    {
        float timer = 0f;
        bool hasHit = false;

        while (timer < skill2DashDuration)
        {
            if (IsWallInFront())
                break;

            rb.linearVelocity = new Vector2(lookDirection * skill2DashSpeed, rb.linearVelocity.y);

            if (!hasHit)
            {
                Collider2D hit = GetPlayerHit(skill2DashRange, skill2DashHeight, skill2DashOffset);
                if (hit != null)
                {
                    IDamageable target = hit.GetComponentInParent<IDamageable>();
                    if (target != null)
                    {
                        target.TakeDamage(
                            data.skill2Damage * skill2DashDamageMultiplier,
                            (Vector2)transform.position,
                            true,
                            false
                        );
                        hasHit = true;
                    }
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }

        StopMove();
    }

    private IEnumerator JumpAndLand(float xSpeed, float ySpeed)
    {
        rb.linearVelocity = new Vector2(lookDirection * xSpeed, ySpeed);

        float timer = 0f;
        while (timer < jumpTimeout && rb.linearVelocity.y > 0f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = new Vector2(lookDirection * xSpeed, -slamDownSpeed);

        while (timer < jumpTimeout)
        {
            if (IsGrounded())
                break;

            timer += Time.deltaTime;
            yield return null;
        }

        StopMove();
    }

    private bool IsGrounded()
    {
        Vector2 origin = bossCollider != null
            ? new Vector2(bossCollider.bounds.center.x, bossCollider.bounds.min.y + 0.02f)
            : (Vector2)transform.position;

        return Physics2D.Raycast(origin, Vector2.down, landingCheckDistance, wallLayer).collider != null;
    }

    private IEnumerator SpawnRockWave()
    {
        if (rockPrefab == null)
        {
            Debug.LogWarning("[Stage2 MainBoss] Rock Prefab이 연결되지 않았습니다.");
            yield break;
        }

        Vector3 origin = rockSpawnPoint != null ? rockSpawnPoint.position : transform.position;
        int count = Mathf.Max(1, rockCount);

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPosition = origin + Vector3.right * lookDirection * rockSpacing * (i + 1);
            Stage2_BossRock rock = Instantiate(rockPrefab, spawnPosition, Quaternion.identity);
            rock.Init(
                rockDamage,
                playerLayer,
                rockHitboxSize,
                rockRiseHeight,
                rockRiseDuration,
                rockHoldDuration,
                rockFallDuration,
                transform.position
            );

            if (i < count - 1)
                yield return new WaitForSeconds(rockSpawnInterval);
        }
    }

    private bool HitPlayerWithDirectionalKnockback(
        float damage,
        float range,
        float height,
        float offset,
        float knockbackX,
        float knockbackY)
    {
        Collider2D hit = GetPlayerHit(range, height, offset);
        if (hit == null)
            return false;

        PlayerController player = hit.GetComponentInParent<PlayerController>();
        if (player == null)
            return false;

        player.TakeDamage(damage, (Vector2)transform.position, true, false);

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.AddForce(
                new Vector2(lookDirection * knockbackX, knockbackY),
                ForceMode2D.Impulse
            );
        }

        return true;
    }

    private void LogSkill(string message)
    {
        if (showSkillDebugLog)
            Debug.Log($"[Stage2 MainBoss] {message}");
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        if (!showStage2AttackGizmos)
            return;

        int dir = lookDirection == 0 ? 1 : lookDirection;
        DrawAttackBox(Color.cyan, basicRange, basicHeight, basicOffset, 0f, dir);
        DrawAttackBox(Color.yellow, skill1FirstRange, skill1FirstHeight, skill1FirstOffset, 0f, dir);
        DrawAttackBox(new Color(1f, 0.5f, 0f), skill1SecondRange, skill1SecondHeight, skill1SecondOffset, 0f, dir);
        DrawAttackBox(Color.red, skill2DashRange, skill2DashHeight, skill2DashOffset, 0f, dir);
        DrawAttackBox(Color.green, skill2UppercutRange, skill2UppercutHeight, skill2UppercutOffset, 0f, dir);
        DrawAttackBox(Color.magenta, skill2SlamRange, skill2SlamHeight, skill2SlamOffset, 0f, dir);
        DrawAttackBox(Color.blue, skill3SlamRange, skill3SlamHeight, skill3SlamOffset, 0f, dir);
    }
}
