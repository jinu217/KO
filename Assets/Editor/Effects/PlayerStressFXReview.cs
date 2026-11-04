#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Opt-in only; no scene/prefab/save-data changes. Can be removed with the prototype.
[InitializeOnLoad]
public static class PlayerStressFXReview
{
    const string Folder = "C:/Users/User/Documents/Codex/2026-10-26/new-chat-3/work/stress_fx_review";
    static bool running;
    static PlayerStressFXReview()
    {
        EditorApplication.update += () =>
        {
            string path = Folder + "/request.txt";
            if (!Application.isPlaying || running || !File.Exists(path)) return;
            string id = File.ReadAllText(path);
            if (SessionState.GetString("StressFXReview", "") == id) return;
            SessionState.SetString("StressFXReview", id);
            Run();
        };
    }
    [MenuItem("Tools/Effects/Player/Review Stress Screen FX (Play Mode)")]
    public static void Run()
    {
        var fx = Object.FindFirstObjectByType<PlayerStressScreenFX>();
        if (!Application.isPlaying || fx == null || running) return;
        running = true;
        fx.StartCoroutine(Review(fx));
    }
    static IEnumerator Review(PlayerStressScreenFX fx)
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "/result.txt", "RUNNING\n");
        var config = fx.settings;
        bool originalEnabled = config.effectsEnabled;
        float originalTime = Time.timeScale;
        bool wasPreview = fx.preview;
        int oldCount = fx.previewMachineCount; float oldBpm = fx.previewBpm;
        try
        {
            fx.preview = true;
            int[] counts = { 0, 5, 0, 5 };
            float[] bpms = { 91, 91, 195, 195 };
            string[] names = { "baseline", "machine", "heartbeat", "combined" };
            for (int i = 0; i < names.Length; i++)
            {
                fx.previewMachineCount = counts[i]; fx.previewBpm = bpms[i];
                yield return new WaitForSecondsRealtime(2);
                // Catch the bright part of a beat, rather than an arbitrary still frame.
                float until = Time.realtimeSinceStartup + 1;
                while (i >= 2 && Time.realtimeSinceStartup < until
                    && fx.GetComponentsInChildren<RawImage>().All(x => x.name != "BPM edge pulse" || x.color.a < .12f))
                    yield return null;
                yield return Capture(names[i]);
                Log(fx, names[i]);
            }
            config.effectsEnabled = false;
            yield return null;
            Log(fx, "disabled");
            config.effectsEnabled = originalEnabled;
            Time.timeScale = 0;
            yield return null;
            Log(fx, "paused");
            Time.timeScale = originalTime;
            fx.previewMachineCount = 0; fx.previewBpm = 91;
            yield return new WaitForSecondsRealtime(2.5f);
            Log(fx, "removed_and_recovered");
            File.AppendAllText(Folder + "/result.txt", "DONE\n");
        }
        finally
        {
            config.effectsEnabled = originalEnabled; Time.timeScale = originalTime;
            fx.preview = wasPreview; fx.previewMachineCount = oldCount; fx.previewBpm = oldBpm;
            running = false;
        }
    }
    static void Log(PlayerStressScreenFX fx, string label)
    {
        var canvas = fx.GetComponentInChildren<Canvas>();
        File.AppendAllText(Folder + "/result.txt", label + " canvas=" + (canvas != null && canvas.enabled)
            + " " + string.Join("; ", fx.GetComponentsInChildren<RawImage>().Select(x => x.name + " alpha=" + x.color.a + " raycast=" + x.raycastTarget)) + "\n");
    }
    static IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        var image = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Folder + "/" + name + ".png", image.EncodeToPNG());
        Object.Destroy(image);
    }
}
#endif
