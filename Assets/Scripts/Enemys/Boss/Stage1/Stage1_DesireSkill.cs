using System.Collections;
using UnityEngine;

// 1스테이지 메인보스만 사용하는 전용기. 일반 BossSkillData 목록과 분리된다.
public sealed class Stage1_DesireSkill : MonoBehaviour
{
    [Header("발동")]
    [SerializeField] private float cooldown = 30f;
    [SerializeField] private float preDelay = 0.8f;
    [SerializeField] private float postDelay = 0.5f;

    [Header("15초 강화")]
    [SerializeField] private float mainBuffDuration = 15f;
    [SerializeField] private float attackSpeedMultiplier = 1.4f;
    [SerializeField] private float moveSpeedMultiplier = 1.25f;
    [SerializeField] private BossSkillData enhancedBasicAttack;

    [Header("10초 스킬 강화")]
    [SerializeField] private float skillEnhanceDuration = 10f;
    [SerializeField] private BossSkillData enhancedSkill1;
    [SerializeField] private BossSkillData enhancedSkill2;

    private float lastUseTime = -999f;
    public bool MainBuffActive { get; private set; }
    public bool SkillEnhanceActive { get; private set; }
    public float AttackSpeedMultiplier => MainBuffActive ? attackSpeedMultiplier : 1f;
    public float MoveSpeedMultiplier => MainBuffActive ? moveSpeedMultiplier : 1f;
    public BossSkillData EnhancedBasicAttack => enhancedBasicAttack;
    public BossSkillData EnhancedSkill1 => enhancedSkill1;
    public BossSkillData EnhancedSkill2 => enhancedSkill2;

    public void Configure(BossSkillData basic, BossSkillData skill1, BossSkillData skill2)
    {
        enhancedBasicAttack = basic;
        enhancedSkill1 = skill1;
        enhancedSkill2 = skill2;
    }

    public bool CanActivate(int phase) => phase >= 3 && !MainBuffActive && Time.time >= lastUseTime + cooldown;

    public IEnumerator Activate(System.Action resetCooldowns)
    {
        lastUseTime = Time.time;
        if (preDelay > 0f) yield return new WaitForSeconds(preDelay);
        resetCooldowns?.Invoke();
        StartCoroutine(MainBuffRoutine());
        StartCoroutine(SkillEnhanceRoutine());
        if (postDelay > 0f) yield return new WaitForSeconds(postDelay);
    }

    private IEnumerator MainBuffRoutine()
    {
        MainBuffActive = true;
        yield return new WaitForSeconds(mainBuffDuration);
        MainBuffActive = false;
    }

    private IEnumerator SkillEnhanceRoutine()
    {
        SkillEnhanceActive = true;
        yield return new WaitForSeconds(skillEnhanceDuration);
        SkillEnhanceActive = false;
    }

    public void Cancel()
    {
        StopAllCoroutines();
        MainBuffActive = false;
        SkillEnhanceActive = false;
    }
}
