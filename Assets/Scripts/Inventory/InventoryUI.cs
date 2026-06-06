using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public GameObject inventoryPanel;
    public GameObject equipmentPanel;
    public GameObject characterPreview;
    public InventoryDisplay inventoryDisplay;

    private int selectedIndex = 0;

    void Start()
    {
        inventoryPanel.SetActive(false);
        equipmentPanel.SetActive(false);
        characterPreview.SetActive(false);
    }

    void Update()
    {
        // 열기/닫기
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool isOpen = !inventoryPanel.activeSelf;

            inventoryPanel.SetActive(isOpen);
            equipmentPanel.SetActive(isOpen);
            characterPreview.SetActive(isOpen);

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

// ===============임의용=============
    public bool TryAddItemToInventory(ItemData item)
    {
        if (item == null)
            return false;

        if (inventoryDisplay == null)
            return false;

        if (inventoryDisplay.inventory == null)
            return false;

        inventoryDisplay.inventory.AddItem(item);

        inventoryDisplay.UpdateInventory();

        return true;
    }

    public void Open()
    {
        inventoryPanel.SetActive(true);

        if (equipmentPanel != null)
            equipmentPanel.SetActive(true);

        if (characterPreview != null)
            characterPreview.SetActive(true);

        selectedIndex = 0;

        if (inventoryDisplay != null)
            inventoryDisplay.SetSelectedIndex(selectedIndex);
    }

    public void Close()
    {
        inventoryPanel.SetActive(false);

        if (equipmentPanel != null)
            equipmentPanel.SetActive(false);

        if (characterPreview != null)
            characterPreview.SetActive(false);
    }
    public void Navigate(Vector2 moveInput)
    {
        if (!inventoryPanel.activeSelf)
            return;

        if (moveInput.x > 0.5f)
            selectedIndex++;

        else if (moveInput.x < -0.5f)
            selectedIndex--;

        else if (moveInput.y < -0.5f)
            selectedIndex += 3;

        else if (moveInput.y > 0.5f)
            selectedIndex -= 3;

        selectedIndex = Mathf.Clamp(
            selectedIndex,
            0,
            inventoryDisplay.GetSlotCount() - 1
        );

        inventoryDisplay.SetSelectedIndex(selectedIndex);
    }

    public void ConfirmCurrentSelection()
    {
        inventoryDisplay.EquipSelected(selectedIndex);
    }

}