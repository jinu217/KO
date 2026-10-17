using UnityEngine;
using UnityEngine.SceneManagement;

public class EquipmentManager : MonoBehaviour
{
    public Inventory inventory;

    public EquipmentSlot weaponSlot;
    public EquipmentSlot handSlot;
    public EquipmentSlot headSlot;
    public EquipmentSlot bodySlot;
    public EquipmentSlot footSlot;

    [Header("새 런 시작 장비")]
    public ItemData[] startEquipItems;

    public CharacterPreviewUI previewUI;
    public PlayerStats stats = new PlayerStats();
    public System.Action onStatsChanged;

    [SerializeField] private PlayerController playerController;

    // 플레이어 외형 장비 연결
    public PlayerAppearance playerAppearance;

    private const string FirstTutorialScene = "Tutorial_0";
    private bool IsFirstTutorialScene => SceneManager.GetActiveScene().name == FirstTutorialScene;


    //============== 초기화 ==============

    void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
    }


    void Start()
    {
        if (SaveData.Instance == null)
        {
            Debug.LogError("[EquipmentManager] SaveData.Instance가 NULL입니다.");
            EquipStartItems();
            return;
        }

        RunData run = SaveData.Instance.run;

        Debug.Log(
            $"[EquipmentManager] Start / " +
            $"initialized={run.initialized}, " +
            $"weapon={run.weaponItemId}, " +
            $"hand={run.handItemId}, " +
            $"head={run.headItemId}, " +
            $"body={run.bodyItemId}, " +
            $"foot={run.footItemId}"
        );

        if (run.initialized)
        {
            Debug.Log("[EquipmentManager] 저장된 장비 복원");
            RestoreEquipmentFromRunData();
            GiveStartWeaponIfNeeded(run);   // 추가
            return;
        }

        //============== 새 런 초기 장비 처리 ==============

        Debug.Log("[EquipmentManager] 초기 장비 장착");
        EquipStartItems();

        // 튜토리얼_0이 아닌 씬에서 시작했다면 무기도 이미 지급된 상태  // 추가
        if (!IsFirstTutorialScene)
            run.startWeaponGiven = true;

        SaveCurrentEquipmentToRunData();

        run.initialized = true;

        Debug.Log(
            $"[EquipmentManager] 초기 장비 저장 완료 / " +
            $"weapon={run.weaponItemId}, " +
            $"hand={run.handItemId}, " +
            $"head={run.headItemId}, " +
            $"body={run.bodyItemId}, " +
            $"foot={run.footItemId}"
        );
    }

    //============== 새 런 초기 장비 ==============

    private void EquipStartItems()
    {
        if (startEquipItems == null)
        {
            playerAppearance?.ClearWeapon("Start Equip Items 배열이 없음");
            return;
        }

        bool isTutorialScene = IsFirstTutorialScene;

        foreach (ItemData item in startEquipItems)
        {
            if (item == null)
                continue;

            //============== 튜토리얼_0 초기 무기 미장착 ==============

            // 튜토리얼_0에서는 무기만 장착하지 않음
            // 손 / 머리 / 몸 / 발 장비는 정상적으로 장착됨
            if (isTutorialScene &&
                item.equipType == EquipmentType.Weapon)
            {
                Debug.Log(
                    $"[EquipmentManager] Tutorial_0 → 초기 무기 미장착: {item.itemName}"
                );

                continue;
            }

            Equip(item);
        }

        if (GetItem(weaponSlot) == null)
            playerAppearance?.ClearWeapon(isTutorialScene
                ? "Tutorial_0 시작 시 무기 미장착"
                : "시작 장비 적용 후 Weapon Slot에 장착된 아이템 없음");
    }

    //============== 시작 무기 뒤늦게 지급 (튜토리얼_0 이후 씬) ==============
    private void GiveStartWeaponIfNeeded(RunData run)
    {
        // 튜토리얼_0에서는 지급하지 않음
        if (IsFirstTutorialScene)
            return;

        // 이미 한 번 지급했으면 다시 주지 않음
        if (run.startWeaponGiven)
            return;

        run.startWeaponGiven = true;

        // 이미 무기를 장착 중이면 그대로 둠
        if (GetItem(weaponSlot) != null)
            return;

        if (startEquipItems == null)
            return;

        foreach (ItemData item in startEquipItems)
        {
            if (item != null && item.equipType == EquipmentType.Weapon)
            {
                Equip(item);   // 외형, 스탯, RunData 저장까지 처리됨
                break;
            }
        }
    }

    //============== RunData → 현재 장비 복원 ==============
    private void RestoreEquipmentFromRunData()
    {
        RunData run = SaveData.Instance.run;

        Debug.Log(
            $"[EquipmentManager] 장비 복원 시작 / " +
            $"weaponID={run.weaponItemId}, " +
            $"handID={run.handItemId}, " +
            $"headID={run.headItemId}, " +
            $"bodyID={run.bodyItemId}, " +
            $"footID={run.footItemId}"
        );

        ItemData weapon = SaveData.Instance.GetItemById(run.weaponItemId);
        ItemData hand = SaveData.Instance.GetItemById(run.handItemId);
        ItemData head = SaveData.Instance.GetItemById(run.headItemId);
        ItemData body = SaveData.Instance.GetItemById(run.bodyItemId);
        ItemData foot = SaveData.Instance.GetItemById(run.footItemId);

        Debug.Log(
            $"[EquipmentManager] 복원할 장비 / " +
            $"weapon={GetItemName(weapon)}, " +
            $"hand={GetItemName(hand)}, " +
            $"head={GetItemName(head)}, " +
            $"body={GetItemName(body)}, " +
            $"foot={GetItemName(foot)}"
        );

        RestoreEquipment(
            weapon,
            hand,
            head,
            body,
            foot
        );
    }

    //============== 디버그용 아이템 이름 ==============
    private string GetItemName(ItemData item)
    {
        return item != null
            ? $"{item.itemName}(ID={item.id})"
            : "NULL";
    }


    //============== 현재 장비 → RunData 저장 ==============

    private void SaveCurrentEquipmentToRunData()
    {
        if (SaveData.Instance == null)
            return;

        RunData run = SaveData.Instance.run;

        ItemData weapon = GetItem(weaponSlot);
        ItemData hand = GetItem(handSlot);
        ItemData head = GetItem(headSlot);
        ItemData body = GetItem(bodySlot);
        ItemData foot = GetItem(footSlot);

        run.weaponItemId = GetItemId(weapon);
        run.handItemId = GetItemId(hand);
        run.headItemId = GetItemId(head);
        run.bodyItemId = GetItemId(body);
        run.footItemId = GetItemId(foot);
    }


    private int GetItemId(ItemData item)
    {
        if (item == null)
            return -1;

        return item.id;
    }


    //============== 장비 복원 ==============

    public void RestoreEquipment(
        ItemData weapon,
        ItemData hand,
        ItemData head,
        ItemData body,
        ItemData foot)
    {
        // 인벤토리에 넣지 않고 장비 슬롯만 직접 변경
        SetSlotWithoutInventory(weaponSlot, weapon);
        SetSlotWithoutInventory(handSlot, hand);
        SetSlotWithoutInventory(headSlot, head);
        SetSlotWithoutInventory(bodySlot, body);
        SetSlotWithoutInventory(footSlot, foot);

        // 캐릭터 외형 적용
        if (weapon != null) ApplyAppearance(weapon);
        else playerAppearance?.ClearWeapon("저장 데이터에서 무기 아이템을 복원하지 못했거나 저장된 장착 무기가 없음");
        ApplyAppearance(hand);
        ApplyAppearance(head);
        ApplyAppearance(body);
        ApplyAppearance(foot);

        if (previewUI != null)
            previewUI.Refresh();

        RefreshStats();
    }


    private void SetSlotWithoutInventory(
        EquipmentSlot slot,
        ItemData item)
    {
        if (slot != null)
            slot.SetItem(item);
    }


    private void ApplyAppearance(ItemData item)
    {
        if (item != null && playerAppearance != null)
            playerAppearance.Equip(item);
    }


    //============== 장비 장착 ==============

    public bool Equip(ItemData item)
    {
        if (item == null)
            return false;

        EquipmentSlot slot = item.equipType switch
        {
            EquipmentType.Weapon => weaponSlot,
            EquipmentType.Hand => handSlot,
            EquipmentType.Head => headSlot,
            EquipmentType.Body => bodySlot,
            EquipmentType.Foot => footSlot,
            _ => null
        };

        if (slot == null || GetItem(slot)?.rarity == ItemRarity.Mechanization)
            return false;

        ReplaceIfDifferent(slot, item);

        // 플레이어 외형 적용
        if (playerAppearance != null)
        {
            playerAppearance.Equip(item);
        }

        if (previewUI != null)
            previewUI.Refresh();

        RefreshStats();

        // 현재 장비를 RunData에 저장
        SaveCurrentEquipmentToRunData();
        Debug.Log($"기계장비 개수: {EndingManager.Instance.MachineEquipmentCount}");
        return true;
    }


    //============== 기존 장비 교체 ==============

    void ReplaceIfDifferent(
        EquipmentSlot slot,
        ItemData newItem)
    {
        if (slot == null)
            return;

        ItemData current = slot.GetItem();

        if (current != null && current != newItem)
        {
            if (inventory != null)
                inventory.AddItem(current);
        }

        slot.SetItem(newItem);
    }


    //============== 장비 해제 ==============

    public void Unequip(int index)
    {
        EquipmentSlot slot = index switch
        {
            0 => weaponSlot,
            1 => handSlot,
            2 => headSlot,
            3 => bodySlot,
            4 => footSlot,
            _ => null
        };

        if (slot == null || GetItem(slot)?.rarity == ItemRarity.Mechanization)
            return;

        ItemData item = slot.GetItem();
        slot.SetItem(null);

        if (slot == weaponSlot)
            playerAppearance?.ClearWeapon("무기 장착 해제");

        if (item != null && inventory != null)
            inventory.AddItem(item);

        if (previewUI != null)
            previewUI.Refresh();

        RefreshStats();

        // 장비 해제 상태도 RunData에 저장
        SaveCurrentEquipmentToRunData();
    }


    //============== 능력치 갱신 ==============

    public void RefreshStats()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        ItemData weapon = GetItem(weaponSlot);
        ItemData head = GetItem(headSlot);
        ItemData body = GetItem(bodySlot);
        ItemData hand = GetItem(handSlot);
        ItemData foot = GetItem(footSlot);

        if (playerController != null)
        {
            playerController.SetCurrentWeaponFromInventoryType(
                weapon != null ? weapon.weaponType : InventoryWeaponType.None
            );
            playerController.RecalculateEquipmentStats(
                weapon,
                head,
                body,
                hand,
                foot
            );

            stats = playerController.Stats;
        }

        onStatsChanged?.Invoke();
    }


    //============== 장비 슬롯 아이템 가져오기 ==============

    private ItemData GetItem(EquipmentSlot slot)
    {
        return slot != null
            ? slot.GetItem()
            : null;
    }


    //============== 디버그용 ==============

    void Add(EquipmentSlot slot)
    {
        if (slot == null)
            return;

        ItemData item = slot.GetItem();

        if (item == null)
        {
            Debug.Log(slot.name + " = EMPTY");
            return;
        }

        Debug.Log(slot.name + " = " + item.itemName);

        stats.Add(item);
    }
}
