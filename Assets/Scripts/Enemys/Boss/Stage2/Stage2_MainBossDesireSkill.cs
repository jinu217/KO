using System.Collections;
using UnityEngine;

// 2스테이지 메인보스의 안전 욕구 전용 상태와 행동을 모두 소유한다.
public sealed class Stage2_MainBossDesireSkill : MonoBehaviour
{
    [Header("발동")]
    [SerializeField, Min(0f), Tooltip("욕망 스킬 재사용 대기시간입니다. 발동 시작 순간부터 계산합니다.")]
    private float cooldown = 60f;
    [SerializeField, Min(0f), Tooltip("욕망 스킬 발동 후 일반 행동을 다시 시작하기 전 대기시간입니다.")]
    private float castDelay = 0.8f;
    [SerializeField, Min(0f), Tooltip("안전 필드가 유지되는 시간입니다.")]
    private float fieldDuration = 25f;
    [SerializeField, Min(0f), Tooltip("기본 피해 감소와 필드 제거 후 피해 증가를 포함한 전체 효과 시간입니다.")]
    private float damageEffectDuration = 30f;
    [SerializeField, Tooltip("욕망 스킬 발동 위치에 생성할 안전 필드 프리팹입니다. 2스테이지 안전 필드 컴포넌트와 트리거 방식의 2D 충돌체가 필요합니다.")]
    private GameObject fieldPrefab;
    [SerializeField, InspectorName("필드 생성 Y 오프셋"), Tooltip("보스 위치를 기준으로 한 필드의 세로 생성 위치입니다. 양수는 위쪽, 음수는 아래쪽으로 이동합니다. X 위치는 항상 보스와 동일합니다.")]
    private float fieldSpawnYOffset;
    [SerializeField, Min(0f), Tooltip("욕망 스킬 발동 시 플레이어를 밀어내는 원형 범위입니다.")]
    private float activationKnockbackRadius = 4.5f;
    [SerializeField, Tooltip("발동 넉백의 X/Y 힘입니다. X는 필드 중심에서 바깥 방향으로 적용됩니다.")]
    private Vector2 activationKnockbackForce = new Vector2(14f, 5f);

    [Header("필드 내부 효과")]
    [SerializeField, InspectorName("기본 피해 감소율"), Range(0f, 1f), Tooltip("욕망 효과 중 항상 적용되는 받는 피해 감소율입니다. 0.25는 25% 감소입니다.")]
    private float baseDamageReduction = 0.25f;
    [SerializeField, InspectorName("필드 추가 피해 감소율"), Range(0f, 1f), Tooltip("보스가 필드 안에 있을 때 기본 감소율에 더해지는 피해 감소율입니다.")]
    private float fieldDamageReduction = 0.5f;
    [SerializeField, InspectorName("필드 내부 공격력 배율"), Min(0f), Tooltip("보스가 필드 안에 있을 때 적용되는 공격력 배율입니다. 1.1은 10% 증가입니다.")]
    private float fieldAttackMultiplier = 1.1f;
    [SerializeField, InspectorName("체력 회복 간격"), Min(0.1f), Tooltip("보스가 필드 안에 있을 때 체력을 회복하는 간격입니다.")]
    private float healInterval = 3f;
    [SerializeField, InspectorName("최대 체력 회복 비율"), Range(0f, 1f), Tooltip("한 번 회복할 최대 체력 비율입니다. 0.03은 최대 체력의 3%입니다.")]
    private float healMaxHpRatio = 0.03f;
    [SerializeField, InspectorName("필드 종료 후 피해 증가율"), Min(0f), Tooltip("필드 제거 후 전체 효과 종료까지 적용되는 받는 피해 증가율입니다.")]
    private float postFieldDamageTakenIncrease = 0.25f;

    [Header("외부 복귀")]
    [SerializeField, Min(0f), Tooltip("보스가 필드 밖으로 나온 뒤 복귀용 넉백 공격을 시도하기까지의 시간입니다.")]
    private float returnAttackDelay = 5f;
    [SerializeField, Min(0f), Tooltip("보스가 필드 밖으로 나온 뒤 전투를 중단하고 강제로 복귀하기까지의 시간입니다.")]
    private float forcedReturnDelay = 10f;
    [SerializeField, Min(0f), Tooltip("복귀용 넉백 공격 판정이 나오기 전 선딜레이입니다.")]
    private float returnAttackPreDelay = 0.7f;
    [SerializeField, Min(0f), Tooltip("복귀용 넉백 공격이 플레이어에게 주는 피해입니다.")]
    private float returnAttackDamage = 10f;
    [SerializeField, Tooltip("복귀용 넉백 공격 판정의 가로(X)·세로(Y) 크기입니다.")]
    private Vector2 returnAttackSize = new Vector2(3.5f, 1.8f);
    [SerializeField, Tooltip("보스 위치를 기준으로 한 복귀 공격 판정 중심입니다. X는 보는 방향으로 반전됩니다.")]
    private Vector2 returnAttackOffset = new Vector2(1.8f, 0f);
    [SerializeField, Tooltip("복귀 공격 적중 시 적용할 X/Y 넉백 힘입니다.")]
    private Vector2 returnAttackKnockback = new Vector2(16f, 5f);
    [SerializeField, Min(0f), Tooltip("복귀 공격이 빗나갔을 때 보스가 행동하지 못하는 시간입니다.")]
    private float missedAttackStunDuration = 3f;
    [SerializeField, Min(0f), Tooltip("강제 복귀 상태에서 필드 중심으로 이동하는 속도입니다.")]
    private float returnMoveSpeed = 8f;

    private Stage2_MainBoss owner;
    private Stage2_SafetyField fieldVisual;
    private Vector2 fieldCenter;
    private float lastUseTime = -999f;
    private float fieldEndTime;
    private float damageEffectEndTime;
    private float nextHealTime;
    private float outsideSince = -1f;
    private bool fieldActive;
    private bool wasInside;
    private bool combatEngaged;
    private bool returnAttackAttempted;
    private bool returning;
    private bool stunned;

    public bool FieldActive => fieldActive;
    public bool BlocksCombatAI => fieldActive && (returning || stunned || (IsBossInside() && !IsPlayerInside() && !combatEngaged));
    public float OutgoingDamageMultiplier => fieldActive && IsBossInside() ? fieldAttackMultiplier : 1f;

    public void Initialize(Stage2_MainBoss boss) => owner = boss;

    public bool Tick(int phase)
    {
        if (owner == null || owner.DesireIsDead) return false;
        if (phase >= 3 && !fieldActive && Time.time >= lastUseTime + cooldown && !owner.DesireIsAttacking)
        {
            StartCoroutine(Activate());
            return true;
        }
        if (!fieldActive) return false;

        UpdateField();
        if (!fieldActive) return false;
        if (returning || stunned || owner.DesireIsAttacking) return true;

        if (outsideSince >= 0f)
        {
            float outsideTime = Time.time - outsideSince;
            if (outsideTime >= forcedReturnDelay) { BeginReturn(); return true; }
            if (outsideTime >= returnAttackDelay && !returnAttackAttempted)
            {
                StartCoroutine(UseReturnAttack());
                return true;
            }
        }
        return false;
    }

    public bool CanRunCombatAI()
    {
        if (!fieldActive) return true;
        if (returning || stunned) return false;
        if (!IsBossInside()) return true;
        if (IsPlayerInside())
        {
            combatEngaged = true;
            owner.DesireSetChasing(true);
        }
        return IsPlayerInside() || combatEngaged;
    }

    public void FixedTick()
    {
        if (!returning || !fieldActive || owner?.DesireBody == null) return;
        if (IsBossInside()) { FinishReturn(); return; }
        int direction = fieldCenter.x >= transform.position.x ? 1 : -1;
        Rigidbody2D body = owner.DesireBody;
        body.linearVelocity = new Vector2(direction * returnMoveSpeed, body.linearVelocity.y);
    }

    public float ModifyIncomingDamage(float damage)
    {
        if (fieldActive)
        {
            float reduction = baseDamageReduction + (IsBossInside() ? fieldDamageReduction : 0f);
            return damage * (1f - Mathf.Clamp01(reduction));
        }
        return Time.time < damageEffectEndTime ? damage * (1f + postFieldDamageTakenIncrease) : damage;
    }

    public void OnBossDamaged()
    {
        if (!fieldActive || returning || stunned) return;
        combatEngaged = true;
        owner.DesireSetChasing(true);
    }

    public void Cancel()
    {
        StopAllCoroutines();
        RemoveField();
    }

    private IEnumerator Activate()
    {
        owner.DesireIsAttacking = true;
        owner.DesireStop();
        lastUseTime = Time.time;
        fieldCenter = (Vector2)transform.position + Vector2.up * fieldSpawnYOffset;
        if (!CreateField())
        {
            owner.DesireIsAttacking = false;
            yield break;
        }

        fieldEndTime = Time.time + fieldDuration;
        damageEffectEndTime = Time.time + damageEffectDuration;
        fieldActive = true;
        wasInside = true;
        combatEngaged = false;
        outsideSince = -1f;
        returnAttackAttempted = false;
        nextHealTime = Time.time + healInterval;
        PushPlayer();
        owner.DesireLog("욕망 스킬 시작", "안전에 대한 강박", cooldown);
        if (castDelay > 0f) yield return new WaitForSeconds(castDelay);
        owner.DesireIsAttacking = false;
    }

    private void UpdateField()
    {
        if (fieldVisual == null) { RemoveField(); return; }
        if (Time.time >= fieldEndTime) { RemoveField(); return; }
        bool inside = IsBossInside();
        if (inside && Time.time >= nextHealTime)
        {
            owner.DesireHeal(owner.DesireMaxHp * healMaxHpRatio);
            nextHealTime = Time.time + healInterval;
        }
        if (!inside && wasInside)
        {
            outsideSince = Time.time;
            returnAttackAttempted = false;
        }
        else if (inside && !wasInside && !returning)
        {
            outsideSince = -1f;
            returnAttackAttempted = false;
            nextHealTime = Time.time + healInterval;
        }
        wasInside = inside;
    }

    private IEnumerator UseReturnAttack()
    {
        returnAttackAttempted = true;
        owner.DesireIsAttacking = true;
        owner.DesireStop();
        owner.DesireFacePlayer();
        owner.DesireLog("복귀 넉백 공격 시작", "안전지대 확보", 0f);
        if (returnAttackPreDelay > 0f) yield return new WaitForSeconds(returnAttackPreDelay);

        int direction = owner.DesireLookDirection;
        Vector2 center = (Vector2)transform.position + new Vector2(direction * returnAttackOffset.x, returnAttackOffset.y);
        Collider2D hit = Physics2D.OverlapBox(center, returnAttackSize, 0f, owner.DesirePlayerLayer);
        IDamageable target = hit != null ? hit.GetComponentInParent<IDamageable>() : null;
        if (target != null)
        {
            target.TakeDamage(returnAttackDamage, transform.position, false, false);
            Rigidbody2D targetBody = hit.GetComponentInParent<Rigidbody2D>();
            if (targetBody != null)
            {
                targetBody.linearVelocity = Vector2.zero;
                targetBody.AddForce(new Vector2(direction * returnAttackKnockback.x, returnAttackKnockback.y), ForceMode2D.Impulse);
            }
            owner.DesireIsAttacking = false;
            BeginReturn();
            yield break;
        }

        stunned = true;
        yield return new WaitForSeconds(missedAttackStunDuration);
        stunned = false;
        owner.DesireIsAttacking = false;
        if (fieldActive && outsideSince >= 0f && Time.time - outsideSince >= forcedReturnDelay)
            BeginReturn();
    }

    private void BeginReturn()
    {
        if (!fieldActive) return;
        returning = true;
        stunned = false;
        owner.DesireIsAttacking = false;
        owner.DesireSetChasing(false);
        owner.DesireStop();
    }

    private void FinishReturn()
    {
        returning = false;
        outsideSince = -1f;
        returnAttackAttempted = false;
        combatEngaged = false;
        wasInside = true;
        nextHealTime = Time.time + healInterval;
        owner.DesireStop();
    }

    private bool CreateField()
    {
        if (fieldPrefab == null)
        {
            Debug.LogError($"[{name}] 2스테이지 안전 필드 프리팹이 연결되지 않았습니다.", this);
            return false;
        }

        GameObject fieldObject = Instantiate(fieldPrefab, fieldCenter, Quaternion.identity);
        fieldVisual = fieldObject.GetComponent<Stage2_SafetyField>();
        if (fieldVisual == null || !fieldVisual.Initialize())
        {
            Debug.LogError($"[{name}] 안전 필드 프리팹에 Stage2_SafetyField와 Collider2D가 필요합니다.", fieldObject);
            Destroy(fieldObject);
            fieldVisual = null;
            return false;
        }

        fieldCenter = fieldVisual.Center;
        return true;
    }

    private void RemoveField()
    {
        fieldActive = returning = stunned = combatEngaged = false;
        outsideSince = -1f;
        if (fieldVisual != null) Destroy(fieldVisual.gameObject);
        fieldVisual = null;
        if (owner != null) owner.DesireSetChasing(owner.DesirePlayer != null);
    }

    private bool IsBossInside() => fieldActive && fieldVisual != null && fieldVisual.Contains(transform.position);
    private bool IsPlayerInside() => fieldActive && fieldVisual != null && owner?.DesirePlayer != null && fieldVisual.Contains(owner.DesirePlayer.position);

    private void PushPlayer()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, activationKnockbackRadius, owner.DesirePlayerLayer);
        Rigidbody2D targetBody = hit != null ? hit.GetComponentInParent<Rigidbody2D>() : null;
        if (targetBody == null) return;
        float direction = Mathf.Sign(targetBody.position.x - transform.position.x);
        if (direction == 0f) direction = 1f;
        targetBody.linearVelocity = Vector2.zero;
        targetBody.AddForce(new Vector2(direction * activationKnockbackForce.x, activationKnockbackForce.y), ForceMode2D.Impulse);
    }

    private void OnDrawGizmosSelected()
    {
        if (!fieldActive || fieldVisual == null) return;
        Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.8f);
        Bounds bounds = fieldVisual.EffectBounds;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
