using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Player Stress Screen Settings")]
public sealed class PlayerStressFXSettings : ScriptableObject
{
    [Header("전체 효과 / 0이면 즉시 해제")]
    public bool effectsEnabled = true;
    [Range(0, 1)] public float intensity = 1;
    [Header("기계 장비: 화면 가장자리 노이즈")]
    public bool machineNoise = true;
    [Range(0, .12f)] public float noiseOpacity = .065f;
    [Range(2, 12)] public float noiseFramesPerSecond = 8;
    [Header("BPM: 화면 확대 박동 + 가장자리 붉은빛")]
    public bool heartbeat = true;
    [Range(0, .25f)] public float pulseOpacity = .22f;
    [Range(0, .025f)] public float pulseZoom = .012f;
    public float pulseStartBpm = 130;
    public float pulseFullBpm = 200;
    public Color pulseColor = new Color(.5f, .035f, .05f, 1);
}
