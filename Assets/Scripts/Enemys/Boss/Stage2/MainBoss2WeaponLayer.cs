using UnityEngine;
[ExecuteAlways,DefaultExecutionOrder(800)]
public sealed class MainBoss2WeaponLayer : MonoBehaviour
{
    public SpriteRenderer weapon,hand;
    public float front;
    void OnDidApplyAnimationProperties(){Apply();}
    void LateUpdate(){Apply();}
    void OnDisable(){front=0;Apply();}
    void Apply()
    {
        string layer=front>.5f?"Front":"Back";
        if(weapon!=null){weapon.sortingLayerName=layer;weapon.sortingOrder=-1;}
        if(hand!=null){hand.sortingLayerName=layer;hand.sortingOrder=1;}
    }
}
