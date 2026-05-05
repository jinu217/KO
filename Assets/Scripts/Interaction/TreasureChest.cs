using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 보물상자 상호작용:
// - 기준은 보물상자 위치
// - 플레이어가 상자 근처(interactRange)에서 Interact(F) 입력 시 오픈
// - 오픈 시 랜덤 아이템 3개를 UI에 표시하고 1개 선택 지급
public class TreasureChest : MonoBehaviour
{
    [Header("상호작용")]
    [SerializeField] private float interactRange = 2f;
    [SerializeField] private bool oneTimeChest = true;

    [Header("드롭 풀")]
    [SerializeField] private List<InventoryItemData> possibleItems = new List<InventoryItemData>();
    [SerializeField] private int rollCount = 3;
    [SerializeField] private bool allowDuplicate = false;

    [Header("UI")]
    [SerializeField] private ChestRewardUI rewardUI;

    [Header("디버그")]
    [SerializeField] private bool showDebugLog = true;

    private Transform playerTransform;
    private PlayerInput playerInput;
    private InputAction interactAction;
    private InventoryUI inventoryUI;

    private bool isOpened;
    private bool waitingRewardSelect;

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogWarning($"[{name}] Player 태그 오브젝트를 찾지 못해 상호작용이 비활성화됩니다.");
            enabled = false;
            return;
        }

        playerTransform = playerObj.transform;
        playerInput = playerObj.GetComponent<PlayerInput>();

        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogWarning($"[{name}] PlayerInput 또는 InputActionAsset을 찾지 못했습니다.");
            enabled = false;
            return;
        }

        interactAction = playerInput.actions["Interact"];
        if (interactAction == null)
        {
            Debug.LogWarning($"[{name}] PlayerInput에 'Interact' 액션이 없습니다. 액션 이름을 확인해 주세요.");
            enabled = false;
            return;
        }

        // 인벤토리 UI 탐색(비활성 포함)
        inventoryUI = FindInventoryUIAnyState();

        if (rewardUI == null)
        {
            rewardUI = FindChestRewardUIAnyState();
        }
    }

    private void Update()
    {
        if (isOpened && oneTimeChest) return;
        if (waitingRewardSelect) return;
        if (playerTransform == null || interactAction == null) return;

        float distance = Vector2.Distance(transform.position, playerTransform.position);
        bool isPlayerNear = distance <= interactRange;

        // 상자 기준: 플레이어가 근처에 있을 때 F(Interact) 입력으로 상호작용
        if (isPlayerNear && interactAction.WasPressedThisFrame())
        {
            Interact();
        }
    }

    private void Interact()
    {
        if (rewardUI == null)
        {
            Debug.LogWarning($"[{name}] ChestRewardUI가 없어 보상 UI를 띄울 수 없습니다.");
            return;
        }

        if (inventoryUI == null)
        {
            inventoryUI = FindInventoryUIAnyState();
            if (inventoryUI == null)
            {
                Debug.LogWarning($"[{name}] InventoryUI를 찾지 못했습니다.");
                return;
            }
        }

        List<InventoryItemData> rewards = RollItems();

        if (rewards.Count == 0)
        {
            if (showDebugLog)
                Debug.Log($"[{name}] 보물상자 오픈: 획득 가능한 아이템이 없습니다.");
            return;
        }

        if (showDebugLog)
        {
            string joined = string.Join(", ", rewards.ConvertAll(item => item != null ? item.itemName : "(null)"));
            Debug.Log($"[{name}] 보물상자 오픈! 랜덤 아이템 {rewards.Count}개: {joined}");
        }

        waitingRewardSelect = true;
        rewardUI.Open(rewards, this, inventoryUI);
    }

    // ChestRewardUI에서 보상 선택 완료 시 호출
    public void NotifyRewardClaimed()
    {
        waitingRewardSelect = false;
        isOpened = true;

        if (showDebugLog)
            Debug.Log($"[{name}] 보상 획득 완료");
    }

    private List<InventoryItemData> RollItems()
    {
        List<InventoryItemData> result = new List<InventoryItemData>();
        if (possibleItems == null || possibleItems.Count == 0) return result;

        int targetCount = Mathf.Max(0, rollCount);
        if (!allowDuplicate)
            targetCount = Mathf.Min(targetCount, possibleItems.Count);

        if (allowDuplicate)
        {
            for (int i = 0; i < targetCount; i++)
            {
                int rand = Random.Range(0, possibleItems.Count);
                result.Add(possibleItems[rand]);
            }
            return result;
        }

        List<int> indices = new List<int>();
        for (int i = 0; i < possibleItems.Count; i++)
            indices.Add(i);

        for (int i = 0; i < targetCount; i++)
        {
            int pick = Random.Range(0, indices.Count);
            int selectedIndex = indices[pick];
            indices.RemoveAt(pick);
            result.Add(possibleItems[selectedIndex]);
        }

        return result;
    }

    private InventoryUI FindInventoryUIAnyState()
    {
        InventoryUI found = FindFirstObjectByType<InventoryUI>();
        if (found != null) return found;

        var all = Resources.FindObjectsOfTypeAll<InventoryUI>();
        foreach (var ui in all)
        {
            if (ui == null) continue;
            if (!ui.gameObject.scene.IsValid()) continue;
            return ui;
        }

        return null;
    }

    private ChestRewardUI FindChestRewardUIAnyState()
    {
        ChestRewardUI found = FindFirstObjectByType<ChestRewardUI>();
        if (found != null) return found;

        var all = Resources.FindObjectsOfTypeAll<ChestRewardUI>();
        foreach (var ui in all)
        {
            if (ui == null) continue;
            if (!ui.gameObject.scene.IsValid()) continue;
            return ui;
        }

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
