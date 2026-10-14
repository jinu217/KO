
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BossDialogueTrigger : MonoBehaviour
{
    //============== 보스 대화 설정 ==============

    [Header("보스 대화")]
    [SerializeField] private MainBossDialogueController dialogueController;
    [SerializeField] private MainBossController bossController;
    [SerializeField] private string bossId = "Knight1";

    private bool hasTriggered;

    //============== 초기 설정 ==============

    private void Reset()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;
    }

    private void Awake()
    {
        if (bossController == null)
            bossController = GetComponentInParent<MainBossController>();

        if (dialogueController == null)
            dialogueController = FindFirstObjectByType<MainBossDialogueController>();
    }

    //============== 트리거 진입 ==============

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (dialogueController == null)
        {
            Debug.LogError("[BossDialogueTrigger] Dialogue Controller가 연결되지 않았습니다.", this);
            return;
        }

        if (bossController == null)
        {
            Debug.LogError("[BossDialogueTrigger] Boss Controller가 연결되지 않았습니다.", this);
            return;
        }

        bool started = dialogueController.TryPlayEncounter(
            bossController,
            bossId
        );

        if (started)
            hasTriggered = true;
    }
}
