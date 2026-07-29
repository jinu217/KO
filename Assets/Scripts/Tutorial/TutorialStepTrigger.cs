using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class TutorialStepTrigger : MonoBehaviour
{
    private enum FloatingTextHideMode
    {
        HideWhenTriggered,
        HideWhenNextStepLeaves
    }

    [SerializeField] private TutorialStep requiredStep;
    [SerializeField] private TutorialStep nextStep;
    [SerializeField] private bool requireExactStep = true;
    [SerializeField] private bool oneShot = true;

    [Header("Go Town")]
    [SerializeField] private string townSceneName = "Town";

    [Header("Floating Text")]
    [SerializeField] private bool showFloatingText;
    [SerializeField] private string floatingText = "";
    [SerializeField] private TMP_Text floatingTextPrefab;
    [SerializeField] private Vector3 floatingTextOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private float floatingTextFontSize = 4f;
    [SerializeField] private Color floatingTextColor = Color.white;
    [SerializeField] private bool showOnlyOnRequiredStep = true;
    [SerializeField] private FloatingTextHideMode floatingTextHideMode = FloatingTextHideMode.HideWhenTriggered;
    [SerializeField] private float bobAmplitude = 0.15f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private bool faceCamera = true;
    [SerializeField] private string floatingTextSortingLayer = "Default";
    [SerializeField] private int floatingTextSortingOrder = 100;

    private bool used;
    private bool triggered;
    private TMP_Text floatingTextInstance;
    private Vector3 floatingTextBaseLocalPosition;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        SetupFloatingText();
    }

    private void LateUpdate()
    {
        UpdateFloatingText();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used || !other.CompareTag("Player"))
            return;

        TutorialManager manager = TutorialManager.Instance;
        if (manager == null || !manager.IsActive)
            return;

        if (requireExactStep && manager.CurrentStep != requiredStep)
            return;

        triggered = true;

        if (manager.CurrentStep == TutorialStep.GoTown)
        {
            if (oneShot)
                used = true;

            SceneManager.LoadScene(townSceneName);
            return;
        }

        manager.SetStep(nextStep);

        if (oneShot)
            used = true;
    }

    private void SetupFloatingText()
    {
        if (!showFloatingText)
            return;

        if (floatingTextPrefab != null)
        {
            floatingTextInstance = Instantiate(floatingTextPrefab, transform);
        }
        else
        {
            GameObject textObj = new GameObject("Floating Tutorial Text");
            textObj.transform.SetParent(transform, false);
            floatingTextInstance = textObj.AddComponent<TextMeshPro>();
        }

        floatingTextBaseLocalPosition = floatingTextOffset;
        floatingTextInstance.transform.localPosition = floatingTextBaseLocalPosition;
        floatingTextInstance.text = floatingText;
        floatingTextInstance.fontSize = floatingTextFontSize;
        floatingTextInstance.color = floatingTextColor;
        floatingTextInstance.alignment = TextAlignmentOptions.Center;
        ApplyFloatingTextSorting();
        floatingTextInstance.gameObject.SetActive(ShouldShowFloatingText());
    }

    private void ApplyFloatingTextSorting()
    {
        if (floatingTextInstance == null)
            return;

        Renderer[] renderers = floatingTextInstance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingLayerName = floatingTextSortingLayer;
            renderers[i].sortingOrder = floatingTextSortingOrder;
        }

        Canvas[] canvases = floatingTextInstance.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            canvases[i].overrideSorting = true;
            canvases[i].sortingLayerName = floatingTextSortingLayer;
            canvases[i].sortingOrder = floatingTextSortingOrder;
        }
    }

    private void UpdateFloatingText()
    {
        if (floatingTextInstance == null)
            return;

        floatingTextInstance.gameObject.SetActive(ShouldShowFloatingText());
        if (!floatingTextInstance.gameObject.activeSelf)
            return;

        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        floatingTextInstance.transform.localPosition = floatingTextBaseLocalPosition + Vector3.up * bob;

        if (faceCamera && Camera.main != null)
            floatingTextInstance.transform.rotation = Camera.main.transform.rotation;
    }

    private bool ShouldShowFloatingText()
    {
        if (!showFloatingText || string.IsNullOrWhiteSpace(floatingText))
            return false;

        TutorialManager manager = TutorialManager.Instance;

        if (triggered && floatingTextHideMode == FloatingTextHideMode.HideWhenNextStepLeaves)
            return manager != null && manager.IsActive && manager.CurrentStep == nextStep;

        if (used)
            return false;

        if (!showOnlyOnRequiredStep)
            return manager == null || manager.IsActive;

        return manager != null && manager.IsActive && manager.CurrentStep == requiredStep;
    }
}
