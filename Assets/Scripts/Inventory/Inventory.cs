using System;
using System.Collections.Generic;
using UnityEngine;

//============== 인벤토리 ==============

public class Inventory : MonoBehaviour
{
    [Header("용량")]
    [Tooltip("인벤토리 UI가 연결되기 전에 사용할 기본 슬롯 개수")]
    [SerializeField, Min(1)] private int capacity = 9;

    public EquipmentManager equipmentManager;

    public List<InventorySlot> items = new List<InventorySlot>();

    public Action onInventoryChanged;

    public int Capacity => capacity;
    public int Count => items != null ? items.Count : 0;
    public bool HasSpace => Count < capacity;

    private bool initializedFromSave = false;

    //============== 기본 설정 ==============

    public void SetCapacity(int slotCount)
    {
        if (slotCount > 0)
            capacity = slotCount;
    }

    //============== 인벤토리 초기화 / 복원 ==============

    private void Start()
    {
        InitializeInventory();
    }

    private void InitializeInventory()
    {
        if (initializedFromSave)
            return;

        initializedFromSave = true;

        if (SaveData.Instance == null)
        {
            Debug.LogWarning("[Inventory] SaveData가 없어 인벤토리를 빈 상태로 시작합니다.");
            items.Clear();
            onInventoryChanged?.Invoke();
            return;
        }

        // 이미 진행 중인 런이면 저장된 인벤토리 복원
        if (SaveData.Instance.run.initialized)
        {
            RestoreFromSaveData();
            return;
        }

        // 새로운 런이면 인벤토리는 빈 상태로 시작
        items.Clear();

        onInventoryChanged?.Invoke();
    }

    //============== SaveData → Inventory 복원 ==============

    public void RestoreFromSaveData()
    {
        if (SaveData.Instance == null)
            return;

        List<ItemData> restoredItems = new List<ItemData>();

        if (SaveData.Instance.run.inventoryItemIds != null)
        {
            foreach (int itemId in SaveData.Instance.run.inventoryItemIds)
            {
                ItemData item = SaveData.Instance.GetItemById(itemId);

                if (item != null)
                {
                    restoredItems.Add(item);
                }
                else
                {
                    Debug.LogWarning(
                        $"[Inventory] 아이템 ID {itemId}를 ItemDatabase에서 찾을 수 없습니다."
                    );
                }
            }
        }

        ReplaceItems(restoredItems);

        Debug.Log(
            $"[Inventory] SaveData에서 인벤토리 복원 완료 : {restoredItems.Count}개"
        );
    }

    //============== Inventory → SaveData 저장 ==============

    public void SyncToSaveData()
    {
        if (SaveData.Instance == null)
            return;

        if (SaveData.Instance.run.inventoryItemIds == null)
            SaveData.Instance.run.inventoryItemIds = new List<int>();

        SaveData.Instance.run.inventoryItemIds.Clear();

        if (items == null)
            return;

        foreach (InventorySlot slot in items)
        {
            if (slot == null || slot.item == null)
                continue;

            SaveData.Instance.run.inventoryItemIds.Add(slot.item.id);
        }

        Debug.Log(
            $"[Inventory] SaveData에 인벤토리 저장 : {SaveData.Instance.run.inventoryItemIds.Count}개"
        );
    }

    //============== 아이템 추가 ==============

    public void AddItem(ItemData item)
    {
        if (item == null)
            return;

        if (!HasSpace)
        {
            Debug.Log("[Inventory] 인벤토리가 가득 찼습니다.");
            return;
        }

        items.Add(new InventorySlot(item, 1));

        SyncToSaveData();

        onInventoryChanged?.Invoke();
    }

    //============== 아이템 목록 교체 ==============

    public void ReplaceItems(IEnumerable<ItemData> restoredItems)
    {
        items.Clear();

        if (restoredItems != null)
        {
            foreach (ItemData item in restoredItems)
            {
                if (item == null)
                    continue;

                if (!HasSpace)
                    break;

                items.Add(new InventorySlot(item, 1));
            }
        }

        onInventoryChanged?.Invoke();
    }

    //============== 아이템 제거 ==============

    public void RemoveItem(int index)
    {
        if (index < 0 || index >= items.Count)
            return;

        items.RemoveAt(index);

        SyncToSaveData();

        onInventoryChanged?.Invoke();
    }

    //============== 장비 장착 ==============

    public bool EquipItem(ItemData item)
    {
        if (item == null)
            return false;

        if (item.itemType != ItemType.Equipment)
            return false;

        if (equipmentManager == null)
        {
            Debug.LogError("[Inventory] EquipmentManager가 연결되지 않았습니다.");
            return false;
        }

        if (!equipmentManager.Equip(item))
            return false;

        onInventoryChanged?.Invoke();
        return true;
    }
}
