using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public GameObject inventoryPanel;
    public GameObject equipmentPanel;
    public InventoryDisplay inventoryDisplay;

    private int selectedIndex = 0;

    void Start()
    {
        inventoryPanel.SetActive(false);
        equipmentPanel.SetActive(false);
    }

    void Update()
    {
        // 열기/닫기
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool isOpen = !inventoryPanel.activeSelf;

            inventoryPanel.SetActive(isOpen);
            equipmentPanel.SetActive(isOpen);

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

        //교체(장착)
        if (Input.GetKeyDown(KeyCode.Z))
            inventoryDisplay.EquipSelected(selectedIndex);
        //버리기
        if (Input.GetKeyDown(KeyCode.X))
            inventoryDisplay.DropSelected(selectedIndex);
    }
}