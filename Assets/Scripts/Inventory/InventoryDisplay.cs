using UnityEngine;

public class InventoryDisplay : MonoBehaviour
{
    public GameObject slotPrefab;
    public int slotCount = 20;

    void Start()
    {
        for(int i = 0; i < slotCount; i++)
        {
            Instantiate(slotPrefab, transform);
        }
    }
}