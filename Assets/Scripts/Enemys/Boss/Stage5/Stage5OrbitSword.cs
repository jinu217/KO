using UnityEngine;

[DefaultExecutionOrder(500), DisallowMultipleComponent]
public sealed class Stage5OrbitSword : MonoBehaviour
{
    [Min(.5f)] public float triggerDistance = 8f;
    [Min(.5f)] public float orbitRadius = 3.5f;
    [Min(0)] public float damage = 8f;
    public bool Active { get; private set; }
    const float Pause = .25f, Reposition = .12f, Thrust = .10f, Hold = .25f, Return = .4f;
    const float Beat = Reposition + Thrust, EndStrikes = Pause + 4 * Beat;
    const float EndHold = EndStrikes + Hold, EndSequence = EndHold + Return;
    static readonly float[] Angles = { 0, 100, 210, 300 };
    Stage5_MainBoss owner;
    Transform target, blade;
    SpriteRenderer art;
    Animator animator;
    Vector3 localPosition, launch, aim;
    Quaternion localRotation;
    bool posed, hit;
    float attackStart = -100, nextAttack, baseAngle, frozenAngle;
    int lastStrike = -1;
    Bounds previous;
    BossAttackStepData strike;

    public void Begin(Stage5_MainBoss boss, Transform player)
    {
        Cancel(); owner = boss; target = player;
        var fx = GetComponent<BossEffects>();
        blade = fx != null ? fx.weaponAnchor : null;
        art = blade != null ? blade.GetComponent<SpriteRenderer>() : null;
        animator = fx != null ? fx.motionAnimator : null;
        Active = art != null && animator != null;
        nextAttack = 1.5f; attackStart = -100; lastStrike = -1; hit = false;
        strike = new BossAttackStepData { actionType = BossActionType.MeleeAttack, damage = damage, attackCount = 1, hitboxSize = Vector2.one };
    }
    public void Cancel()
    {
        if (posed && blade != null) { blade.localPosition = localPosition; blade.localRotation = localRotation; }
        Active = posed = false;
    }
    void OnDisable() { Cancel(); }
    Vector2 Axis(int index)
    {
        float angle = (baseAngle + Angles[index]) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }
    Vector3 StartPoint(int index) => aim - (Vector3)Axis(index) * 3f;
    Vector3 EndPoint(int index) => aim + (Vector3)Axis(index) * .65f;

    void LateUpdate()
    {
        if (!Active) return;
        if (owner == null || owner.IsDead || target == null) { Cancel(); return; }
        var clips = animator.GetCurrentAnimatorClipInfo(0);
        if (clips.Length == 0 || !clips[0].clip.name.StartsWith("MainBoss5_skill1")) { Cancel(); return; }
        float t = animator.GetCurrentAnimatorStateInfo(0).normalizedTime * clips[0].clip.length;
        localPosition = blade.localPosition; localRotation = blade.localRotation; posed = true;
        if (t < 1 || t >= 7.5f) return;
        Vector3 authored = blade.position;
        Quaternion authoredRotation = blade.rotation;
        float orbitAngle = (t - 1) * 1.7f;
        Vector3 orbit = transform.position + new Vector3(Mathf.Cos(orbitAngle) * orbitRadius,
            .5f + Mathf.Sin(orbitAngle) * 1.1f + Mathf.Sin(t * 4) * .18f, 0);
        // A full turn during each floating cycle, with a quiet interval between turns.
        float spin = 360 * Mathf.SmoothStep(0, 1, Mathf.Clamp01((Mathf.Repeat(t - 1, 2.8f) - .65f) / .8f));
        float floatingAngle = Mathf.Atan2(.65f, -Mathf.Sin(orbitAngle)) * Mathf.Rad2Deg + spin;
        Vector3 point = orbit;
        float desiredAngle = floatingAngle;
        int damagingStrike = -1;
        // Finish all four strokes, hold and return before the authored hand catch begins.
        if (t <= 7 - EndSequence && t >= nextAttack && Vector2.Distance(target.position, transform.position) <= triggerDistance)
        {
            attackStart = t; launch = orbit; frozenAngle = floatingAngle;
            var col = target.GetComponent<Collider2D>(); aim = col != null ? col.bounds.center : target.position;
            Vector2 direction = aim - launch;
            baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            nextAttack = t + EndSequence + .35f; lastStrike = -1;
        }
        float age = t - attackStart;
        if (age >= 0 && age < EndSequence)
        {
            if (age < Pause) { point = launch; desiredAngle = frozenAngle; }
            else if (age < EndStrikes)
            {
                int index = Mathf.Clamp(Mathf.FloorToInt((age - Pause) / Beat), 0, 3);
                float phase = age - Pause - index * Beat;
                desiredAngle = baseAngle + Angles[index];
                if (phase < Reposition)
                {
                    float u = Mathf.SmoothStep(0, 1, phase / Reposition);
                    point = Vector3.Lerp(index == 0 ? launch : EndPoint(index - 1), StartPoint(index), u);
                    desiredAngle = Mathf.LerpAngle(index == 0 ? frozenAngle : baseAngle + Angles[index - 1], desiredAngle, u);
                }
                else
                {
                    point = Vector3.Lerp(StartPoint(index), EndPoint(index), (phase - Reposition) / Thrust);
                    damagingStrike = index;
                }
            }
            else if (age < EndHold) { point = EndPoint(3); desiredAngle = baseAngle + Angles[3]; }
            else
            {
                float u = Mathf.SmoothStep(0, 1, (age - EndHold) / Return);
                point = Vector3.Lerp(EndPoint(3), orbit, u);
                desiredAngle = Mathf.LerpAngle(baseAngle + Angles[3], floatingAngle, u);
            }
        }
        float weight = Mathf.SmoothStep(0, 1, (t - 1) / .3f) * (1 - Mathf.SmoothStep(0, 1, (t - 7) / .5f));
        Vector2 currentAxis = blade.TransformVector(Vector3.up);
        float currentAngle = Mathf.Atan2(currentAxis.y, currentAxis.x) * Mathf.Rad2Deg;
        blade.rotation = Quaternion.AngleAxis(Mathf.DeltaAngle(currentAngle, desiredAngle), Vector3.forward) * blade.rotation;
        blade.position += point - art.bounds.center;
        blade.position = Vector3.Lerp(authored, blade.position, weight);
        blade.rotation = Quaternion.Slerp(authoredRotation, blade.rotation, weight);
        Bounds contact = BossEffects.VisibleBounds(art); contact.Expand(.12f);
        if (damagingStrike >= 0)
        {
            if (lastStrike != damagingStrike)
            {
                lastStrike = damagingStrike; hit = false;
                strike.stepName = "예비 이기어검 " + (damagingStrike + 1) + "번째 찌르기";
                // Start the sweep at this stroke's launch, never at the previous angle.
                previous = contact; previous.center += StartPoint(damagingStrike) - point;
            }
            if (!hit) hit = Stage5PatternMotion.SweepAttack(strike, owner, previous, contact);
        }
        previous = contact;
    }
}
