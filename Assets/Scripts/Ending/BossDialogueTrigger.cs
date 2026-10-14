using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BossDialogueTrigger : MonoBehaviour
{
    //============== 보스 연결 ==============

    [Header("보스 대화")]
    [Tooltip("조우 대화를 시작할 메인보스. 비워두면 부모에서 찾습니다.")]
    [SerializeField] private MainBossController bossController;

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
    }

    //============== 트리거 진입 ==============

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (bossController == null)
        {
            Debug.LogError("[BossDialogueTrigger] Boss Controller가 연결되지 않았습니다.", this);
            return;
        }

        // 조우 대화의 중복 판단과 bossId는 보스가 담당합니다.
        if (bossController.TryStartEncounterDialogue())
            hasTriggered = true;
    }
}