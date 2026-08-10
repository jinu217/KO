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

    public int blacksmithRound = -1;
    public int blacksmithPurchaseCount;

    public int equipmentShopRound = -1;
    public bool equipmentOffersInitialized;
    public int offeredHeadItemId = -1;
    public int offeredBodyItemId = -1;
    public int offeredHandItemId = -1;
    public int offeredFootItemId = -1;
    public bool offeredHeadSold;
    public bool offeredBodySold;
    public bool offeredHandSold;
    public bool offeredFootSold;
    public int equipmentRerollUsedCount;

    public List<int> shamanPurchasedProductIds = new List<int>();
    public int shamanHpPotionPurchasedCount;
    public int shamanBpmPotionPurchasedCount;

    public string currentScene;
    public string spawnId;

    public bool inBossFight;
    public string currentBossID;

    // 이번 회차 기록
    public int killCount;
    public int midBossKillCount;
    public int mainBossKillCount;
}
