using UnityEngine;

public enum MachineNoiseStyle
{
    [InspectorName("기존 지직거림")] Static = 0,
    [InspectorName("가로 신호 간섭")] HorizontalInterference = 1,
    [InspectorName("세로 신호 간섭")] VerticalInterference = 2
}

[CreateAssetMenu(menuName = "Effects/Player Stress Screen Settings")]
public sealed class PlayerStressFXSettings : ScriptableObject
{
    [Header("전체 효과 / 0이면 즉시 해제")]
    public bool effectsEnabled = true;
    [Range(0, 1)] public float intensity = 1;
    [Header("기계 장비: 전체 장착 개수에 비례하는 화면 지직거림")]
    public bool machineNoise = true;
    public MachineNoiseStyle noiseStyle = MachineNoiseStyle.Static;
    [Range(0, .18f)] public float noiseOpacity = .11f;
    [Range(2, 24)] public float noiseFramesPerSecond = 20;
    [Header("기계 장비: 가장자리에서 중앙으로 번지는 붉은빛")]
    public bool machineRed = true;
    [Range(0, .4f)] public float machineRedOpacity = .4f;
    public Color machineRedColor = new Color(.65f, .015f, .025f, 1);
    [Header("BPM: 화면 확대 박동 + 가장자리 붉은빛")]
    public bool heartbeat = true;
    [Range(0, .25f)] public float pulseOpacity = .22f;
    [Range(0, .025f)] public float pulseZoom = .012f;
    public float pulseStartBpm = 130;
    public float pulseFullBpm = 200;
    public Color pulseColor = new Color(.5f, .035f, .05f, 1);
}
