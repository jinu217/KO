using System.Collections.Generic;

[System.Serializable]
public class RunData
{
    public bool initialized;

    public float hp = 50f;
    public float maxHp = 50f;
    public float sp = 100f;
    public float bpm = 91f;

    public List<int> inventoryItemIds = new List<int>();

    public int weaponItemId = -1;
    public int handItemId = -1;
    public int headItemId = -1;
    public int bodyItemId = -1;
    public int footItemId = -1;

    public int healPotionCount;
    public int bpmPotionCount;
    public int coin;

    public string currentScene;
    public string spawnId;

    public bool inBossFight;
    public string currentBossID;
}
