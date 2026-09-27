using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    private const float SceneActivationDisplayedProgress = 0.7f;

    private sealed class PendingTransition
    {
        public string TargetSceneName;
        public string TargetSpawnId;
        public bool SaveBeforeLoad;
    }

    public static SceneTransitionManager Instance { get; private set; }

    [Header("Loading Progress")]
    [SerializeField, Range(0f, 1f)] private float previousSceneProgress = 0.45f;
    [SerializeField, Min(0f)] private float minimumPreviousSceneDuration = 0.35f;
    [Tooltip("다음 씬이 활성화된 뒤 로딩 패널이 100%까지 차오르는 최소 시간")]
    [SerializeField, Min(1f)] private float minimumNextSceneDuration = 1f;
    [SerializeField, Min(0f)] private float completedHoldDuration = 0.15f;

    private PendingTransition pendingTransition;
    private SceneLoadingPanel loadingPanel;
    private bool isTransitioning;
    private float displayedProgress;

    public bool IsTransitioning => isTransitioning;
    public bool HasPendingTransition => pendingTransition != null;
    public float DisplayedProgress => displayedProgress;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 씬에 배치된 매니저가 다른 오브젝트의 자식이어도
        // DontDestroyOnLoad가 정상 작동하도록 독립된 루트로 분리한다.
        if (transform.parent != null)
            transform.SetParent(null, true);

        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static SceneTransitionManager GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        SceneTransitionManager found = FindFirstObjectByType<SceneTransitionManager>();
        if (found != null)
            return found;

        GameObject managerObject = new GameObject(nameof(SceneTransitionManager));
        return managerObject.AddComponent<SceneTransitionManager>();
    }

    public void PrepareAutoExit(
        PlayerController player,
        string targetSceneName,
        string targetSpawnId,
        bool saveBeforeLoad,
        float autoMoveSpeed)
    {
        if (player == null || isTransitioning)
            return;

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("씬 전환 대상 이름이 비어 있어 자동 전진을 시작하지 않습니다.", this);
            return;
        }

        pendingTransition = new PendingTransition
        {
            TargetSceneName = targetSceneName,
            TargetSpawnId = targetSpawnId,
            SaveBeforeLoad = saveBeforeLoad
        };

        player.StartAutoMoveForward(autoMoveSpeed);
    }

    public bool TryLoadPendingTransition(PlayerController player)
    {
        if (isTransitioning || pendingTransition == null)
            return false;

        StartCoroutine(LoadPendingTransitionRoutine(player, pendingTransition));
        return true;
    }

    public void RegisterLoadingPanel(SceneLoadingPanel panel)
    {
        if (panel == null)
            return;

        loadingPanel = panel;
        loadingPanel.SetProgress(displayedProgress);

        if (isTransitioning)
            loadingPanel.Show();
        else
            loadingPanel.HideImmediate();
    }

    public void UnregisterLoadingPanel(SceneLoadingPanel panel)
    {
        if (loadingPanel == panel)
            loadingPanel = null;
    }

    private IEnumerator LoadPendingTransitionRoutine(
        PlayerController player,
        PendingTransition transition)
    {
        isTransitioning = true;
        displayedProgress = 0f;
        ShowLoadingPanel();

        if (player != null)
            player.PauseKeyboardControl();

        yield return AnimateProgress(0f, previousSceneProgress, minimumPreviousSceneDuration);

        SaveBeforeTransition(player, transition);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(transition.TargetSceneName);
        if (loadOperation == null)
        {
            AbortTransition(player, $"씬을 불러올 수 없습니다: {transition.TargetSceneName}");
            yield break;
        }

        loadOperation.allowSceneActivation = false;

        while (loadOperation.progress < 0.9f)
        {
            float normalizedLoadProgress = Mathf.Clamp01(loadOperation.progress / 0.9f);
            SetProgress(Mathf.Lerp(
                previousSceneProgress,
                SceneActivationDisplayedProgress,
                normalizedLoadProgress));
            yield return null;
        }

        SetProgress(SceneActivationDisplayedProgress);
        loadOperation.allowSceneActivation = true;

        while (!loadOperation.isDone)
            yield return null;

        // 새 씬의 Awake/Start와 로딩 패널 등록이 끝날 시간을 보장한다.
        yield return null;
        // 기존 씬에 1초 미만의 직렬화 값이 남아 있어도 최소 1초를 보장한다.
        float nextSceneVisibleDuration = Mathf.Max(1f, minimumNextSceneDuration);
        yield return AnimateProgress(
            SceneActivationDisplayedProgress,
            1f,
            nextSceneVisibleDuration);

        if (completedHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(completedHoldDuration);

        loadingPanel?.Hide();
        pendingTransition = null;
        isTransitioning = false;
    }

    private void SaveBeforeTransition(PlayerController player, PendingTransition transition)
    {
        if (!transition.SaveBeforeLoad || SaveData.Instance == null || SaveManager.Instance == null)
            return;

        player?.UpdateSaveData();
        SaveData.Instance.persistent.nextStage = transition.TargetSceneName;
        SaveData.Instance.run.spawnId = transition.TargetSpawnId;

        if (SaveData.Instance.currentSlot > 0)
            SaveManager.Instance.Save(SaveData.Instance.currentSlot);
    }

    private IEnumerator AnimateProgress(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetProgress(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetProgress(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetProgress(to);
    }

    private void ShowLoadingPanel()
    {
        loadingPanel?.Show();
        loadingPanel?.SetProgress(0f);
    }

    private void SetProgress(float progress)
    {
        displayedProgress = Mathf.Clamp01(progress);
        loadingPanel?.SetProgress(displayedProgress);
    }

    private void AbortTransition(PlayerController player, string message)
    {
        Debug.LogError(message, this);
        loadingPanel?.Hide();
        player?.ResumeKeyboardControl();
        pendingTransition = null;
        isTransitioning = false;
        displayedProgress = 0f;
    }
}
