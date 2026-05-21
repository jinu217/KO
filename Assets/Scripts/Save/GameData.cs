using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData Instance;

    public int level;
    public float hp;
    public Vector3 spawnPosition;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}