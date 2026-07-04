using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class TutorialStepTrigger : MonoBehaviour
{
    [SerializeField] private TutorialStep requiredStep;
    [SerializeField] private TutorialStep nextStep;
    [SerializeField] private bool requireExactStep = true;
    [SerializeField] private bool oneShot = true;

    [Header("Floating Text")]
    [SerializeField] private bool showFloatingText;
    [SerializeField] private string floatingText = "";
    [SerializeField] private TMP_Text floatingTextPrefab;
    [SerializeField] private Vector3 floatingTextOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private float floatingTextFontSize = 4f;
    [SerializeField] private Color floatingTextColor = Color.white;
    [SerializeField] private bool showOnlyOnRequiredStep = true;
    [SerializeField] private float bobAmplitude = 0.15f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private bool faceCamera = true;

    private bool used;
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
        floatingTextInstance.gameObject.SetActive(ShouldShowFloatingText());
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
        if (!showFloatingText || used || string.IsNullOrWhiteSpace(floatingText))
            return false;

        TutorialManager manager = TutorialManager.Instance;
        if (!showOnlyOnRequiredStep)
            return manager == null || manager.IsActive;

        return manager != null && manager.IsActive && manager.CurrentStep == requiredStep;
    }
}
