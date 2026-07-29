using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    void Start()
    {
        transform.position = SaveData.Instance.spawnPosition;
    }
}