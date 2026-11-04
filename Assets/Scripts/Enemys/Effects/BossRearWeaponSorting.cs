using UnityEngine;

// The clip stores small flags, never a SortingLayer ID rounded to a float.
[ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(1100)]
public sealed class BossRearWeaponSorting : MonoBehaviour
{
    [HideInInspector] public float active, front;
    [SerializeField] SpriteRenderer hand, weapon;
    [SerializeField] bool followExistingWeapon;
    [SerializeField, HideInInspector] int handLayer, weaponLayer, handOrder, weaponOrder;
#if UNITY_EDITOR
    bool previewing;
#endif
    public void Configure(SpriteRenderer rearHand, SpriteRenderer blade, bool follow)
    {
        hand=rearHand; weapon=blade; followExistingWeapon=follow;
        handLayer=hand.sortingLayerID; handOrder=hand.sortingOrder;
        if(weapon!=null) { weaponLayer=weapon.sortingLayerID; weaponOrder=weapon.sortingOrder; }
    }
    void OnEnable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= Preview;
        UnityEditor.EditorApplication.update += Preview;
#endif
    }
    void OnDidApplyAnimationProperties() { Apply(); }
    void LateUpdate() { Apply(); }
    void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= Preview;
#endif
        active=front=0; Apply();
    }
    public void Apply()
    {
        bool action=active>.5f;
        int layer=followExistingWeapon && weapon!=null ? weapon.sortingLayerID : SortingLayer.NameToID(front>.5f?"Front":"Back");
        int bladeOrder=followExistingWeapon && weapon!=null ? weapon.sortingOrder : Mathf.Min(weaponOrder,handOrder-1);
        Set(hand, action?layer:handLayer, action && weapon!=null ? Mathf.Max(handOrder,bladeOrder+1):handOrder);
        if(!followExistingWeapon) Set(weapon,action?layer:weaponLayer,action?bladeOrder:weaponOrder);
    }
    void Set(SpriteRenderer renderer,int layer,int order)
    {
        if(renderer==null) return;
#if UNITY_EDITOR
        if(!Application.isPlaying && UnityEditor.AnimationMode.InAnimationMode())
        {
            Record(renderer,"m_SortingLayerID",renderer.sortingLayerID.ToString());
            Record(renderer,"m_SortingOrder",renderer.sortingOrder.ToString());
        }
#endif
        renderer.sortingLayerID=layer; renderer.sortingOrder=order;
    }
#if UNITY_EDITOR
    void Record(SpriteRenderer renderer,string property,string value)
    {
        UnityEditor.AnimationMode.AddPropertyModification(
            UnityEditor.EditorCurveBinding.DiscreteCurve(UnityEditor.AnimationUtility.CalculateTransformPath(renderer.transform,transform),typeof(SpriteRenderer),property),
            new UnityEditor.PropertyModification { target=renderer,propertyPath=property,value=value },true);
    }
    void Preview()
    {
        if(this==null || !isActiveAndEnabled || Application.isPlaying) return;
        if(UnityEditor.AnimationMode.InAnimationMode()) { previewing=true; Apply(); }
        else if(previewing) { previewing=false; active=front=0; Apply(); }
    }
#endif
}
