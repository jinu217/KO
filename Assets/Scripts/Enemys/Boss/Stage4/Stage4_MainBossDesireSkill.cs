using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Stage4_MainBossDesireSkill : MonoBehaviour
{
    [SerializeField, Min(0), Tooltip("발동 순간부터 계산하는 욕망 지속시간입니다.")] private float duration = 20f;
    [SerializeField, Min(0), Tooltip("발동 순간부터 계산하는 재사용 대기시간입니다.")] private float cooldown = 50f;
    [SerializeField, Min(0), Tooltip("제자리에서 포효하는 준비 시간입니다.")] private float castDelay = .7f;
    [SerializeField, Tooltip("욕망 유지 중 공격력 배율입니다.")] private float damageMultiplier = 1.15f;
    [SerializeField, Tooltip("욕망 유지 중 공격속도 배율입니다.")] private float attackSpeedMultiplier = 1.2f;
    [SerializeField, Tooltip("욕망 유지 중 추적 이동속도 배율입니다.")] private float moveMultiplier = 1.2f;
    [SerializeField, Range(0, 1), Tooltip("종료 HP에서 시작 HP를 뺀 값이 최대 HP의 이 비율 미만이면 그로기입니다.")] private float requiredHpGain = .05f;
    [SerializeField, Min(0), Tooltip("목표 HP 증가량을 달성하지 못했을 때 행동 불가 시간입니다.")] private float groggyDuration = 3f;
    [SerializeField, Range(0, 1), Tooltip("기본 공격 한 패턴에서 최초 적중 시 회복하는 최대 HP 비율입니다.")] private float basicHeal = .01f;
    [SerializeField, Range(0, 1), Tooltip("1스킬 한 패턴에서 최초 적중 시 회복하는 최대 HP 비율입니다.")] private float skill1Heal = .02f;
    [SerializeField, Range(0, 1), Tooltip("2스킬 보디 블로 적중 시 회복하는 최대 HP 비율입니다.")] private float bodyHeal = .02f;
    [SerializeField, Range(0, 1), Tooltip("2스킬 어퍼컷 적중 시 추가 회복하는 최대 HP 비율입니다.")] private float uppercutHeal = .02f;
    [SerializeField, Range(0, 1), Tooltip("4스킬 한 패턴에서 최초 적중 시 회복하는 최대 HP 비율입니다.")] private float skill4Heal = .03f;
    [SerializeField, Tooltip("발동 시 주변 플레이어를 밀어내는 판정입니다. 피해는 0으로 유지할 수 있습니다.")]
    private BossAttackStepData roar = new BossAttackStepData { actionType = BossActionType.MeleeAttack, attackCount = 1, hitboxSize = new Vector2(6, 4), knockbackForce = new Vector2(3, 1), knockbackAwayFromCenter = true };
    private Stage4_MainBoss owner;
    private BossSkillData[] skills;
    private float nextUse, end, startHp, groggyEnd;
    private bool active, casting;
    public bool Active => active && Time.time < end;
    public bool BlocksCombat => casting || Time.time < groggyEnd;
    public float DamageMultiplier => damageMultiplier;
    public float AttackSpeedMultiplier => attackSpeedMultiplier;
    public float MoveMultiplier => moveMultiplier;
    public float UppercutHeal => uppercutHeal;
    public BossSkillData Basic { get; private set; }
    public BossSkillData Skill(int index) => index >= 0 && index < skills.Length ? skills[index] : null;
    public float HealRatio(int index) => index < 0 ? basicHeal : index == 0 ? skill1Heal : index == 1 ? bodyHeal : index == 3 ? skill4Heal : 0f;

    public void Initialize(Stage4_MainBoss boss)
    {
        owner = boss;
        Basic = Enhance(owner.data.basicAttack);
        Basic.active.attackSteps.Add(Stage4BossPatterns.Punch("4타 전진 스트레이트", 20, .16f, true));
        skills = new BossSkillData[owner.data.skills.Count];
        for (int i = 0; i < skills.Length; i++) skills[i] = Enhance(owner.data.skills[i]);
        if (skills.Length > 1)
        {
            skills[1].active.attackSteps.Add(new BossAttackStepData { stepName = "점프 스트레이트 도약", actionType = BossActionType.Jump, jumpForce = new Vector2(2, 7), duration = .18f });
            var strike = Stage4BossPatterns.Punch("공중 스트레이트", 22, .04f);
            strike.hitboxSize = new Vector2(2.4f, 3f);
            strike.hitboxOffset = new Vector2(1, .5f);
            strike.knockbackForce = new Vector2(2, -8);
            skills[1].active.attackSteps.Add(strike);
        }
        if (skills.Length > 3)
        {
            var finish = Stage4BossPatterns.Punch("최종 스트레이트", 25, .4f, true);
            finish.knockbackForce = new Vector2(12, 4);
            skills[3].active.attackSteps.Add(finish);
        }
    }
    private static BossSkillData Enhance(BossSkillData source)
    {
        var clone = JsonUtility.FromJson<BossSkillData>(JsonUtility.ToJson(source));
        foreach (var step in clone.active.attackSteps)
        {
            step.moveSpeed *= 1.2f;
            step.maxMoveDistance *= 1.3f;
        }
        return clone;
    }
    private void Update()
    {
        if (owner == null || owner.IsDead) return;
        if (active && Time.time >= end)
        {
            active = false;
            float gain = owner.Hp - startHp;
            float target = owner.data.maxHp * requiredHpGain;
            bool failed = gain < target && !Mathf.Approximately(gain, target);
            owner.DesireLog($"종료: HP 증가 {owner.Hp - startHp:F1}, 필요 {owner.data.maxHp * requiredHpGain:F1}, 그로기 {failed}");
            owner.InterruptAttack();
            if (failed) groggyEnd = Time.time + groggyDuration;
        }
        if (BlocksCombat && !owner.Busy) owner.Halt();
        if (CanActivate(owner.Phase))
            StartCoroutine(Activate());
    }
    private bool CanActivate(int phase)
        => owner != null && !owner.IsDead && !active && !BlocksCombat
            && owner.InCombat && phase >= 3 && Time.time >= nextUse && !owner.Busy;

    public bool Tick(int phase)
    {
        if (owner == null || owner.IsDead) return false;
        if (BlocksCombat) return true;
        if (CanActivate(phase))
        {
            StartCoroutine(Activate());
            return true;
        }
        return false;
    }
    private IEnumerator Activate()
    {
        nextUse = Time.time + cooldown;
        end = Time.time + duration;
        startHp = owner.Hp;
        active = casting = true;
        owner.InterruptAttack();
        owner.Busy = true;
        owner.Halt();
        owner.DesireLog("시작");
        // 포효는 피해 없이 밀어내므로 피해가 필요한 공통 공격 판정과 별도로 처리한다.
        var seen = new System.Collections.Generic.HashSet<Rigidbody2D>();
        foreach (var hit in Physics2D.OverlapBoxAll(owner.transform.position, roar.hitboxSize, 0, owner.PlayerLayer))
        {
            var body = hit.GetComponentInParent<Rigidbody2D>();
            if (body == null || !seen.Add(body)) continue;
            float direction = body.position.x >= owner.transform.position.x ? 1 : -1;
            hit.GetComponentInParent<PlayerController>()?.ApplyMoveDelay(.2f);
            body.linearVelocity = Vector2.zero;
            body.AddForce(new Vector2(direction * roar.knockbackForce.x, roar.knockbackForce.y), ForceMode2D.Impulse);
        }
        yield return new WaitForSeconds(castDelay);
        casting = false;
        owner.Busy = false;
    }
    public void Cancel() { StopAllCoroutines(); active = casting = false; groggyEnd = 0; }
    private void OnDisable() { Cancel(); }
}
