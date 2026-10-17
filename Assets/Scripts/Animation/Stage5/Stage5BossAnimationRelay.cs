using System;
using System.Collections;
using UnityEngine;

// Animation-only bridge. Gameplay code owns damage, movement, buffs and hit confirmation.
[DisallowMultipleComponent]
public sealed class Stage5BossAnimationRelay : MonoBehaviour
{
    public event Action<string> Cue;
    private Animator cachedAnimator;
    private Animator AnimatorComponent
    {
        get
        {
            if (cachedAnimator == null) cachedAnimator = GetComponent<Animator>();
            return cachedAnimator;
        }
    }

    // Called by generated AnimationEvents. No damage or movement is performed here.
    public void OnStage5AnimationCue(string cue) { Cue?.Invoke(cue); }

    public void ConfirmSkill3Hit() { Trigger("Skill3Hit"); }
    public void EndMidBossDash() { Trigger("DashEnd"); }
    public void EnterGroggy() { Trigger("Groggy"); }
    public void RecoverFromGroggy() { Trigger("GroggyEnd"); }
    public void DesireSucceeded() { Trigger("DesireSuccess"); }
    public void PlayAction(string trigger) { Trigger(trigger); }

    public void SetMotion(float speed,bool chasing,bool busy)
    {
        var a=AnimatorComponent;if(a==null||a.runtimeAnimatorController==null)return;
        a.SetFloat("MoveSpeed",speed);a.SetBool("IsChasing",chasing);a.SetBool("IsAttacking",busy);
    }
    public void PlayState(string state,float rate=1f)
    {
        var a=AnimatorComponent;if(a==null||a.runtimeAnimatorController==null)return;
        int hash=Animator.StringToHash("Base Layer."+state);
        if(!a.HasState(0,hash)){Debug.LogWarning("Stage5 state missing: "+state,this);return;}
        foreach(var p in a.parameters)if(p.type==AnimatorControllerParameterType.Trigger)a.ResetTrigger(p.name);
        a.SetFloat("AttackSpeed",rate);a.SetBool("IsAttacking",true);a.Play(hash,0,0);
    }
    public IEnumerator WaitForEnd()
    {
        var a=AnimatorComponent;
        if(a==null||!a.isActiveAndEnabled||a.runtimeAnimatorController==null)yield break;
        yield return null;
        int hash=a.GetCurrentAnimatorStateInfo(0).shortNameHash;
        while(a!=null&&a.isActiveAndEnabled)
        {
            var state=a.GetCurrentAnimatorStateInfo(0);
            if(state.shortNameHash!=hash||state.normalizedTime>=1||state.loop)yield break;
            yield return null;
        }
    }

    private void Trigger(string name)
    {
        Animator a = AnimatorComponent;
        if (a == null) return;
        foreach (var parameter in a.parameters)
            if (parameter.name == name && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                a.SetTrigger(name);
                return;
            }
        Debug.LogWarning("Stage5 Animator trigger missing: " + name, this);
    }
}
