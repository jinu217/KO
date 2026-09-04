using UnityEngine;

[CreateAssetMenu(fileName = "NewMainBossData", menuName = "ScriptableObjects/Boss/MainBossData")]
public class MainBossData : BossDataBase
{
    [Header("페이즈")]
    [Range(0f, 1f)] public float phase2HpPercent = 0.7f;
    [Range(0f, 1f)] public float phase3HpPercent = 0.3f;
}
