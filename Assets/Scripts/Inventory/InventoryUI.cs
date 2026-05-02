using UnityEngine;

// 인벤토리 UI 관리
// 슬롯 이동/선택 로직은 InventoryNavigationController로 분리
public class InventoryUI : MonoBehaviour
{
    [Header("패널")]
    public GameObject inventoryPanel; // 인벤토리 패널 (인스펙터에서 드래그드롭으로 할당)

    [Header("칸 이동 컨트롤러")]
    public InventoryNavigationController navigationController;

    void Start()
    {
        if (inventoryPanel == null)
        {
            Debug.LogWarning("InventoryPanel을 인스펙터에서 할당해 주세요.");
        }

        if (navigationController == null)
            navigationController = GetComponentInChildren<InventoryNavigationController>(true);
    }

    // 인벤토리 UI 열기
    public void Open()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            if (navigationController != null)
                navigationController.ResetNavigation();
            Debug.Log("인벤토리 UI 열림");
        }
    }

    // 인벤토리 UI 닫기
    public void Close()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            Debug.Log("인벤토리 UI 닫음");
        }
    }

    // 플레이어 입력(OnMove)에서 호출
    public void Navigate(Vector2 moveInput)
    {
        if (inventoryPanel == null || !inventoryPanel.activeSelf) return;
        if (navigationController == null) return;

        navigationController.Navigate(moveInput);
    }
}
