using UnityEngine;
using System.Collections.Generic;

//============== 아이템 데이터베이스 ==============

[CreateAssetMenu(menuName = "Items/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField]
    private List<ItemData> items = new List<ItemData>();

    public IReadOnlyList<ItemData> Items => items;

    private Dictionary<int, ItemData> itemsById;


    //============== ID로 아이템 찾기 ==============

    public ItemData GetById(int id)
    {
        if (id < 0)
            return null;

        BuildLookup();

        itemsById.TryGetValue(id, out ItemData item);

        return item;
    }


    //============== ID 검색용 Dictionary 생성 ==============

    private void BuildLookup()
    {
        if (itemsById != null)
            return;

        itemsById = new Dictionary<int, ItemData>();

        foreach (ItemData item in items)
        {
            if (item == null)
                continue;

            if (itemsById.ContainsKey(item.id))
            {
                Debug.LogError(
                    $"[ItemDatabase] 중복 아이템 ID: {item.id} ({item.name})",
                    this
                );

                continue;
            }

            itemsById.Add(item.id, item);
        }
    }


#if UNITY_EDITOR

    //============== Inspector 변경 시 Dictionary 초기화 ==============

    private void OnValidate()
    {
        itemsById = null;
    }

#endif
}