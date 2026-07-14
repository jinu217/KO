using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PersistentData
{
    public float playTime;

    public int deathCount;

    // 이어하기 시 시작할 스테이지
    public string nextStage;

    // 영구 재화
    public int soul;
}