using UnityEngine;

/// <summary>
/// Idle 상태에 붙이는 StateMachineBehaviour.
/// Idle에 진입할 때마다(Attack에서 오든, Fall/Landing에서 오든 어디서 오든)
/// 클립의 랜덤한 지점부터 재생을 시작해서, 매번 같은 프레임에서 "픽" 하고
/// 리셋되는 느낌 없이 자연스럽게 보이게 한다.
///
/// 사용법: Animator Controller에서 Idle State를 클릭 → Inspector 맨 아래
/// "Add Behaviour" → RandomizeIdleStart 추가. 코드 수정/연결 불필요.
/// </summary>
public class RandomizeIdleStart : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float randomNormalizedTime = Random.value; // 0~1 사이 랜덤 재생 위치
        animator.Play(stateInfo.fullPathHash, layerIndex, randomNormalizedTime);
    }
}
