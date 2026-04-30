using UnityEngine;

// 인벤토리 UI 관리
public class InventoryUI : MonoBehaviour
{
    public GameObject inventoryPanel; // 인벤토리 패널 (인스펙터에서 드래그드롭으로 할당)
    
    void Start()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false); // 시작 시 비활성화
        }
        else
        {
            Debug.LogWarning("InventoryPanel을 인스펙터에서 할당해 주세요.");
        }
    }
    
    // 인벤토리 UI 열기
    public void Open()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
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
}
