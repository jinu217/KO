using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public GameObject inventoryPanel;
    public GameObject equipmentPanel;
    public GameObject characterPreview;
    public InventoryDisplay inventoryDisplay;
    private bool inputLocked = false;
    private int selectedIndex = 0;

    public bool IsOpen => inventoryPanel != null && inventoryPanel.activeSelf;

    void Start()
    {
        inventoryPanel.SetActive(false);
        equipmentPanel.SetActive(false);
        characterPreview.SetActive(false);
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

        TutorialManager.Instance?.NotifyInventoryClosed();
    }
    public void Navigate(Vector2 moveInput)
    {
        if (!inventoryPanel.activeSelf)
            return;

        if (Mathf.Abs(moveInput.x) < 0.5f &&
            Mathf.Abs(moveInput.y) < 0.5f)
        {
            inputLocked = false;
            return;
        }

        if (inputLocked)
            return;

        inputLocked = true;

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

    public void DropCurrentSelection()
    {
        Debug.Log("DropCurrentSelection 호출");
        inventoryDisplay.DropSelected(selectedIndex);
    }
}
