using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    private void Start()
    {
        if (SaveData.Instance == null)
            return;

        string spawnId = SaveData.Instance.run.spawnId;
        if (!string.IsNullOrWhiteSpace(spawnId))
        {
            SpawnPoint[] spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            foreach (SpawnPoint spawnPoint in spawnPoints)
            {
                if (spawnPoint.SpawnId == spawnId)
                {
                    transform.position = spawnPoint.transform.position;
                    return;
                }
            }

            Debug.LogWarning($"SpawnPoint를 찾지 못했습니다: {spawnId}");
        }

        if (SaveData.Instance.spawnPosition != Vector3.zero)
            transform.position = SaveData.Instance.spawnPosition;
    }
}
