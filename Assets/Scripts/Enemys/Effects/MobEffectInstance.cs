using UnityEngine;
using UnityEngine.SceneManagement;

// Independent lifetime: death effects finish even when the enemy is disabled or pooled.
public sealed class MobEffectInstance : MonoBehaviour
{
#if UNITY_EDITOR
    public static MobEffectInstance LastPreviewSpawn;
#endif
    Sprite[] frames;
    SpriteRenderer spriteRenderer;
    bool ownsFrames;
    bool looping;
    bool manualFrame;
    float elapsed, fps;
    public float SlashProgress => frames!=null && frames.Length>0?Mathf.Clamp01(elapsed*fps/frames.Length):0;
    bool fading;
    float fadeAge, fadeHold, fadeDuration, fadeAlpha;
    public static MobEffectInstance Spawn(MobEffects.Effect effect, Vector3 position, float direction,
        float ppu, int layer, int order, Scene scene, bool loop = false, float scaleMultiplier = 1f, float speedMultiplier = 1f)
    {
        bool supplied = effect.frames != null && effect.frames.Length > 0;
        if (!supplied && (effect.sheet == null || effect.columns < 1 || effect.sheet.width % effect.columns != 0))
        { Debug.LogWarning("Mob FX: 시트 너비와 Columns를 확인하세요."); return null; }
        if (supplied) foreach (var frame in effect.frames) if (frame == null) return null;
        var obj = new GameObject("Mob FX");
        obj.hideFlags = HideFlags.DontSave;
        if (scene.IsValid() && scene.isLoaded) SceneManager.MoveGameObjectToScene(obj, scene);
        var instance = obj.AddComponent<MobEffectInstance>();
        instance.looping = loop;
        instance.fps = Mathf.Max(1, effect.fps) * Mathf.Max(.01f, speedMultiplier);
        if (supplied) instance.frames = effect.frames;
        else
        {
            instance.ownsFrames = true;
            instance.frames = new Sprite[effect.columns];
            int width = effect.sheet.width / effect.columns;
            for (int i = 0; i < instance.frames.Length; i++)
                instance.frames[i] = Sprite.Create(effect.sheet, new Rect(i * width, 0, width, effect.sheet.height),
                    new Vector2(.5f, .5f), Mathf.Max(1, ppu), 0, SpriteMeshType.FullRect);
        }
        obj.transform.position = position;
        obj.transform.localScale = Vector3.one * Mathf.Max(.01f, effect.scale) * Mathf.Max(.01f, scaleMultiplier);
        instance.spriteRenderer = obj.AddComponent<SpriteRenderer>();
        instance.spriteRenderer.sortingLayerID = layer;
        instance.spriteRenderer.sortingOrder = order;
        instance.spriteRenderer.flipX = effect.artFacesRight ? direction < 0 : direction > 0;
        instance.spriteRenderer.flipY = effect.flipY;
        instance.spriteRenderer.sprite = instance.frames[0];
#if UNITY_EDITOR
        if (!Application.isPlaying) LastPreviewSpawn = instance;
#endif
        // Update owns the lifetime so stop-fade can finish past the last animation frame.
        return instance;
    }
    BossWeaponSlashAttachment weaponSlash;
    public void FollowWeaponSlash(Transform weapon, BossSlashSweepVisual.Plane plane=BossSlashSweepVisual.Plane.Down)
    {
        weaponSlash=GetComponent<BossWeaponSlashAttachment>() ?? gameObject.AddComponent<BossWeaponSlashAttachment>();
        weaponSlash.Configure(spriteRenderer,weapon,plane);
    }
    Transform followAnchor;
    Vector3 anchorPoint;
    float? groundHeight;
    public void AlignBottomToGround(float height){groundHeight=height;AlignToAnchor();}
    public void FollowAnchor(Transform anchor, Vector3 worldPoint)
    {
        followAnchor = anchor; anchorPoint = anchor.InverseTransformPoint(worldPoint);
        AlignToAnchor();
    }
    void LateUpdate() { AlignToAnchor(); }
    void AlignToAnchor()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;
        if(followAnchor!=null)transform.position += followAnchor.TransformPoint(anchorPoint) - BossEffects.VisibleBounds(spriteRenderer).center;
        if(groundHeight.HasValue)transform.position+=Vector3.up*(groundHeight.Value-BossEffects.VisibleBounds(spriteRenderer).min.y);
        if(weaponSlash!=null)weaponSlash.Align();
    }
    public void SetFacing(float direction, bool artFacesRight)
    {
        if (spriteRenderer != null && !fading)
            spriteRenderer.flipX = artFacesRight ? direction < 0 : direction > 0;
    }
    public void StopAndFade(float hold = .06f, float duration = .14f)
    {
        if (fading || spriteRenderer == null) return;
        followAnchor = null;
        fading = true;
        fadeAge = 0;
        fadeHold = Mathf.Max(0, hold);
        fadeDuration = Mathf.Max(.01f, duration);
        fadeAlpha = spriteRenderer.color.a;
        // Freeze both current sprite and world position; emitter may immediately create a new trail.
        transform.SetParent(null, true);
    }
    public void SetManualFrame(int index)
    {
        manualFrame = true;
        spriteRenderer.enabled = index >= 0 && index < frames.Length;
        if(spriteRenderer.enabled) spriteRenderer.sprite = frames[index];
    }
    void Update()
    {
        if(manualFrame) return;
        if (frames == null || frames.Length == 0) { Destroy(gameObject); return; }
        if (fading)
        {
            fadeAge += Time.deltaTime;
            Color color = spriteRenderer.color;
            color.a = fadeAlpha * (1f - Mathf.Clamp01((fadeAge - fadeHold) / fadeDuration));
            spriteRenderer.color = color;
            if (fadeAge >= fadeHold + fadeDuration) Destroy(gameObject);
            return;
        }
        elapsed += Time.deltaTime;
        int index = Mathf.FloorToInt(elapsed * fps);
        if (looping) index %= frames.Length;
        if (index >= frames.Length)
        { spriteRenderer.sprite = null; gameObject.SetActive(false); Destroy(gameObject); return; }
        spriteRenderer.sprite = frames[index];
    }
#if UNITY_EDITOR
    public void PreviewSample(float age)
    {
        if (Application.isPlaying || spriteRenderer == null) return;
        gameObject.hideFlags = HideFlags.HideAndDontSave;
        elapsed=Mathf.Max(0,age);
        int index = Mathf.FloorToInt(Mathf.Max(0, age) * fps);
        if (looping) index %= frames.Length;
        spriteRenderer.enabled = age >= 0 && index < frames.Length;
        if (spriteRenderer.enabled) spriteRenderer.sprite = frames[index];
        AlignToAnchor();
    }
#endif
    void OnDestroy()
    {
        if (ownsFrames && frames != null) foreach (var frame in frames) if (frame != null) { if (Application.isPlaying) Destroy(frame); else DestroyImmediate(frame); }
    }
}
