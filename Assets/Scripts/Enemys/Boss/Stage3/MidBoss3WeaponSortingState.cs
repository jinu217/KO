using UnityEngine;

public sealed class MidBoss3WeaponSortingState : StateMachineBehaviour
{
    private const string WeaponPath = "Body/Shoulder_B/Forearm_B/Hand_B/Sword";
    public bool isSkill3;
    private static void Apply(Animator animator, string layer)
    {
        var weapon = animator.transform.Find(WeaponPath);
        if (weapon == null) return;
        var sprite = weapon.GetComponent<SpriteRenderer>();
        if (sprite != null) sprite.sortingLayerName = layer;
    }
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Apply(animator, isSkill3 ? "Back" : "Front");
    }
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!isSkill3) Apply(animator, "Front");
    }
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (isSkill3) Apply(animator, "Front");
    }
}
