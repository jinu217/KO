using System.Collections;
using UnityEngine;

// [스테이지1 메인보스 구현]
// - MainBossController 공통 로직을 바탕으로 실제 패턴 연출/타격 판정 구현
// - 기본공격(2/3연참), 1스킬(3/4/5연참), 2스킬(검기), 4스킬(돌진), 욕망 강화
public class Stage1_MainBoss : MainBossController
{
    [Header("디버그")]
    [Tooltip("스킬 진행 단계 로그를 콘솔에 출력")]
    [SerializeField] private bool showSkillDebugLog = true;

    [Header("검기")]
    [Tooltip("2스킬에서 발사할 검기 프리팹")]
    [SerializeField] private Stage1_BossSwordWave swordWavePrefab;
    [Tooltip("검기 생성 위치(없으면 보스 전방 1.2 기본값 사용)")]
    [SerializeField] private Transform swordWaveSpawnPoint;

    [Header("기본공격 1타 - 세로")]
    [SerializeField] private float basicHit1Range = 1.5f;
    [SerializeField] private float basicHit1Height = 2.4f;
    [SerializeField] private float basicHit1Offset = 1.2f;

    [Header("기본공격 2타 - 가로")]
    [SerializeField] private float basicHit2Range = 2.6f;
    [SerializeField] private float basicHit2Height = 1.5f;
    [SerializeField] private float basicHit2Offset = 1.4f;

    [Header("욕망 기본공격 3타 - 세로")]
    [SerializeField] private float basicHit3Range = 1.8f;
    [SerializeField] private float basicHit3Height = 2.5f;
    [SerializeField] private float basicHit3Offset = 1.4f;

    [Header("1스킬 1타 - 세로")]
    [SerializeField] private float skill1Hit1Range = 1.6f;
    [SerializeField] private float skill1Hit1Height = 2.5f;
    [SerializeField] private float skill1Hit1Offset = 1.2f;

    [Header("1스킬 2타 - 세로")]
    [SerializeField] private float skill1Hit2Range = 1.8f;
    [SerializeField] private float skill1Hit2Height = 2.5f;
    [SerializeField] private float skill1Hit2Offset = 1.5f;

    [Header("1스킬 3타 - 가로")]
    [SerializeField] private float skill1Hit3Range = 3.0f;
    [SerializeField] private float skill1Hit3Height = 1.7f;
    [SerializeField] private float skill1Hit3Offset = 1.7f;

    [Header("1스킬 4타 - 세로")]
    [SerializeField] private float skill1Hit4Range = 2.0f;
    [SerializeField] private float skill1Hit4Height = 2.6f;
    [SerializeField] private float skill1Hit4Offset = 1.6f;

    [Header("1스킬 5타 - 가로")]
    [SerializeField] private float skill1Hit5Range = 3.5f;
    [SerializeField] private float skill1Hit5Height = 1.8f;
    [SerializeField] private float skill1Hit5Offset = 1.9f;

    [Header("욕망 1스킬 추적")]
    [Tooltip("욕망 1스킬 시전 중 플레이어 추적 이동 속도")]
    [SerializeField] private float desireSkill1TrackSpeed = 2.5f;
    [Tooltip("욕망 1스킬 시전 중 방향 갱신 주기")]
    [SerializeField] private float desireSkill1TurnDelay = 0.15f;

    [Header("범위 시각화")]
    [Tooltip("기본공격 1타 박스 표시")]
    [SerializeField] private bool showBasicHit1Gizmo = true;
    [Tooltip("기본공격 2타 박스 표시")]
    [SerializeField] private bool showBasicHit2Gizmo = true;
    [Tooltip("욕망 기본공격 3타 박스 표시")]
    [SerializeField] private bool showBasicHit3Gizmo = true;
    [Tooltip("1스킬 1타 박스 표시")]
    [SerializeField] private bool showSkill1Hit1Gizmo = true;
    [Tooltip("1스킬 2타 박스 표시")]
    [SerializeField] private bool showSkill1Hit2Gizmo = true;
    [Tooltip("1스킬 3타 박스 표시")]
    [SerializeField] private bool showSkill1Hit3Gizmo = true;
    [Tooltip("1스킬 4타 박스 표시(강화)")]
    [SerializeField] private bool showSkill1Hit4Gizmo = true;
    [Tooltip("1스킬 5타 박스 표시(강화 마지막)")]
    [SerializeField] private bool showSkill1Hit5Gizmo = true;

    private float nextDesireTurnTime;

    // 기본공격: 일반 2연참 / 욕망 상태 3연참
    protected override IEnumerator BasicAttackAction()
    {
        if (desireBuffActive)
        {
            LogSkill("기본공격 강화 3연참 시작");

            LogSkill("기본공격 1타 - 세로");
            HitPlayer(data.desireBasicVerticalDamage, basicHit1Range, basicHit1Height, basicHit1Offset);
            yield return new WaitForSeconds(GetAttackDelay(data.basicComboInterval));

            LogSkill("기본공격 2타 - 가로");
            HitPlayer(data.desireBasicHorizontalDamage, basicHit2Range, basicHit2Height, basicHit2Offset);
            yield return new WaitForSeconds(GetAttackDelay(data.basicComboInterval));

            LogSkill("기본공격 3타 - 세로");
            HitPlayer(data.desireBasicVerticalDamage, basicHit3Range, basicHit3Height, basicHit3Offset);
        }
        else
        {
            LogSkill("기본공격 2연참 시작");

            LogSkill("기본공격 1타 - 세로");
            HitPlayer(data.basicVerticalDamage, basicHit1Range, basicHit1Height, basicHit1Offset);
            yield return new WaitForSeconds(GetAttackDelay(data.basicComboInterval));

            LogSkill("기본공격 2타 - 가로");
            HitPlayer(data.basicHorizontalDamage, basicHit2Range, basicHit2Height, basicHit2Offset);
        }
    }

    // 1스킬: 일반 3연참 / 강화 4연참 / 욕망 5연참(추적)
    protected override IEnumerator Skill1Action()
    {
        if (desireEnhanceActive)
        {
            LogSkill("1스킬 욕망 강화 5연참 시작");
            yield return DesireSkill1Combo();
            yield break;
        }

        if (nextSkill1Empowered)
        {
            LogSkill("1스킬 강화 4연참 시작");
            yield return Skill1FourHitCombo();
            nextSkill1Empowered = false;
            yield break;
        }

        LogSkill("1스킬 3연참 시작");

        LogSkill("1스킬 1타 - 세로");
        HitSkill1(data.skill1VerticalDamage, skill1Hit1Range, skill1Hit1Height, skill1Hit1Offset, false, 0f);
        yield return new WaitForSeconds(GetAttackDelay(data.skill1ComboInterval));

        LogSkill("1스킬 2타 - 세로");
        HitSkill1(data.skill1VerticalDamage, skill1Hit2Range, skill1Hit2Height, skill1Hit2Offset, false, 0f);
        yield return new WaitForSeconds(GetAttackDelay(data.skill1ComboInterval));

        LogSkill("1스킬 3타 - 가로 넉백");
        bool thirdHit = HitSkill1(
            data.skill1HorizontalDamage,
            skill1Hit3Range,
            skill1Hit3Height,
            skill1Hit3Offset,
            true,
            data.skill1KnockbackPower
        );

        if (thirdHit)
        {
            skill1HitSuccessCount++;

            LogSkill($"1스킬 3타 적중 누적: {skill1HitSuccessCount}/3");

            if (skill1HitSuccessCount >= 3)
            {
                skill1HitSuccessCount = 0;
                nextSkill1Empowered = true;
                LogSkill("다음 1스킬은 강화 4연참");
            }
        }
    }

    // 1스킬 강화 4연참(누적 적중 후 자동 활성화)
    private IEnumerator Skill1FourHitCombo()
    {
        LogSkill("1스킬 강화 1타 - 세로");
        HitSkill1(data.skill1VerticalDamage, skill1Hit1Range, skill1Hit1Height, skill1Hit1Offset, false, 0f);
        yield return new WaitForSeconds(GetAttackDelay(data.skill1ComboInterval));

        LogSkill("1스킬 강화 2타 - 가로");
        HitSkill1(data.skill1HorizontalDamage, skill1Hit3Range, skill1Hit3Height, skill1Hit3Offset, false, 0f);
        yield return new WaitForSeconds(GetAttackDelay(data.skill1ComboInterval));

        LogSkill("1스킬 강화 3타 - 세로");
        HitSkill1(data.skill1VerticalDamage, skill1Hit4Range, skill1Hit4Height, skill1Hit4Offset, false, 0f);
        yield return new WaitForSeconds(GetAttackDelay(data.skill1ComboInterval));

        LogSkill("1스킬 강화 4타 - 가로 강넉백");
        HitSkill1(data.skill1FinalHorizontalDamage, skill1Hit5Range, skill1Hit5Height, skill1Hit5Offset, true, data.skill1StrongKnockbackPower);
    }

    // 욕망 상태 1스킬 5연참(타격 사이 플레이어 추적 이동 포함)
    private IEnumerator DesireSkill1Combo()
    {
        string[] combo = { "V", "V", "H", "V", "H" };

        for (int i = 0; i < combo.Length; i++)
        {
            TrackPlayerDuringDesireSkill1();

            if (combo[i] == "V")
            {
                float range = i == 0 ? skill1Hit1Range : skill1Hit4Range;
                float height = i == 0 ? skill1Hit1Height : skill1Hit4Height;
                float offset = i == 0 ? skill1Hit1Offset : skill1Hit4Offset;

                LogSkill($"욕망 1스킬 {i + 1}타 - 세로");

                HitPlayer(
                    data.desireSkill1VerticalDamage,
                    range + 0.4f,
                    height + 0.3f,
                    offset + 0.2f
                );
            }
            else
            {
                bool isFinalHit = i == combo.Length - 1;

                float range = isFinalHit ? skill1Hit5Range : skill1Hit3Range;
                float height = isFinalHit ? skill1Hit5Height : skill1Hit3Height;
                float offset = isFinalHit ? skill1Hit5Offset : skill1Hit3Offset;

                LogSkill($"욕망 1스킬 {i + 1}타 - 가로");

                HitPlayer(
                    data.desireSkill1HorizontalDamage,
                    range + 0.6f,
                    height + 0.2f,
                    offset + 0.3f,
                    isFinalHit,
                    data.skill1StrongKnockbackPower
                );
            }

            if (i < combo.Length - 1)
                yield return new WaitForSeconds(GetAttackDelay(data.skill1ComboInterval * 0.75f));
        }
    }

    // 욕망 1스킬 시전 중 플레이어 방향 추적 + 전방 이동
    private void TrackPlayerDuringDesireSkill1()
    {
        if (playerTransform == null) return;

        if (Time.time >= nextDesireTurnTime)
        {
            float dir = playerTransform.position.x - transform.position.x;

            if (Mathf.Abs(dir) > turnDeadZone)
            {
                lookDirection = dir > 0 ? 1 : -1;
                delayedDirection = lookDirection;

                if (spriteRenderer != null)
                    spriteRenderer.flipX = lookDirection < 0;
            }

            nextDesireTurnTime = Time.time + desireSkill1TurnDelay;
        }

        if (!IsWallInFront())
        {
            rb.linearVelocity = new Vector2(
                lookDirection * desireSkill1TrackSpeed,
                rb.linearVelocity.y
            );
        }
    }

    // 1스킬 전용 타격 판정(적중 시 true 반환, 누적 카운트 처리에 사용)
    private bool HitSkill1(float damage, float range, float height, float offset, bool knockback, float knockbackPower)
    {
        Collider2D hit = GetPlayerHit(range, height, offset);

        if (hit == null) return false;

        if (hit.TryGetComponent<PlayerController>(out var player))
        {
            player.TakeDamage(damage, (Vector2)transform.position, false, false);

            if (knockback)
                ApplyKnockbackToPlayer(player, knockbackPower);

            return true;
        }

        return false;
    }

    // 2스킬: 검기 발사(욕망 강화 시 발사 수/크기 증가)
    protected override IEnumerator Skill2Action()
    {
        int count = desireEnhanceActive ? data.desireSkill2ProjectileCount : 1;
        float damage = desireEnhanceActive ? data.desireSkill2Damage : data.skill2Damage;

        LogSkill(desireEnhanceActive ? "2스킬 강화 검기 2발 발사" : "2스킬 검기 발사");

        for (int i = 0; i < count; i++)
        {
            SpawnSwordWave(damage, i, count);

            if (i < count - 1)
                yield return new WaitForSeconds(0.15f);
        }
    }

    // 검기 프리팹 생성/초기화(욕망 강화 시 크기 1.35배, 2발은 상하 분리)
    private void SpawnSwordWave(float damage, int index, int totalCount)
    {
        if (swordWavePrefab == null)
        {
            Debug.LogWarning("SwordWavePrefab이 연결되지 않았습니다.");
            return;
        }

        Vector3 spawnPos = swordWaveSpawnPoint != null
            ? swordWaveSpawnPoint.position
            : transform.position + new Vector3(lookDirection * 1.2f, 0.2f, 0f);

        Stage1_BossSwordWave wave = Instantiate(swordWavePrefab, spawnPos, Quaternion.identity);

        float sizeMultiplier = desireEnhanceActive ? 1.35f : 1f;

        float yOffset = 0f;
        if (totalCount == 2)
            yOffset = index == 0 ? 0.25f : -0.25f;

        wave.transform.position += new Vector3(0f, yOffset, 0f);
        wave.Init(lookDirection, damage, playerLayer, sizeMultiplier);
    }

    // 4스킬: 일정 시간 동안 직선 돌진(벽 충돌 시 종료, 1회 적중)
    protected override IEnumerator Skill4Action()
    {
        LogSkill("4스킬 돌진기 사용");

        float timer = 0f;
        bool hasHit = false;

        while (timer < data.skill4DashDuration)
        {
            if (IsWallInFront())
                break;

            rb.linearVelocity = new Vector2(
                lookDirection * data.skill4DashSpeed,
                rb.linearVelocity.y
            );

            if (!hasHit)
            {
                Collider2D hit = GetPlayerHit(
                    data.skill4Range,
                    data.skill4Height,
                    data.skill4Offset
                );

                if (hit != null && hit.TryGetComponent<PlayerController>(out var player))
                {
                    player.TakeDamage(data.skill4Damage, (Vector2)transform.position);
                    hasHit = true;
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }

        StopMove();
    }

    // 3스킬 발동 시 로그 출력 후 부모 버프 적용
    protected override void StartAttackSpeedBuff()
    {
        LogSkill("3스킬 공격속도 버프 발동");
        base.StartAttackSpeedBuff();
    }

    // 5스킬 발동 시 로그 출력 후 부모 버프 적용
    protected override void StartMoveSpeedBuff()
    {
        LogSkill("5스킬 이동속도 버프 발동");
        base.StartMoveSpeedBuff();
    }

    // 욕망 발동 시 상세 로그 출력 후 부모 활성화
    protected override void ActivateDesire()
    {
        LogSkill("욕망 스킬 발동");
        LogSkill("모든 스킬 쿨타임 초기화");
        LogSkill("15초간 공격속도/이동속도 증가, 기본공격 강화");
        LogSkill("10초간 1스킬/2스킬 강화");

        base.ActivateDesire();
    }

    // 욕망 자체에는 별도 연출이 없고 즉시 종료(버프만 적용)
    protected override IEnumerator DesireAction()
    {
        yield break;
    }

    // 디버그 로그 출력 헬퍼(showSkillDebugLog가 켜져 있을 때만 출력)
    private void LogSkill(string message)
    {
        if (!showSkillDebugLog) return;
        Debug.Log($"[Stage1 MainBoss] {message}");
    }

    // 부모 시각화 + 자식의 기본공격/1스킬 세부 타격 박스 추가 표시
    // 4스킬 박스는 부모(MainBossController)의 showSkill4Gizmo가 처리하므로 자식에서 그리지 않음
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        int dir = lookDirection == 0 ? 1 : lookDirection;

        if (showBasicHit1Gizmo)
            DrawAttackBox(Color.red, basicHit1Range, basicHit1Height, basicHit1Offset, 0f, dir);

        if (showBasicHit2Gizmo)
            DrawAttackBox(Color.blue, basicHit2Range, basicHit2Height, basicHit2Offset, 0f, dir);

        if (showBasicHit3Gizmo)
            DrawAttackBox(Color.cyan, basicHit3Range, basicHit3Height, basicHit3Offset, 0f, dir);

        if (showSkill1Hit1Gizmo)
            DrawAttackBox(new Color(1f, 0.35f, 0f), skill1Hit1Range, skill1Hit1Height, skill1Hit1Offset, 0f, dir);

        if (showSkill1Hit2Gizmo)
            DrawAttackBox(Color.yellow, skill1Hit2Range, skill1Hit2Height, skill1Hit2Offset, 0f, dir);

        if (showSkill1Hit3Gizmo)
            DrawAttackBox(Color.magenta, skill1Hit3Range, skill1Hit3Height, skill1Hit3Offset, 0f, dir);

        if (showSkill1Hit4Gizmo)
            DrawAttackBox(new Color(0.4f, 1f, 0.4f), skill1Hit4Range, skill1Hit4Height, skill1Hit4Offset, 0f, dir);

        if (showSkill1Hit5Gizmo)
            DrawAttackBox(new Color(0.7f, 0f, 1f), skill1Hit5Range, skill1Hit5Height, skill1Hit5Offset, 0f, dir);
    }
}
