using System.Collections;
using UnityEngine;

public class Stage1_MidBoss : MidBossController
{
    [Header("Stage 1 전용 연출")]
    [SerializeField] private Color buffColor = new Color(1f, 0.45f, 0.25f);

    [Header("패턴2 - 데미지 배율")]
    [SerializeField] private float pattern2FirstHitDamageMultiplier = 0.8f;
    [SerializeField] private float pattern2SecondHitDamageMultiplier = 1.0f;
    [SerializeField] private float pattern2ThirdHitDamageMultiplier = 1.8f;

    [Header("패턴2 - 1타 범위")]
    [SerializeField] private float pattern2Hit1Range = 1.6f;
    [SerializeField] private float pattern2Hit1Height = 2.4f;
    [SerializeField] private float pattern2Hit1Offset = 1.2f;
    [SerializeField] private float pattern2Hit1HeightOffset = 0f;

    [Header("패턴2 - 2타 범위")]
    [SerializeField] private float pattern2Hit2Range = 1.8f;
    [SerializeField] private float pattern2Hit2Height = 2.5f;
    [SerializeField] private float pattern2Hit2Offset = 1.5f;
    [SerializeField] private float pattern2Hit2HeightOffset = 0f;

    [Header("패턴2 - 3타 범위")]
    [SerializeField] private float pattern2Hit3Range = 3.2f;
    [SerializeField] private float pattern2Hit3Height = 1.8f;
    [SerializeField] private float pattern2Hit3Offset = 1.7f;
    [SerializeField] private float pattern2Hit3HeightOffset = 0f;

    [Header("패턴2 - 3타 넉백")]
    [SerializeField] private float pattern2ThirdHitKnockbackPower = 12f;
    [SerializeField] private float pattern2ThirdHitUpPower = 0.35f;

    private Color originalColor;
    private Coroutine buffRoutine;

    protected override void Awake()
    {
        base.Awake();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    protected override void OnBasicAttackStart()
    {
        Debug.Log("Stage1 MidBoss 기본공격: 철팔 휘두르기");
    }

    protected override void BasicAttackAction(int attackIndex)
    {
        Debug.Log($"철팔 휘두르기 {attackIndex + 1}타");
        base.BasicAttackAction(attackIndex);
    }

    protected override void OnPattern1Start()
    {
        Debug.Log("Stage1 MidBoss 패턴1: 돌진 박치기");
    }

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
                    player.TakeDamageNoIFrame(
                        GetFinalDamage(data.pattern1Damage),
                        (Vector2)transform.position
                    );

                    hasHitPlayer = true;
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }

        StopMove();
    }

    protected override void OnPattern2Start()
    {
        Debug.Log("Stage1 MidBoss 패턴2: 3연속 참격");
    }

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

            player.TakeDamageNoIFrame(damage, (Vector2)transform.position);

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

    protected override void OnPattern3Start()
    {
        Debug.Log("Stage1 MidBoss 패턴3: 가짜 영광 버프 시전");
    }

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

    private IEnumerator BuffDurationRoutine()
    {
        yield return new WaitForSeconds(data.pattern3Duration);

        isBuffed = false;
        buffRoutine = null;

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        Debug.Log("가짜 영광 종료");
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

        DrawPattern2Box(Color.red, pattern2Hit1Range, pattern2Hit1Height, pattern2Hit1Offset, pattern2Hit1HeightOffset, dir);
        DrawPattern2Box(Color.yellow, pattern2Hit2Range, pattern2Hit2Height, pattern2Hit2Offset, pattern2Hit2HeightOffset, dir);
        DrawPattern2Box(Color.blue, pattern2Hit3Range, pattern2Hit3Height, pattern2Hit3Offset, pattern2Hit3HeightOffset, dir);
    }

    private void DrawPattern2Box(
        Color color,
        float range,
        float height,
        float offset,
        float heightOffset,
        int dir
    )
    {
        Gizmos.color = color;

        Vector3 center = transform.position + new Vector3(
            dir * offset,
            heightOffset,
            0f
        );

        Gizmos.DrawWireCube(
            center,
            new Vector3(range, height, 0f)
        );
    }
}