using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EquipmentVisualAdjustment
{
    public string part;
    [Tooltip("애니메이션 자세에 더하는 플레이어 기준 위치")]
    public Vector3 positionOffset;
    public bool overrideSortingOrder;
    public int sortingOrder;
}

// Remove before animation sampling, then apply after sampling; never accumulate offsets.
public sealed class EquipmentVisualState
{
    readonly Transform root;
    readonly SpriteRenderer[] renderers;
    readonly Dictionary<EquipmentType, ItemData> items = new Dictionary<EquipmentType, ItemData>();
    readonly Dictionary<Transform, Vector3> appliedLocal = new Dictionary<Transform, Vector3>();
    readonly Dictionary<Transform, Quaternion> appliedRotation = new Dictionary<Transform, Quaternion>();
    readonly Dictionary<SpriteRenderer, Vector3> baseRootPositions = new Dictionary<SpriteRenderer, Vector3>();
    readonly Dictionary<SpriteRenderer, int> baseOrders = new Dictionary<SpriteRenderer, int>();
    public bool ApplyPositionOffsets = true;
    public IEnumerable<ItemData> Items => items.Values;
    public EquipmentVisualState(Transform root, SpriteRenderer[] renderers)
    {
        this.root = root; this.renderers = renderers;
        foreach (var r in renderers) if (r != null) baseOrders[r] = r.sortingOrder;
    }
    public void SetItem(ItemData item) { if (item != null) items[item.equipType] = item; }
    public void ClearItem(EquipmentType type) { Restore(); items.Remove(type); }
    // Player clips are authored with a zero-degree Sword rest rotation.
    // Add the equipment rest angle after sampling so authored motion deltas survive.
    public static float WeaponRestAngle(InventoryWeaponType type)
    {
        switch (type)
        {
            case InventoryWeaponType.Sword:
                return 70f;
            case InventoryWeaponType.Dagger:
                return 80f;
            case InventoryWeaponType.Spear:
            case InventoryWeaponType.Scythe:
            case InventoryWeaponType.Blunt: return 70f;
            default: return 0f;
        }
    }
    public void Restore()
    {
        foreach (var pair in appliedRotation)
            if (pair.Key != null) pair.Key.localRotation *= Quaternion.Inverse(pair.Value);
        appliedRotation.Clear();
        foreach (var pair in appliedLocal) if (pair.Key != null) pair.Key.localPosition -= pair.Value;
        appliedLocal.Clear();
    }
    public ItemData ItemFor(string part)
    {
        EquipmentType type;
        switch (part)
        {
            case "Head": type = EquipmentType.Head; break;
            case "Body": case "Shoulder_F": case "Shoulder_B": type = EquipmentType.Body; break;
            case "Hand_F": case "Hand_B": type = EquipmentType.Hand; break;
            case "Foot_F": case "Foot_B": type = EquipmentType.Foot; break;
            case "Sword": type = EquipmentType.Weapon; break;
            default: return null;
        }
        return items.TryGetValue(type, out var item) ? item : null;
    }
    public Vector3 BasePosition(SpriteRenderer r) => baseRootPositions.TryGetValue(r, out var p) ? p : root.InverseTransformPoint(r.transform.position);
    public void Apply()
    {
        Restore();
        var positions = new Dictionary<Transform, Vector3>();
        var locals = new Dictionary<Transform, Vector3>();
        foreach (var r in renderers)
        {
            if (r == null) continue;
            var t = r.transform;
            var item = ItemFor(t.name);
            var entry = item != null && item.visualAdjustments != null ? item.visualAdjustments.Find(a => a.part == t.name) : null;
            baseRootPositions[r] = root.InverseTransformPoint(t.position);
            // Unconfigured descendants (especially Sword) must follow the corrected hand.
            if (entry != null && ApplyPositionOffsets)
            {
                positions[t] = t.position + root.TransformVector(entry.positionOffset);
                locals[t] = t.localPosition;
            }
            r.sortingOrder = entry != null && entry.overrideSortingOrder ? entry.sortingOrder : baseOrders[r];
        }
        // GetComponentsInChildren supplies parent-before-child order, keeping offsets independent.
        foreach (var r in renderers) if (r != null && positions.TryGetValue(r.transform, out var position)) r.transform.position = position;
        foreach (var pair in locals) appliedLocal[pair.Key] = pair.Key.localPosition - pair.Value;
        foreach (var r in renderers)
        {
            if (r == null || r.transform.name != "Sword") continue;
            var item = ItemFor("Sword");
            if (item == null) continue;
            var delta = Quaternion.Euler(0, 0, WeaponRestAngle(item.weaponType));
            r.transform.localRotation *= delta;
            appliedRotation[r.transform] = delta;
        }
    }
}
