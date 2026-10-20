using System.Collections;
using UnityEngine;

/// <summary>
/// 재생 가능한 1행 스프라이트 시트로 플레이어의 공격/피격 이펙트를 표시합니다.
/// 시트는 각각 10프레임이며, PNG를 Sprite (Single)로 임포트해 연결합니다.
/// </summary>
public sealed class PlayerCombatEffects : MonoBehaviour
{
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
    [SerializeField] private Vector2 damageOffset = new Vector2(0f, 0.1f);
    [SerializeField] private string sortingLayerOverride = "";
    [SerializeField] private int sortingOrderOffset = 5;

    private SpriteRenderer sortingReference;

    private void Awake()
    {
        sortingReference = GetComponentInChildren<SpriteRenderer>();
    }

    public void PlayAttackEffect(WeaponType weapon, Vector2 position, float facing)
    {
        Sprite sheet;
        bool flipY = false;

        switch (weapon)
        {
            case WeaponType.Scythe:
                sheet = cutMedium;
                break;
            case WeaponType.Blunt:
                sheet = slashBig;
                flipY = true;
                break;
            case WeaponType.Spear:
                // 현재 PlayerController의 찌르기 계열 무기 타입에 sting을 연결합니다.
                sheet = sting;
                break;
            case WeaponType.Sword:
                sheet = slashMedium;
                flipY = true;
                break;
            case WeaponType.Axe:
                sheet = slashBig;
                flipY = true;
                break;
            default:
                sheet = slashMedium;
                flipY = true;
                break;
        }

        Play(sheet, position + new Vector2(Mathf.Sign(facing) * attackOffset.x, attackOffset.y),
            Mathf.Sign(facing), flipY);
    }

    public void PlayDamageEffect(float actualDamage)
    {
        Sprite sheet = actualDamage >= 15f ? impactBig : impactMedium;
        Play(sheet, (Vector2)transform.position + damageOffset, 1f, false);
    }

    private void Play(Sprite sheet, Vector2 position, float facing, bool flipY)
    {
        if (sheet == null)
            return;

        Rect sheetRect;
        try
        {
            sheetRect = sheet.textureRect;
        }
        catch (UnityException)
        {
            Debug.LogWarning($"[PlayerCombatEffects] '{sheet.name}'이 아틀라스에 타이트 메시로 패킹되어 시트 영역을 읽을 수 없습니다.", this);
            return;
        }

        const int frameCount = 10;
        float frameWidth = sheetRect.width / frameCount;
        if (frameWidth < 1f || sheetRect.height < 1f)
        {
            Debug.LogWarning($"[PlayerCombatEffects] '{sheet.name}' 스프라이트 시트 크기가 올바르지 않습니다.", this);
            return;
        }

        GameObject effectObject = new GameObject($"FX_{sheet.name}");
        effectObject.transform.position = position;

        SpriteRenderer renderer = effectObject.AddComponent<SpriteRenderer>();
        renderer.flipY = flipY;
        renderer.flipX = facing < 0f;
        renderer.sortingLayerID = ResolveSortingLayerID();
        renderer.sortingOrder = (sortingReference != null ? sortingReference.sortingOrder : 0) + sortingOrderOffset;

        float ppu = sheet.pixelsPerUnit > 0f ? sheet.pixelsPerUnit : 64f;
        effectObject.transform.localScale = Vector3.one * effectScale;

        StartCoroutine(AnimateSheet(effectObject, renderer, sheet, sheetRect, frameWidth, ppu));
    }

    private int ResolveSortingLayerID()
    {
        if (!string.IsNullOrWhiteSpace(sortingLayerOverride))
        {
            int requested = SortingLayer.NameToID(sortingLayerOverride);
            if (requested != 0 || sortingLayerOverride == "Default")
                return requested;
        }

        return sortingReference != null ? sortingReference.sortingLayerID : SortingLayer.NameToID("Default");
    }

    private IEnumerator AnimateSheet(GameObject effectObject, SpriteRenderer renderer, Sprite source,
        Rect sheetRect, float frameWidth, float pixelsPerUnit)
    {
        const int frameCount = 10;
        float frameDuration = 1f / Mathf.Max(1f, framesPerSecond);
        Sprite runtimeFrame = null;

        for (int i = 0; i < frameCount; i++)
        {
            if (effectObject == null || renderer == null)
            {
                if (runtimeFrame != null)
                    Destroy(runtimeFrame);
                yield break;
            }

            Rect frameRect = new Rect(sheetRect.x + frameWidth * i, sheetRect.y, frameWidth, sheetRect.height);
            Sprite nextFrame = Sprite.Create(source.texture, frameRect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
            renderer.sprite = nextFrame;
            if (runtimeFrame != null)
                Destroy(runtimeFrame);
            runtimeFrame = nextFrame;
            yield return new WaitForSeconds(frameDuration);
        }

        if (effectObject != null)
            Destroy(effectObject);
        if (runtimeFrame != null)
            Destroy(runtimeFrame);
    }
}
