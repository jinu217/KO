using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class MobEffects : MonoBehaviour
{
    [System.Serializable]
    public class Effect
    {
        public Texture2D sheet;
        public Sprite[] frames;
        [Min(1)] public int columns = 10;
        [Min(1)] public float fps = 24;
        [Min(.01f)] public float scale = 1;
        public Vector2 offset;
        public bool artFacesRight = true;
        public bool flipY;
    }
    public Effect attack = new Effect { flipY = true };
    public Effect shot = new Effect();
    public Effect hit = new Effect();
    public Effect death = new Effect { columns = 24, scale = 1.3f };
    public Effect notice = new Effect { columns = 1, fps = 20, scale = 1.5f, artFacesRight = false };
    public Effect chase = new Effect { columns = 8, fps = 16, scale = 1.5f, offset = new Vector2(-.3f, .05f), artFacesRight = false };
    [Min(1)] public float pixelsPerUnit = 64;
    [Min(.05f)] public float chaseInterval = .18f;
    public string sortingLayerOverride = "";
    public int sortingOrderOffset = 5;
    EnemyController enemy;
    Collider2D body;
    float nextDust;
    MobEffectInstance dust;

    void Awake() { enemy = GetComponent<EnemyController>(); body = GetComponent<Collider2D>(); }
    void OnEnable() { nextDust = 0; }
    void OnDisable() { if (dust != null) Destroy(dust.gameObject); dust = null; }
    Vector3 Center() { return body != null ? body.bounds.center : transform.position; }
    Vector3 Feet() { Vector3 p = Center(); if (body != null) p.y = body.bounds.min.y; return p; }
    void Update()
    {
        if (enemy == null || !enemy.FXChasingOnGround)
        { if (dust != null) Destroy(dust.gameObject); dust = null; return; }
        if (Time.time < nextDust || dust != null) return;
        nextDust = Time.time + chaseInterval;
        dust = Play(chase, Feet(), enemy.FXDirection);
    }
    public void PlayAttack(Vector3 position, float direction, bool ranged = false)
    { Play(ranged ? shot : attack, position, direction); }
    public void PlayHit(Vector2 source) { Play(hit, Center(), source.x < transform.position.x ? -1 : 1); }
    public void PlayDeath() { Play(death, Center(), enemy != null ? enemy.FXDirection : 1); }
    public void PlayNotice(float direction) { Play(notice, Feet(), direction); }
    MobEffectInstance Play(Effect effect, Vector3 position, float direction)
    {
        if (!isActiveAndEnabled || effect == null) return null;
        if (effect.sheet == null && (effect.frames == null || effect.frames.Length == 0)) return null;
        int layer = 0, order = 0, best = int.MinValue;
        foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            int value = SortingLayer.GetLayerValueFromID(renderer.sortingLayerID);
            if (value > best || (value == best && renderer.sortingOrder > order))
            { best = value; layer = renderer.sortingLayerID; order = renderer.sortingOrder; }
        }
        // Detached effects must also render above the outer sorting group.
        foreach (var group in GetComponentsInParent<SortingGroup>(true))
        {
            int value = SortingLayer.GetLayerValueFromID(group.sortingLayerID);
            if (value > best || (value == best && group.sortingOrder > order))
            { best = value; layer = group.sortingLayerID; order = group.sortingOrder; }
        }
        if (!string.IsNullOrEmpty(sortingLayerOverride)) layer = SortingLayer.NameToID(sortingLayerOverride);
        position += new Vector3(effect.offset.x * Mathf.Sign(direction), effect.offset.y, 0);
        return MobEffectInstance.Spawn(effect, position, direction, pixelsPerUnit, layer,
            order + sortingOrderOffset, gameObject.scene);
    }
}
