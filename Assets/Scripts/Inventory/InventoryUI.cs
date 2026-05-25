using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public GameObject inventoryPanel;
    public InventoryDisplay inventoryDisplay;

    private int selectedIndex = 0;

    void Start()
    {
        inventoryPanel.SetActive(false);
    }

    void Update()
    {
        // 열기/닫기
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            inventoryPanel.SetActive(!inventoryPanel.activeSelf);

            selectedIndex = 0;
            inventoryDisplay.SetSelectedIndex(selectedIndex);
        }

        if (!inventoryPanel.activeSelf)
            return;

        // 이동
        if (Input.GetKeyDown(KeyCode.RightArrow))
            selectedIndex++;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            selectedIndex--;

        if (Input.GetKeyDown(KeyCode.DownArrow))
            selectedIndex += 3;

        if (Input.GetKeyDown(KeyCode.UpArrow))
            selectedIndex -= 3;

        selectedIndex = Mathf.Clamp(
            selectedIndex,
            0,
            inventoryDisplay.GetSlotCount() - 1
        );

        inventoryDisplay.SetSelectedIndex(selectedIndex);

        // ======================
        // 기능은 Display로 위임
        // ======================

        if (Input.GetKeyDown(KeyCode.Z))
            inventoryDisplay.EquipSelected(selectedIndex);

        if (Input.GetKeyDown(KeyCode.X))
        {
            // 나중에 장비창 선택 추가 예정
        }

        if (Input.GetKeyDown(KeyCode.C))
            inventoryDisplay.DropSelected(selectedIndex);
    }
}