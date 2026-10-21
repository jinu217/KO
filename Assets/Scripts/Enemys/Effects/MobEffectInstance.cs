using UnityEngine;
using UnityEngine.SceneManagement;

// Independent lifetime: death effects finish even when the enemy is disabled or pooled.
public sealed class MobEffectInstance : MonoBehaviour
{
    Sprite[] frames;
    SpriteRenderer spriteRenderer;
    bool ownsFrames;
    bool looping;
    float elapsed, fps;
    public static MobEffectInstance Spawn(MobEffects.Effect effect, Vector3 position, float direction,
        float ppu, int layer, int order, Scene scene, bool loop = false)
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
        instance.fps = Mathf.Max(1, effect.fps);
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
        obj.transform.localScale = Vector3.one * Mathf.Max(.01f, effect.scale);
        instance.spriteRenderer = obj.AddComponent<SpriteRenderer>();
        instance.spriteRenderer.sortingLayerID = layer;
        instance.spriteRenderer.sortingOrder = order;
        instance.spriteRenderer.flipX = effect.artFacesRight ? direction < 0 : direction > 0;
        instance.spriteRenderer.flipY = effect.flipY;
        instance.spriteRenderer.sprite = instance.frames[0];
        if (!loop) Destroy(obj, (instance.frames.Length + 1f) / instance.fps);
        return instance;
    }
    void Update()
    {
        if (frames == null || frames.Length == 0) { Destroy(gameObject); return; }
        elapsed += Time.deltaTime;
        int index = Mathf.FloorToInt(elapsed * fps);
        if (looping) index %= frames.Length;
        if (index >= frames.Length)
        { spriteRenderer.sprite = null; gameObject.SetActive(false); Destroy(gameObject); return; }
        spriteRenderer.sprite = frames[index];
    }
    void OnDestroy()
    {
        if (ownsFrames && frames != null) foreach (var frame in frames) if (frame != null) Destroy(frame);
    }
}
