#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Isolated prefab sampling; no scene, prefab or clip changes.
public static class BossWeaponFXAudit
{
    public static void Run()
    {
        var log=new StringBuilder();int bosses=0,effects=0,contacts=0,failures=0;
        foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(asset.GetComponent<MidBossController>()==null)continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var boss=root.GetComponent<MidBossController>();var fx=root.GetComponent<BossEffects>();
                var animator=root.GetComponentInChildren<Animator>(true);bosses++;log.AppendLine(path);
                var blade=fx.weaponAnchor!=null?fx.weaponAnchor.GetComponent<SpriteRenderer>():null;
                if(blade!=null && blade.sprite!=null && !(fx.mainBoss && fx.stage==4))
                {
                    Vector3 size=blade.sprite.bounds.size;
                    float length=Mathf.Max(blade.transform.TransformVector(Vector3.right*size.x).magnitude,blade.transform.TransformVector(Vector3.up*size.y).magnitude);
                    foreach(var effect in new[]{fx.attack,fx.upward,fx.horizontal,fx.spin})
                    {
                        var instance=MobEffectInstance.Spawn(effect,Vector3.zero,1,fx.pixelsPerUnit,0,0,root.scene);
                        if(instance==null){failures++;log.AppendLine("FAIL missing weapon effect");continue;}
                        try
                        {
                            var sr=instance.GetComponent<SpriteRenderer>();BossEffects.FitWeaponEffect(sr,blade.transform);
                            float cell=Mathf.Max(sr.sprite.rect.width,sr.sprite.rect.height)/sr.sprite.pixelsPerUnit;
                            float ratio=cell*sr.transform.lossyScale.x/length;
                            if(Mathf.Abs(ratio-1.1f)>.002f){failures++;log.AppendLine("FAIL FX size ratio "+ratio);}
                            effects++;
                        }
                        finally{UnityEngine.Object.DestroyImmediate(instance.gameObject);}
                    }
                    log.AppendLine("weapon="+blade.name+" world length="+length.ToString("0.###")+" FX cell ratio=1.10");
                }
                var skills=new[]{boss.data.basicAttack}.Concat(boss.data.skills).Where(s=>s!=null && s.active!=null);
                foreach(var skill in skills)foreach(var step in skill.active.attackSteps)
                {
                    if(step==null || step.damage<=0)continue;
                    if(fx.TryAttackBounds(step,out var bounds))
                    {
                        contacts++;
                        if(bounds.size.x<=0 || bounds.size.y<=0 || float.IsNaN(bounds.center.x)){failures++;log.AppendLine("FAIL contact "+step.stepName);}
                    }
                }
                var layer=animator.GetComponent<MainBoss2WeaponLayer>();
                if(layer!=null)foreach(var clip in animator.runtimeAnimatorController.animationClips.Distinct())
                {
                    var transitions=new StringBuilder();bool previous=false;int frontFrames=0;
                    for(int i=0;i<=Mathf.CeilToInt(clip.length*24);i++)
                    {
                        float t=Mathf.Min(clip.length,i/24f);clip.SampleAnimation(animator.gameObject,t);
                        layer.SendMessage("OnDidApplyAnimationProperties");bool front=layer.front>.5f;
                        if(i==0 || previous!=front)transitions.Append(t.ToString("0.###")+":"+(front?"Front":"Back")+" ");
                        previous=front;if(front)frontFrames++;
                        if(layer.weapon.sortingLayerName!=(front?"Front":"Back") || layer.hand.sortingLayerName!=layer.weapon.sortingLayerName)failures++;
                    }
                    log.AppendLine(clip.name+" "+transitions+" front samples="+frontFrames);
                    string n=clip.name.ToLowerInvariant();if((n.Contains("idle")||n.Contains("run")||n.Contains("walk")) && frontFrames!=0)failures++;
                }
            }
            catch(Exception e){failures++;log.AppendLine("FAIL "+e);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        log.Insert(0,$"bosses={bosses} effects={effects} damage contacts={contacts} failures={failures}\n");
        File.WriteAllText("Library/BossWeaponFXAudit.txt",log.ToString());
    }
}
#endif
