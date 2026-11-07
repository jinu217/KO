using UnityEngine;
using UnityEngine.Audio;

public enum UISoundId
{
    TitleNavigate,
    SlotNavigate,
    SlotConfirm,
    Cancel,
    CategoryNavigate,
    DisplayAdjust,
    DeleteConfirm,
    DialogOpen,
    SettingsOpen,
    VolumeAdjust
}

[CreateAssetMenu(menuName = "Audio/UI Audio Library")]
public sealed class UIAudioLibrary : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public UISoundId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.75f;
    }

    public AudioClip titleBGM;
    public AudioMixer mixer;
    public AudioMixerGroup bgmGroup;
    public AudioMixerGroup sfxGroup;
    public Entry[] sounds;

    public Entry Find(UISoundId id)
    {
        if (sounds == null) return null;
        foreach (Entry entry in sounds)
            if (entry != null && entry.id == id) return entry;
        return null;
    }
}

public static class UISound
{
    public static void Play(UISoundId id)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(id);
    }
}
