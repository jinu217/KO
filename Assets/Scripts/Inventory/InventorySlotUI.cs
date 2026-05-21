using UnityEngine;

public class InventorySlotUI : MonoBehaviour
{
    public ItemData item; // 슬롯이 가진 아이템

    public ItemTooltipUI tooltipUI;

    // 버튼 클릭 시 실행
    public void OnClick()
    {
        tooltipUI.Show(item, transform);
    }
}