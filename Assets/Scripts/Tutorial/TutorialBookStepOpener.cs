using System.Collections.Generic;
using UnityEngine;

public class TutorialBookStepOpener : MonoBehaviour
{
    [System.Serializable]
    public class StepBookPage
    {
        [Tooltip("이 튜토리얼 단계가 되었을 때 책을 엽니다.")]
        public TutorialStep step;

        [Tooltip("열 튜토리얼 북 페이지 번호입니다. 0이 첫 번째 메뉴입니다.")]
        public int pageIndex;

        [Tooltip("켜면 해당 스텝에서 한 번만 자동으로 열립니다.")]
        public bool openOnlyOnce = true;
    }

    [SerializeField] private TutorialBookUI bookUI;
    [SerializeField] private TutorialNPCSpeaker npcSpeaker;
    [SerializeField] private StepBookPage[] stepPages;

    private static readonly List<TutorialBookStepOpener> instances = new List<TutorialBookStepOpener>();

    private readonly HashSet<int> openedIndices = new HashSet<int>();
    private StepBookPage pendingFocusPage;
    private TutorialStep pendingFocusStep;
    private bool waitingForBookClose;

    public static bool ShouldDelayNpcForStep(TutorialStep step, TutorialNPCSpeaker speaker)
    {
        foreach (TutorialBookStepOpener opener in instances)
        {
            if (opener == null || !opener.enabled || !opener.HasStepPage(step))
                continue;

            return speaker != null && !speaker.HasFocusForStep(step);
        }

        return false;
    }

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        if (!instances.Contains(this))
            instances.Add(this);

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.onStepChanged.AddListener(HandleStepChanged);
    }

    private void Start()
    {
        CacheReferences();

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.onStepChanged.RemoveListener(HandleStepChanged);
            TutorialManager.Instance.onStepChanged.AddListener(HandleStepChanged);
            HandleStepChanged(TutorialManager.Instance.CurrentStep);
        }
    }

    private void OnDisable()
    {
        instances.Remove(this);

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.onStepChanged.RemoveListener(HandleStepChanged);

        if (npcSpeaker != null)
            npcSpeaker.CameraFocusFinished -= HandleNpcFocusFinished;

        if (bookUI != null)
            bookUI.Closed -= HandleBookClosedForNpcStart;
    }

    private void HandleStepChanged(TutorialStep step)
    {
        CacheReferences();

        if (bookUI == null || stepPages == null)
            return;

        for (int i = 0; i < stepPages.Length; i++)
        {
            StepBookPage stepPage = stepPages[i];
            if (stepPage == null || stepPage.step != step)
                continue;

            if (stepPage.openOnlyOnce && openedIndices.Contains(i))
                return;

            openedIndices.Add(i);
            OpenWithTiming(step, stepPage);
            return;
        }
    }

    private void OpenWithTiming(TutorialStep step, StepBookPage stepPage)
    {
        if (npcSpeaker != null && npcSpeaker.HasFocusForStep(step))
        {
            pendingFocusStep = step;
            pendingFocusPage = stepPage;
            npcSpeaker.CameraFocusFinished -= HandleNpcFocusFinished;
            npcSpeaker.CameraFocusFinished += HandleNpcFocusFinished;
            return;
        }

        waitingForBookClose = true;
        bookUI.Closed -= HandleBookClosedForNpcStart;
        bookUI.Closed += HandleBookClosedForNpcStart;
        bookUI.OpenAt(stepPage.pageIndex);
    }

    private void HandleNpcFocusFinished(TutorialStep step)
    {
        if (pendingFocusPage == null || step != pendingFocusStep)
            return;

        npcSpeaker.CameraFocusFinished -= HandleNpcFocusFinished;
        bookUI.OpenAt(pendingFocusPage.pageIndex);
        pendingFocusPage = null;
    }

    private void HandleBookClosedForNpcStart()
    {
        if (!waitingForBookClose)
            return;

        waitingForBookClose = false;
        bookUI.Closed -= HandleBookClosedForNpcStart;

        if (TutorialManager.Instance != null && npcSpeaker != null)
            npcSpeaker.PlayStepNow(TutorialManager.Instance.CurrentStep);
    }

    private bool HasStepPage(TutorialStep step)
    {
        if (stepPages == null)
            return false;

        foreach (StepBookPage stepPage in stepPages)
        {
            if (stepPage != null && stepPage.step == step)
                return true;
        }

        return false;
    }

    private void CacheReferences()
    {
        if (bookUI == null)
            bookUI = FindBookUIAnyState();

        if (npcSpeaker == null)
            npcSpeaker = FindFirstObjectByType<TutorialNPCSpeaker>();
    }

    private TutorialBookUI FindBookUIAnyState()
    {
        TutorialBookUI found = FindFirstObjectByType<TutorialBookUI>();
        if (found != null)
            return found;

        TutorialBookUI[] all = Resources.FindObjectsOfTypeAll<TutorialBookUI>();
        foreach (TutorialBookUI ui in all)
        {
            if (ui == null || !ui.gameObject.scene.IsValid())
                continue;

            return ui;
        }

        return null;
    }
}
