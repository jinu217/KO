using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    void Start()
    {
        transform.position = GameData.Instance.spawnPosition;
    }
}