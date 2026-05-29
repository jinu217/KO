using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData itemData;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어만 가능
        if (!other.CompareTag("Player"))
            return;

        // PlayerItemManager 가져오기
        PlayerItemManager itemManager =
            other.GetComponent<PlayerItemManager>();

        // 아이템 지급
        if (itemManager != null)
        {
            itemManager.AddItem(itemData);
        }

        // 바닥 아이템 삭제
        Destroy(gameObject);
    }
}