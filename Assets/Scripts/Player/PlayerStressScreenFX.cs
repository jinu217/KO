using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Optional, read-only presentation layer. Removing the Resources settings asset disables bootstrapping.
// Only the render projection pulses; camera settings, combat and save data stay untouched.
public sealed class PlayerStressScreenFX : MonoBehaviour
{
    public PlayerStressFXSettings settings;
    PlayerStatus player;
    EquipmentManager equipment;
    EquipmentTestRoom testRoom;
    Camera pulseCamera, renderingCamera;
    MachineNoisePostProcess distortion;
    float savedSize, savedFov;
    float zoomAmount;
    public float CurrentPulseZoom => zoomAmount;
    InventoryUI inventory;
    Canvas canvas;
    RawImage noise, pulse, machineRed;
    Material machineRedMaterial;
    float redAmount;
    public float CurrentMechanizationRed => redAmount;
    Texture2D[] grain;
    Texture2D vignette;
    float nextFind, nextNoise, phase, machineAmount, tension;
    int frame;
    static PlayerStressScreenFX instance;
#if UNITY_EDITOR
    [Header("실행 중 시각 확인 전용 / 게임 데이터에 영향 없음")]
    public bool preview;
    [Range(0, 5)] public int previewMachineCount;
    [Range(91, 220)] public float previewBpm = 180;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatic() { instance = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null) return;
        var config = Resources.Load<PlayerStressFXSettings>("PlayerStressFXSettings");
        if (config == null) return;
        var go = new GameObject("Player Stress Screen FX");
        instance = go.AddComponent<PlayerStressScreenFX>();
        instance.settings = config;
        DontDestroyOnLoad(go);
    }

    void OnEnable()
    {
        SceneManager.activeSceneChanged += SceneChanged;
        Camera.onPreCull += BeforeCamera;
        Camera.onPostRender += AfterCamera;
    }
    void BeforeCamera(Camera camera)
    {
        if (camera != pulseCamera || zoomAmount <= 0 || camera.cameraType != CameraType.Game) return;
        RestoreProjection();
        renderingCamera = camera;
        savedSize = camera.orthographicSize; savedFov = camera.fieldOfView;
        if(camera.orthographic) camera.orthographicSize = savedSize / (1 + zoomAmount);
        else camera.fieldOfView = 2 * Mathf.Atan(Mathf.Tan(savedFov * Mathf.Deg2Rad * .5f) / (1 + zoomAmount)) * Mathf.Rad2Deg;
    }
    void AfterCamera(Camera camera) { if (camera == renderingCamera) RestoreProjection(); }
    void RestoreProjection()
    {
        if (renderingCamera != null)
        {
            renderingCamera.orthographicSize = savedSize;
            renderingCamera.fieldOfView = savedFov;
        }
        renderingCamera = null;
    }
    void OnDisable()
    {
        SceneManager.activeSceneChanged -= SceneChanged;
        Camera.onPreCull -= BeforeCamera; Camera.onPostRender -= AfterCamera;
        RestoreProjection(); zoomAmount = 0;
        StopDistortion();
        if (canvas != null) canvas.enabled = false;
    }
    void SceneChanged(Scene oldScene, Scene newScene)
    {
        RestoreProjection(); zoomAmount = 0; pulseCamera = null; testRoom = null;
        StopDistortion();
        player = null; equipment = null; inventory = null;
        nextFind = 0; machineAmount = redAmount = tension = phase = 0;
        if (canvas != null) canvas.enabled = false;
    }

    public static int MachineCount(EquipmentManager owner)
    {
        if (owner == null) return 0;
        return IsMachine(owner.weaponSlot) + IsMachine(owner.handSlot) + IsMachine(owner.headSlot)
            + IsMachine(owner.bodySlot) + IsMachine(owner.footSlot);
    }
    static int IsMachine(EquipmentSlot slot)
    {
        var item = slot != null ? slot.GetItem() : null;
        return item != null && item.rarity == ItemRarity.Mechanization ? 1 : 0;
    }

    void Update()
    {
        zoomAmount = 0;
        if (Time.unscaledTime >= nextFind)
        {
            nextFind = Time.unscaledTime + .5f;
            if (testRoom == null) testRoom = FindFirstObjectByType<EquipmentTestRoom>();
            if (testRoom == null && (player == null || !player.isActiveAndEnabled))
            {
                player = FindFirstObjectByType<PlayerStatus>();
                equipment = player != null ? player.GetComponent<EquipmentManager>() : null;
                inventory = FindFirstObjectByType<InventoryUI>();
                phase = machineAmount = redAmount = tension = 0;
            }
        }
        bool testing = testRoom != null && testRoom.isActiveAndEnabled;
        pulseCamera = testing ? testRoom.TestCamera : Camera.main;
        bool visible = settings != null && settings.effectsEnabled && settings.intensity > 0
            && (testing ? testRoom.previewScreenEffects : player != null && player.isActiveAndEnabled && player.currentHp > 0)
            && Time.timeScale > .001f && (testing || inventory == null || !inventory.IsOpen);
        if (!visible)
        {
            StopDistortion();
            if (canvas != null) canvas.enabled = false;
            return;
        }
        int count = testing ? testRoom.MachineEquipmentCount : MachineCount(equipment);
        float bpm = testing ? testRoom.previewBpm : player.currentBpm;
#if UNITY_EDITOR
        if (preview) { count = previewMachineCount; bpm = previewBpm; }
#endif
        float dt = Time.deltaTime;
        float blend = 1 - Mathf.Exp(-dt * 4);
        machineAmount = Mathf.Lerp(machineAmount, settings.machineNoise ? Mathf.Clamp01(count / 5f) : 0, blend);
        redAmount = Mathf.Lerp(redAmount, settings.machineRed ? Mathf.Clamp01(count / 5f) : 0, blend);
        float target = settings.heartbeat ? Mathf.InverseLerp(settings.pulseStartBpm,
            Mathf.Max(settings.pulseStartBpm + 1, settings.pulseFullBpm), bpm) : 0;
        tension = Mathf.Lerp(tension, target, blend);
        phase = Mathf.Repeat(phase + dt * Mathf.Clamp(bpm, 40, 220) / 60, 1);
        if (machineAmount < .001f && redAmount < .001f && tension < .001f)
        {
            StopDistortion();
            if (canvas != null) canvas.enabled = false;
            return;
        }
        if (canvas == null) BuildOverlay();
        canvas.enabled = true;
        Rect viewport = testing && pulseCamera != null ? pulseCamera.rect : new Rect(0, 0, 1, 1);
        noise.rectTransform.anchorMin = pulse.rectTransform.anchorMin = machineRed.rectTransform.anchorMin = viewport.min;
        noise.rectTransform.anchorMax = pulse.rectTransform.anchorMax = machineRed.rectTransform.anchorMax = viewport.max;
        if (Time.unscaledTime >= nextNoise)
        {
            float noiseRate = Mathf.Clamp(settings.noiseFramesPerSecond, 2, 24) * Mathf.Lerp(.45f, 1, machineAmount);
            nextNoise = Time.unscaledTime + 1 / noiseRate;
            // A longer deterministic sequence, independent of combat's random stream and BPM.
            noise.texture = grain[(frame * 5 + frame / 7) % grain.Length];
            frame = (frame + 1) % (grain.Length * 7);
        }
        float strength = Mathf.Clamp01(settings.intensity);
        if(machineRedMaterial != null)
        {
            machineRedMaterial.SetFloat("_Progress", redAmount);
            Color red = settings.machineRedColor;
            red.a = Mathf.Clamp(settings.machineRedOpacity, 0, .4f) * strength;
            machineRed.color = red;
        }
        noise.color = new Color(.9f, .95f, 1,
            Mathf.Clamp(settings.noiseOpacity, 0, .18f) * machineAmount * strength);
        var style = testing ? testRoom.previewNoiseStyle : settings.noiseStyle;
        if (style == MachineNoiseStyle.Static || machineAmount < .001f || pulseCamera == null)
            StopDistortion();
        else
        {
            if (distortion != null && distortion.gameObject != pulseCamera.gameObject)
            { distortion.enabled = false; Destroy(distortion); distortion = null; }
            if (distortion == null) distortion = pulseCamera.gameObject.AddComponent<MachineNoisePostProcess>();
            float rate = Mathf.Clamp(settings.noiseFramesPerSecond, 2, 24) * Mathf.Lerp(.45f, 1, machineAmount);
            if (distortion.Configure(style, machineAmount * strength * Mathf.Clamp(settings.noiseOpacity / .11f, 0, 1.64f), rate))
                noise.color = Color.clear;
        }
        float beat = Beat(phase, 0, .23f) + .55f * Beat(phase, .30f, .19f);
        zoomAmount = Mathf.Clamp(settings.pulseZoom, 0, .025f) * tension * beat * strength;
        Color color = settings.pulseColor;
        color.a = Mathf.Clamp(settings.pulseOpacity, 0, .25f) * tension * beat * strength;
        pulse.color = color;
    }
    void StopDistortion() { if (distortion != null) distortion.enabled = false; }

    static float Beat(float p, float start, float width)
    {
        float t = (p - start) / width;
        if (t <= 0 || t >= 1) return 0;
        float s = Mathf.Sin(t * Mathf.PI);
        return s * s;
    }
    void BuildOverlay()
    {
        var go = new GameObject("Stress FX Overlay", typeof(RectTransform), typeof(Canvas));
        go.transform.SetParent(transform, false);
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -100; // Existing HUD/menu canvases stay above the effect.
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
        var random = new System.Random(4181); // Never advance Unity's gameplay random stream.
        grain = new Texture2D[8];
        for (int i = 0; i < grain.Length; i++) grain[i] = MakeNoiseTexture(384, 216, random);
        vignette = MakeVignette(128, 128);
        machineRed = MakeImage("Mechanization red edge", Texture2D.whiteTexture);
        var redShader = Resources.Load<Shader>("MechanizationRedEdge");
        if(redShader != null && redShader.isSupported)
        {
            machineRedMaterial = new Material(redShader) { hideFlags = HideFlags.DontSave };
            machineRed.material = machineRedMaterial;
        }
        noise = MakeImage("Machine static noise", grain[0]);
        pulse = MakeImage("BPM edge pulse", vignette);
    }
    RawImage MakeImage(string label, Texture texture)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        go.transform.SetParent(canvas.transform, false);
        var image = go.GetComponent<RawImage>();
        image.texture = texture; image.raycastTarget = false; image.color = Color.clear;
        var rect = image.rectTransform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return image;
    }
    static Texture2D MakeVignette(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        { name = "Stress FX vignette", filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
        {
            float nx = Mathf.Abs(2f * (x + .5f) / w - 1), ny = Mathf.Abs(2f * (y + .5f) / h - 1);
            float edge = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.45f, 1, Mathf.Max(nx, ny)));
            pixels[y * w + x] = new Color32(255, 255, 255, (byte)(255 * edge));
        }
        tex.SetPixels32(pixels); tex.Apply(false, true);
        return tex;
    }
    static Texture2D MakeNoiseTexture(int w, int h, System.Random random)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        { name = "Machine static", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave };
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            bool interference = random.NextDouble() < .025;
            int gap = random.Next(w);
            byte lineTone = random.NextDouble() < .5 ? (byte)15 : (byte)240;
            for (int x = 0; x < w; x++)
            {
                float nx = Mathf.Abs(2f * (x + .5f) / w - 1), ny = Mathf.Abs(2f * (y + .5f) / h - 1);
                float edge = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.4f, 1, Mathf.Max(nx, ny)));
                float alpha = (.45f + .55f * edge) * (.15f + .75f * (float)random.NextDouble());
                byte tone = (byte)random.Next(256);
                // Broken scanlines make electrical interference distinct from the smooth heartbeat.
                if (interference && (x + gap) % w < w * .7f)
                { alpha = Mathf.Max(alpha, .65f + .25f * edge); tone = lineTone; }
                pixels[y * w + x] = new Color32(tone, tone, tone, (byte)(255 * alpha));
            }
        }
        tex.SetPixels32(pixels); tex.Apply(false, true);
        return tex;
    }
    void OnDestroy()
    {
        if (instance == this) instance = null;
        if (distortion != null) { distortion.enabled = false; Destroy(distortion); }
        if (grain != null) foreach (var texture in grain) if (texture != null) Destroy(texture);
        if (vignette != null) Destroy(vignette);
        if (machineRedMaterial != null) Destroy(machineRedMaterial);
    }
}
