using UnityEngine;

// 시전 직전의 체력으로 되돌아가려는 5스테이지 메인보스의 욕망을 관리한다.
[DisallowMultipleComponent]
public sealed class Stage5_MainBossDesireSkill : MonoBehaviour
{
    [Header("발동")]
    [SerializeField, Min(0f), Tooltip("3페이즈에서 발동한 순간부터 유지되는 시간입니다.")]
    private float duration = 20f;
    [SerializeField, Min(0f), Tooltip("발동 순간부터 계산하는 기본 재사용 대기시간입니다.")]
    private float cooldown = 50f;
    [SerializeField, Min(0f), Tooltip("8스택 달성으로 체력을 되돌렸을 때 적용할 재사용 대기시간입니다.")]
    private float rewindCooldown = 1f;

    [Header("욕망 강화")]
    [SerializeField, Min(0f), Tooltip("욕망 유지 중 받는 피해 배율입니다. 1.25는 피해 25% 증가입니다.")]
    private float incomingDamageMultiplier = 1.25f;
    [SerializeField, Min(0f), Tooltip("욕망 유지 중 공격력 배율입니다. 1.15는 공격력 15% 증가입니다.")]
    private float damageMultiplier = 1.15f;
    [SerializeField, Min(0f), Tooltip("욕망 유지 중 공격속도 배율입니다. 1.15는 공격속도 15% 증가입니다.")]
    private float attackSpeedMultiplier = 1.15f;
    [SerializeField, Min(0f), Tooltip("욕망 유지 중 추적 이동속도 배율입니다. 1.2는 이동속도 20% 증가입니다.")]
    private float moveSpeedMultiplier = 1.2f;

    [Header("거리와 스택")]
    [SerializeField, Min(0f), Tooltip("플레이어가 이 거리 안에 있으면 스택이 증가합니다. 보스 중심부터 측정합니다.")]
    private float nearDistance = 5f;
    [SerializeField, Min(0f), Tooltip("플레이어가 거리 안에 있을 때 초당 증가하는 스택입니다.")]
    private float gainPerSecond = 1f;
    [SerializeField, Min(0f), Tooltip("플레이어가 거리 밖에 있을 때 초당 감소하는 스택입니다. 0 아래로 내려가지 않습니다.")]
    private float lossPerSecond = .5f;
    [SerializeField, Min(0f), Tooltip("이 수치에 도달하면 체력을 시전 직전으로 되돌리고 욕망을 종료합니다.")]
    private float targetStacks = 8f;
    [SerializeField, Min(0f), Tooltip("지속시간 동안 목표 스택을 모으지 못했을 때 행동 불가 시간입니다.")]
    private float groggyDuration = 3f;

    private Stage5_MainBoss owner;
    private float nextUseTime;
    private float endTime;
    private float groggyEndTime;
    private float savedHp;
    private float stacks;
    private bool active;

    public bool Active => active;
    public bool Groggy => Time.time < groggyEndTime;
    public float IncomingDamageMultiplier => active ? incomingDamageMultiplier : 1f;
    public float DamageMultiplier => active ? damageMultiplier : 1f;
    public float AttackSpeedMultiplier => active ? attackSpeedMultiplier : 1f;
    public float MoveSpeedMultiplier => active ? moveSpeedMultiplier : 1f;
    public float Stacks => stacks;

    public void Initialize(Stage5_MainBoss boss) => owner = boss;

    public bool Tick(int phase)
    {
        if (owner == null || owner.IsDead) return false;
        if (!active && !Groggy && phase >= 3 && owner.DesireInCombat
            && Time.time >= nextUseTime && !owner.DesireBusy)
            Activate();
        return Groggy;
    }

    private void Update()
    {
        if (owner == null || owner.IsDead) return;
        if (Groggy) owner.DesireStop();
        if (!active) return;

        // 시전 순간부터 정확히 20초까지만 쌓을 수 있다.
        float remaining = endTime - Time.time;
        if (remaining <= 0f)
        {
            Fail();
            return;
        }

        Transform player = owner.DesirePlayer;
        bool near = player != null
            && Vector2.Distance(owner.transform.position, player.position) <= nearDistance;
        stacks = Mathf.Clamp(stacks + (near ? gainPerSecond : -lossPerSecond) * Time.deltaTime,
            0f, targetStacks);
        if (stacks >= targetStacks) Succeed();
    }

    private void Activate()
    {
        active = true;
        savedHp = owner.DesireHp;
        stacks = 0f;
        endTime = Time.time + duration;
        nextUseTime = Time.time + cooldown;
        owner.DesireLog("욕망 시작: 체력 저장, 스택 0");
    }

    private void Succeed()
    {
        active = false;
        owner.DesireInterrupt();
        owner.DesireRestoreHp(savedHp);
        nextUseTime = Time.time + rewindCooldown;
        owner.DesireLog("욕망 성공: 시전 전 체력 복원, 재사용 대기시간 1초");
    }

    private void Fail()
    {
        active = false;
        owner.DesireInterrupt();
        groggyEndTime = Time.time + groggyDuration;
        owner.DesireStop();
        owner.DesireLog("욕망 실패: 체력 유지, 3초 그로기");
    }

    public void Cancel()
    {
        active = false;
        groggyEndTime = 0f;
    }

    private void OnDisable() => Cancel();
}
