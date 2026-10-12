using UnityEngine;

// Put outside Editor. Animation sampling invokes this in Scene preview too.
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
public sealed class MidBoss4WeaponLayerPreview : MonoBehaviour
{
    [SerializeField, HideInInspector] private float front;
    private SpriteRenderer target;
    private void OnEnable(){target=GetComponent<SpriteRenderer>();Apply();}
    private void OnDidApplyAnimationProperties(){Apply();}
    private void LateUpdate(){Apply();}
    private void Apply()
    {
        if(target==null)target=GetComponent<SpriteRenderer>();
        if(target!=null)target.sortingLayerName=front>.5f?"Front":"Back";
    }
}
