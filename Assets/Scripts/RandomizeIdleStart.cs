using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Randomizes each Animator's looping Idle state only on its first entry.
/// Subsequent entries (e.g. after attacks) use the normal transition timing.
/// Install as RandomizeIdleStart.cs outside Editor folders.
/// </summary>
public class RandomizeIdleStart : StateMachineBehaviour
{
    // Shared guard also prevents duplicate behaviours from repeatedly seeking the same state.
    // Weak keys do not keep destroyed Animator instances alive.
    private static ConditionalWeakTable<Animator, HashSet<long>> initialized =
        new ConditionalWeakTable<Animator, HashSet<long>>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        initialized = new ConditionalWeakTable<Animator, HashSet<long>>();
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!Application.isPlaying || animator == null || !stateInfo.loop) return;

        long key = ((long)layerIndex << 32) | (uint)stateInfo.fullPathHash;
        HashSet<long> states = initialized.GetOrCreateValue(animator);
        // Mark BEFORE Play: even if the seek causes another OnStateEnter, it cannot seek again.
        if (!states.Add(key)) return;

        float start = Random.Range(0f, 0.999f);
        animator.Play(stateInfo.fullPathHash, layerIndex, start);
    }
}
