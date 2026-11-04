using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// 10칸짜리 가로 PNG 시트를 전체 프레임 사각형으로 재생합니다.
/// 활성 이펙트와 런타임 스프라이트는 이 컴포넌트가 관리합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerCombatEffects : MonoBehaviour
{
    private const int FrameCount = 10;

    [Header("Attack sheets (10 frames each)")]
    [SerializeField] private Sprite cutMedium;
    [SerializeField] private Sprite slashBig;
    [SerializeField] private Sprite slashMedium;
    [SerializeField] private Sprite sting;

    [Header("Damage sheets (10 frames each)")]
    [SerializeField] private Sprite impactBig;
    [SerializeField] private Sprite impactMedium;

    [Header("Playback")]
    [SerializeField, Min(1f)] private float framesPerSecond = 24f;
    [SerializeField, Min(0.01f)] private float effectScale = 1f;
    [SerializeField] private Vector2 attackOffset = new Vector2(0f, 0.15f);
    [SerializeField, Min(0f)] private float daggerEffectPullback = 0.8f;
    [SerializeField] private Vector2 damageOffset = new Vector2(0f, 0.1f);
    [SerializeField] private string sortingLayerOverride = "";
    [SerializeField] private int sortingOrderOffset = 5;

    private sealed class ActiveEffect
    {
        public GameObject gameObject;
        public SpriteRenderer renderer;
        public Sprite[] frames;
        public float elapsed;
        public float fps;
    }

    private readonly Dictionary<Sprite, Sprite[]> frameCache = new Dictionary<Sprite, Sprite[]>();
    private readonly List<ActiveEffect> activeEffects = new List<ActiveEffect>();

    public void PlayAttackEffect(WeaponType weapon, Vector2 position, float facing, float? daggerWeaponHeight = null)
    {
        Sprite sheet;
        bool flipY = false;
        switch (weapon)
        {
            case WeaponType.Dagger:
                sheet = sting;
                break;
            case WeaponType.Scythe:
                sheet = cutMedium;
                break;
            case WeaponType.Sword:
                sheet = slashBig;
                flipY = true;
                break;
            case WeaponType.Blunt:
            case WeaponType.Axe:
                sheet = slashBig;
                flipY = true;
                break;
            default:
                sheet = slashMedium;
                flipY = true;
                break;
        }

        float direction = facing < 0f ? -1f : 1f;
        // The long dagger hitbox center is farther forward than the visible thrust.
        // Apply this visual correction here so runtime and scene preview stay identical.
        float horizontalOffset = attackOffset.x - (weapon == WeaponType.Dagger ? daggerEffectPullback : 0f);
        Vector2 effectPosition = position + new Vector2(direction * horizontalOffset, attackOffset.y);
        if (weapon == WeaponType.Dagger)
        {
            var anchor = DaggerEffectAnchor;
            if (daggerWeaponHeight.HasValue || anchor != null)
                effectPosition.y = (daggerWeaponHeight ?? anchor.position.y) - 0.05f;
        }
        Play(sheet, effectPosition, direction, flipY, 1.5f, weapon == WeaponType.Scythe ? 1.5f : 1f);
    }

    public Transform DaggerEffectAnchor
    {
        get
        {
            foreach (var node in GetComponentsInChildren<Transform>(true))
                if (node.name == "Sword" && node.parent != null && node.parent.name == "Hand_B") return node;
            return null;
        }
    }

    public void PlayDamageEffect(float actualDamage)
    {
        Sprite sheet = actualDamage >= 15f ? impactBig : impactMedium;
        Play(sheet, (Vector2)transform.position + damageOffset, 1f, false);
    }

    private void Play(Sprite sheet, Vector2 position, float facing, bool flipY, float speedMultiplier = 1f, float sizeMultiplier = 1f)
    {
        if (!isActiveAndEnabled || sheet == null)
            return;

        Sprite[] frames = GetFrames(sheet);
        if (frames == null)
            return;

        float fps = Mathf.Max(1f, framesPerSecond) * speedMultiplier;
        var effectObject = new GameObject($"FX_{sheet.name}");
        effectObject.hideFlags = HideFlags.DontSave;
        effectObject.SetActive(false);
        // Additive로 열린 다른 씬에 이펙트가 남지 않도록 Player의 씬에 둡니다.
        if (gameObject.scene.IsValid() && gameObject.scene.isLoaded)
            SceneManager.MoveGameObjectToScene(effectObject, gameObject.scene);
        effectObject.transform.position = new Vector3(position.x, position.y, transform.position.z);
        effectObject.transform.localScale = Vector3.one * Mathf.Max(0.01f, effectScale) * 1.5f * sizeMultiplier;

        SpriteRenderer renderer = effectObject.AddComponent<SpriteRenderer>();
        renderer.sprite = frames[0];
        renderer.flipY = flipY;
        renderer.flipX = facing < 0f;
        ResolveSorting(out int layer, out int order);
        renderer.sortingLayerID = layer;
        renderer.sortingOrder = order;

        activeEffects.Add(new ActiveEffect
        {
            gameObject = effectObject,
            renderer = renderer,
            frames = frames,
            fps = fps
        });
        effectObject.SetActive(true);
        // Update가 중단되더라도 별도 GameObject가 영구히 남지 않도록 수명을 예약합니다.
        if (Application.isPlaying) Destroy(effectObject, (FrameCount + 1f) / fps);
    }

    private Sprite[] GetFrames(Sprite sheet)
    {
        if (frameCache.TryGetValue(sheet, out Sprite[] cached))
            return cached;

        if (sheet.packed)
        {
            Debug.LogWarning($"[PlayerCombatEffects] '{sheet.name}' 시트 PNG를 Sprite Atlas에서 제외하세요. 패킹된 시트는 잘린 좌표로 재생하지 않습니다.", this);
            return null;
        }

        // textureRect의 투명 여백 잘림에 영향을 받지 않는 원본 Sprite 영역을 사용합니다.
        Rect rect = sheet.rect;
        int width = Mathf.RoundToInt(rect.width);
        int height = Mathf.RoundToInt(rect.height);
        if (sheet.texture == null || width <= 0 || height <= 0 || width % FrameCount != 0)
        {
            Debug.LogWarning($"[PlayerCombatEffects] '{sheet.name}'은 Sprite (Single)인 10칸 가로 시트여야 합니다.", this);
            return null;
        }

        int frameWidth = width / FrameCount;
        float ppu = sheet.pixelsPerUnit > 0f ? sheet.pixelsPerUnit : 64f;
        var frames = new Sprite[FrameCount];
        for (int i = 0; i < FrameCount; i++)
        {
            Rect frameRect = new Rect(rect.x + frameWidth * i, rect.y, frameWidth, height);
            // Tight 메시의 윤곽 대신 프레임 사각형 전체를 렌더링합니다.
            frames[i] = Sprite.Create(sheet.texture, frameRect, new Vector2(0.5f, 0.5f),
                ppu, 0, SpriteMeshType.FullRect);
            frames[i].name = $"{sheet.name}_{i:00}";
            frames[i].hideFlags = HideFlags.DontSave;
        }
        frameCache.Add(sheet, frames);
        return frames;
    }

#if UNITY_EDITOR
    // Runs the actual runtime spawn path; the editor only supplies event age.
    public void PreviewAttack(WeaponType weapon, Vector2 position, float facing, float age, float? daggerWeaponHeight = null)
    {
        if (Application.isPlaying) return;
        ClearActiveEffects();
        if (age < 0) return;
        PlayAttackEffect(weapon, position, facing, daggerWeaponHeight);
        foreach (var effect in activeEffects)
        {
            int frame = Mathf.FloorToInt(age * effect.fps);
            effect.renderer.sprite = frame < effect.frames.Length ? effect.frames[frame] : null;
        }
    }
    public void ClearPreview()
    {
        if (Application.isPlaying) return;
        ClearActiveEffects();
        foreach (var list in frameCache.Values)
            foreach (var frame in list) if (frame != null) DestroyImmediate(frame);
        frameCache.Clear();
    }
#endif
    private void Update()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveEffect effect = activeEffects[i];
            if (effect.gameObject == null || effect.renderer == null)
            {
                RemoveEffect(i);
                continue;
            }

            effect.elapsed += Time.deltaTime;
            int frame = Mathf.FloorToInt(effect.elapsed * effect.fps);
            if (frame >= effect.frames.Length)
                RemoveEffect(i);
            else
                effect.renderer.sprite = effect.frames[frame];
        }
    }

    private void ResolveSorting(out int layer, out int order)
    {
        layer = SortingLayer.NameToID("Default");
        order = 0;
        int highestLayer = int.MinValue;
        bool found = false;
        foreach (SpriteRenderer candidate in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (candidate.sprite == null)
                continue;
            int value = SortingLayer.GetLayerValueFromID(candidate.sortingLayerID);
            if (!found || value > highestLayer || (value == highestLayer && candidate.sortingOrder > order))
            {
                found = true;
                highestLayer = value;
                layer = candidate.sortingLayerID;
                order = candidate.sortingOrder;
            }
        }
        // SortingGroup 바깥의 이펙트도 플레이어보다 위에서 그려지도록 그룹 순서를 반영합니다.
        foreach (SortingGroup group in GetComponentsInParent<SortingGroup>(true))
        {
            layer = group.sortingLayerID;
            order = group.sortingOrder;
        }
        if (!string.IsNullOrWhiteSpace(sortingLayerOverride))
        {
            foreach (SortingLayer candidate in SortingLayer.layers)
            {
                if (candidate.name == sortingLayerOverride)
                {
                    layer = candidate.id;
                    break;
                }
            }
        }
        order = Mathf.Clamp(order + sortingOrderOffset, -32768, 32767);
    }

    private void RemoveEffect(int index)
    {
        ActiveEffect effect = activeEffects[index];
        if (effect.renderer != null)
            effect.renderer.sprite = null;
        if (effect.gameObject != null)
        {
            effect.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(effect.gameObject);
            else DestroyImmediate(effect.gameObject);
        }
        activeEffects.RemoveAt(index);
    }

    private void OnDisable()
    {
        ClearActiveEffects();
    }

    private void OnDestroy()
    {
        ClearActiveEffects();
        foreach (Sprite[] frames in frameCache.Values)
            foreach (Sprite frame in frames)
                if (frame != null)
                    if (Application.isPlaying) Destroy(frame); else DestroyImmediate(frame);
        frameCache.Clear();
    }

    private void ClearActiveEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
            RemoveEffect(i);
    }
}
