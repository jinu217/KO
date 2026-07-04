using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// [보물상자 상호작용]
// - 플레이어가 interactRange 이내에서 Interact(F) 입력 시 보상 UI를 열고 아이템 선택 지급
// - oneTimeChest가 true면 1회 오픈 후 비활성화
public class TreasureChest : MonoBehaviour
{
    public static int LastInteractFrame { get; private set; } = -1;

    [Header("상호작용")]
    [Tooltip("플레이어가 상자와 상호작용할 수 있는 반경")]
    [SerializeField] private float interactRange = 2f;
    [Tooltip("true면 보상 선택 후 재오픈 불가")]
    [SerializeField] private bool oneTimeChest = true;

    [Header("범위 시각화")]
    [Tooltip("상호작용 반경 Gizmo 표시(OnDrawGizmosSelected)")]
    [SerializeField] private bool showInteractRangeGizmo = true;

    [Header("드롭 풀")]
    [Tooltip("보상 후보 아이템 목록")]
    [SerializeField] private List<ItemData> possibleItems = new List<ItemData>();
    [Tooltip("오픈 시 랜덤으로 뽑는 보상 개수")]
    [SerializeField] private int rollCount = 3;
    [Tooltip("true면 동일 아이템 중복 선택 허용")]
    [SerializeField] private bool allowDuplicate = false;

    [Header("UI")]
    [Tooltip("보상 선택 UI 컴포넌트(미연결 시 씬에서 자동 탐색)")]
    [SerializeField] private ChestRewardPanel rewardUI;

    [Header("디버그")]
    [Tooltip("보물상자 오픈/보상 획득 로그 출력")]
    [SerializeField] private bool showDebugLog = true;

    // 플레이어 참조 및 입력 액션
    private Transform playerTransform;
    private PlayerInput playerInput;
    private InputAction interactAction;
    private InventoryUI inventoryUI;

    // 상태
    private bool isOpened;              // 보상 선택 완료 여부(1회 상자에서 재오픈 차단)
    private bool waitingRewardSelect;   // 보상 UI가 열려 있어 선택 대기 중 여부

    // 플레이어 오브젝트에서 PlayerInput/Interact 액션 캐시, InventoryUI/ChestRewardUI 탐색
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

        inventoryUI = FindInventoryUIAnyState();

        if (rewardUI == null)
            rewardUI = FindChestRewardUIAnyState();
    }

    // 매 프레임: 보상 대기/이미 열린 상태는 skip, 근처에서 Interact 입력 감지
    private void Update()
    {
        if (isOpened && oneTimeChest) return;
        if (waitingRewardSelect) return;
        if (playerTransform == null || interactAction == null) return;

        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (distance <= interactRange && interactAction.WasPressedThisFrame())
            Interact();
    }

    // 보상 아이템을 롤한 뒤 ChestRewardUI를 열어 선택 대기 상태로 전환
    private void Interact()
    {
        LastInteractFrame = Time.frameCount;
        TutorialManager.Instance?.NotifyChestInteracted();

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

        List<ItemData> rewards = RollItems();

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

    // ChestRewardUI에서 보상 선택 완료 시 호출: 대기 해제 + 오픈 완료 처리
    public void NotifyRewardClaimed()
    {
        waitingRewardSelect = false;
        isOpened = true;

        if (showDebugLog)
            Debug.Log($"[{name}] 보상 획득 완료");
    }

    // possibleItems에서 rollCount개를 랜덤 추출(allowDuplicate에 따라 중복 허용 여부 결정)
    private List<ItemData> RollItems()
    {
        List<ItemData> result = new List<ItemData>();
        if (possibleItems == null || possibleItems.Count == 0) return result;

        int targetCount = Mathf.Max(0, rollCount);
        if (!allowDuplicate)
            targetCount = Mathf.Min(targetCount, possibleItems.Count);

        if (allowDuplicate)
        {
            for (int i = 0; i < targetCount; i++)
                result.Add(possibleItems[Random.Range(0, possibleItems.Count)]);

            return result;
        }

        // 중복 없이 추출: 인덱스 풀에서 하나씩 제거하며 뽑기
        List<int> indices = new List<int>();
        for (int i = 0; i < possibleItems.Count; i++)
            indices.Add(i);

        for (int i = 0; i < targetCount; i++)
        {
            int pick = Random.Range(0, indices.Count);
            result.Add(possibleItems[indices[pick]]);
            indices.RemoveAt(pick);
        }

        return result;
    }

    // 비활성 오브젝트까지 포함해서 InventoryUI 탐색
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

    // 비활성 오브젝트까지 포함해서 ChestRewardUI 탐색
    private ChestRewardPanel FindChestRewardUIAnyState()
    {
        ChestRewardPanel found = FindFirstObjectByType<ChestRewardPanel>();
        if (found != null) return found;

        var all = Resources.FindObjectsOfTypeAll<ChestRewardPanel>();
        foreach (var ui in all)
        {
            if (ui == null) continue;
            if (!ui.gameObject.scene.IsValid()) continue;
            return ui;
        }

        return null;
    }

    // 상호작용 반경 Gizmo 표시(showInteractRangeGizmo 켤 때만)
    private void OnDrawGizmosSelected()
    {
        if (!showInteractRangeGizmo) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
