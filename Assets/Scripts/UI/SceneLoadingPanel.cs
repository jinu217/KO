using UnityEngine;
using UnityEngine.UI;

public class SceneLoadingPanel : MonoBehaviour
{
    private static SceneLoadingPanel persistentInstance;

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image progressFill;
    [SerializeField] private bool keepAcrossScenes = true;

    private void Awake()
    {
        if (keepAcrossScenes && persistentInstance != null && persistentInstance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (panelRoot == null)
            panelRoot = gameObject;

        if (keepAcrossScenes)
        {
            persistentInstance = this;
            DontDestroyOnLoad(gameObject);
        }

        SceneTransitionManager.GetOrCreate().RegisterLoadingPanel(this);
    }

    private void OnDestroy()
    {
        if (persistentInstance == this)
            persistentInstance = null;

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.UnregisterLoadingPanel(this);
    }

    public void Show()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void HideImmediate()
    {
        Hide();
    }

    public void SetProgress(float normalizedProgress)
    {
        float progress = Mathf.Clamp01(normalizedProgress);

        if (progressFill != null)
            progressFill.fillAmount = progress;
    }
}
