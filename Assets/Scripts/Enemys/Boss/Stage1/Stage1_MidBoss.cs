using System.Collections;
using UnityEngine;

// [스테이지1 중간보스 구현]
// - MidBossController를 상속받아 기본공격/패턴1~3 연출과 데미지 처리를 담당
// - 패턴1: 돌진 박치기 / 패턴2: 3연속 참격 / 패턴3: 가짜 영광 버프
public class Stage1_MidBoss : MidBossController
{
    [Header("Stage1 연출")]
    [Tooltip("패턴3 가짜 영광 버프 활성화 시 스프라이트 색상")]
    [SerializeField] private Color buffColor = new Color(1f, 0.45f, 0.25f);

    [Header("패턴2 - 데미지 배율")]
    [Tooltip("1타 데미지 배율 (data.pattern2Damage 기준)")]
    [SerializeField] private float pattern2FirstHitDamageMultiplier = 0.8f;
    [Tooltip("2타 데미지 배율 (data.pattern2Damage 기준)")]
    [SerializeField] private float pattern2SecondHitDamageMultiplier = 1.0f;
    [Tooltip("3타 데미지 배율 (data.pattern2Damage 기준)")]
    [SerializeField] private float pattern2ThirdHitDamageMultiplier = 1.8f;

    [Header("패턴2 - 1타 범위")]
    [Tooltip("세로 내려찍기 박스 너비")]
    [SerializeField] private float pattern2Hit1Range = 1.6f;
    [Tooltip("세로 내려찍기 박스 높이")]
    [SerializeField] private float pattern2Hit1Height = 2.4f;
    [Tooltip("세로 내려찍기 박스 X 오프셋(전방 거리)")]
    [SerializeField] private float pattern2Hit1Offset = 1.2f;
    [Tooltip("세로 내려찍기 박스 Y 오프셋")]
    [SerializeField] private float pattern2Hit1HeightOffset = 0f;

    [Header("패턴2 - 2타 범위")]
    [Tooltip("전진 세로 내려찍기 박스 너비")]
    [SerializeField] private float pattern2Hit2Range = 1.8f;
    [Tooltip("전진 세로 내려찍기 박스 높이")]
    [SerializeField] private float pattern2Hit2Height = 2.5f;
    [Tooltip("전진 세로 내려찍기 박스 X 오프셋")]
    [SerializeField] private float pattern2Hit2Offset = 1.5f;
    [Tooltip("전진 세로 내려찍기 박스 Y 오프셋")]
    [SerializeField] private float pattern2Hit2HeightOffset = 0f;

    [Header("패턴2 - 3타 범위")]
    [Tooltip("가로 강공격 박스 너비")]
    [SerializeField] private float pattern2Hit3Range = 3.2f;
    [Tooltip("가로 강공격 박스 높이")]
    [SerializeField] private float pattern2Hit3Height = 1.8f;
    [Tooltip("가로 강공격 박스 X 오프셋")]
    [SerializeField] private float pattern2Hit3Offset = 1.7f;
    [Tooltip("가로 강공격 박스 Y 오프셋")]
    [SerializeField] private float pattern2Hit3HeightOffset = 0f;

    [Header("패턴2 - 3타 넉백")]
    [Tooltip("3타 적중 시 플레이어 넉백 힘")]
    [SerializeField] private float pattern2ThirdHitKnockbackPower = 12f;
    [Tooltip("3타 적중 시 위로 띄우는 비율")]
    [SerializeField] private float pattern2ThirdHitUpPower = 0.35f;

    [Header("범위 시각화")]
    [Tooltip("패턴2 1타 박스 표시")]
    [SerializeField] private bool showPattern2Hit1Gizmo = true;
    [Tooltip("패턴2 2타 박스 표시")]
    [SerializeField] private bool showPattern2Hit2Gizmo = true;
    [Tooltip("패턴2 3타 박스 표시")]
    [SerializeField] private bool showPattern2Hit3Gizmo = true;

    private Color originalColor;
    private Coroutine buffRoutine;

    protected override void Awake()
    {
        base.Awake();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    // 기본공격 시작 시점 로그 출력
    protected override void OnBasicAttackStart()
    {
        Debug.Log("Stage1 MidBoss 기본공격: 철팔 휘두르기");
    }

    // 기본공격 1타: 부모 기본 동작 그대로 사용
    protected override void BasicAttackAction(int attackIndex)
    {
        Debug.Log($"철팔 휘두르기 {attackIndex + 1}타");
        base.BasicAttackAction(attackIndex);
    }

    // 패턴1 시작 시점 로그 출력
    protected override void OnPattern1Start()
    {
        Debug.Log("Stage1 MidBoss 패턴1: 돌진 박치기");
    }

    // 패턴1: 일정 시간 동안 lookDirection 방향으로 돌진하며 1회 적중
    protected override IEnumerator Pattern1Action()
    {
        float timer = 0f;
        bool hasHitPlayer = false;

        while (timer < data.pattern1Duration)
        {
            if (IsWallInFront())
                break;

            rb.linearVelocity = new Vector2(
                lookDirection * data.pattern1MoveSpeed,
                rb.linearVelocity.y
            );

            if (!hasHitPlayer)
            {
                Collider2D hit = GetPlayerHit(
                    data.pattern1Range,
                    data.pattern1Height,
                    data.pattern1Offset,
                    data.pattern1HeightOffset
                );

                if (hit != null && hit.TryGetComponent<PlayerController>(out var player))
                {
                    player.TakeDamage(GetFinalDamage(data.pattern1Damage), (Vector2)transform.position, true, false);

                    hasHitPlayer = true;
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }

        StopMove();
    }

    // 패턴2 시작 시점 로그 출력
    protected override void OnPattern2Start()
    {
        Debug.Log("Stage1 MidBoss 패턴2: 3연속 참격");
    }

    // 패턴2: 1타(세로) → 2타(전진 세로) → 3타(가로 강타+넉백) 3연속 참격
    protected override IEnumerator Pattern2Action()
    {
        Debug.Log("3연속 참격 1타 - 세로 내려찍기");

        HitPlayer(
            GetFinalDamage(data.pattern2Damage * pattern2FirstHitDamageMultiplier),
            pattern2Hit1Range,
            pattern2Hit1Height,
            pattern2Hit1Offset,
            pattern2Hit1HeightOffset
        );

        yield return new WaitForSeconds(data.pattern2AttackInterval);

        Debug.Log("3연속 참격 2타 - 전진 세로 내려찍기");

        HitPlayer(
            GetFinalDamage(data.pattern2Damage * pattern2SecondHitDamageMultiplier),
            pattern2Hit2Range,
            pattern2Hit2Height,
            pattern2Hit2Offset,
            pattern2Hit2HeightOffset
        );

        yield return new WaitForSeconds(data.pattern2AttackInterval);

        Debug.Log("3연속 참격 3타 - 가로 강공격");

        Collider2D hit = GetPlayerHit(
            pattern2Hit3Range,
            pattern2Hit3Height,
            pattern2Hit3Offset,
            pattern2Hit3HeightOffset
        );

        if (hit != null && hit.TryGetComponent<PlayerController>(out var player))
        {
            float damage = GetFinalDamage(data.pattern2Damage * pattern2ThirdHitDamageMultiplier);

            player.TakeDamage(damage, (Vector2)transform.position, true, false);

            // 3타 적중 시 강제 넉백 적용
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();

            if (playerRb != null)
            {
                Vector2 knockbackDir = new Vector2(
                    lookDirection,
                    pattern2ThirdHitUpPower
                ).normalized;

                playerRb.linearVelocity = Vector2.zero;

                playerRb.AddForce(
                    knockbackDir * pattern2ThirdHitKnockbackPower,
                    ForceMode2D.Impulse
                );
            }
        }
    }

    // 패턴3 시작 시점 로그 출력
    protected override void OnPattern3Start()
    {
        Debug.Log("Stage1 MidBoss 패턴3: 가짜 영광 버프 시전");
    }

    // 패턴3: 가짜 영광 버프(이동/공격력/쿨타임 강화) 활성화 시 스프라이트 색상 변경
    protected override IEnumerator Pattern3Action()
    {
        if (isBuffed)
            yield break;

        isBuffed = true;

        if (spriteRenderer != null)
            spriteRenderer.color = buffColor;

        Debug.Log("가짜 영광 시작: 이동속도, 공격력, 쿨타임 강화");

        if (buffRoutine != null)
            StopCoroutine(buffRoutine);

        buffRoutine = StartCoroutine(BuffDurationRoutine());

        yield break;
    }

    // 버프 지속 시간 종료 시 상태/색상 복구
    private IEnumerator BuffDurationRoutine()
    {
        yield return new WaitForSeconds(data.pattern3Duration);

        isBuffed = false;
        buffRoutine = null;

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        Debug.Log("가짜 영광 종료");
    }

    // 사망 시 진행 중인 버프 정리 후 부모 사망 처리 호출
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

    // 부모 시각화 + 패턴2 세부 타격 박스 추가 표시
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        int dir = lookDirection == 0 ? 1 : lookDirection;

        if (showPattern2Hit1Gizmo)
            DrawPattern2Box(Color.red, pattern2Hit1Range, pattern2Hit1Height, pattern2Hit1Offset, pattern2Hit1HeightOffset, dir);

        if (showPattern2Hit2Gizmo)
            DrawPattern2Box(Color.yellow, pattern2Hit2Range, pattern2Hit2Height, pattern2Hit2Offset, pattern2Hit2HeightOffset, dir);

        if (showPattern2Hit3Gizmo)
            DrawPattern2Box(Color.blue, pattern2Hit3Range, pattern2Hit3Height, pattern2Hit3Offset, pattern2Hit3HeightOffset, dir);
    }

    // 패턴2 타격 박스 와이어 큐브 한 개 그리기
    private void DrawPattern2Box(Color color, float range, float height, float offset, float heightOffset, int dir)
    {
        Gizmos.color = color;

        Vector3 center = transform.position + new Vector3(dir * offset, heightOffset, 0f);

        Gizmos.DrawWireCube(center, new Vector3(range, height, 0f));
    }
}
