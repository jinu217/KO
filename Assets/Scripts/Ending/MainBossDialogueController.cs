
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class MainBossDialogueController : MonoBehaviour
{
    //============== 대화 UI ==============
    [Header("기존 NPC 대화 UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("대화 중 숨길 UI")]
    [Tooltip("코인/영혼, HP/SP/BPM, 포션 가방 등 대화 중 숨길 UI 오브젝트를 연결합니다.")]
    [SerializeField] private GameObject[] hudObjectsToHide;

    //============== 플레이어 제어 ==============
    [Header("플레이어")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Rigidbody2D playerBody;

    //============== 카메라 ==============
    [Header("카메라 연출")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private CameraFollow2D cameraFollow;
    [SerializeField] private Vector3 cameraFocusOffset = new Vector3(0f, 1f, -10f);
    [SerializeField, Min(0.1f)] private float cameraZoomSize = 5f;
    [SerializeField, Min(0f)] private float cameraBlendTime = 0.35f;

        //============== 대사 설정 ==============
    [Header("대사 설정")]
    [SerializeField] private string csvResourcePath = "BossDialogue/MainBossDialogue";
    [SerializeField, Min(0f)] private float typeSpeed = 0.12f;

    [Tooltip("각 대사 출력이 끝난 뒤 다음 대사로 넘어가기 전까지 기다리는 시간(초)")]
    [SerializeField, Min(0f)] private float lineHoldTime = 2f;

    [Tooltip("마지막 대사 출력이 끝난 뒤 대사창이 사라지기 전까지 기다리는 시간(초)")]
    [SerializeField, Min(0f)] private float lastLineHoldTime = 2f;

    [Tooltip("카메라가 보스를 확대한 뒤, 대사창이 나타나기 전까지 기다리는 시간(초)")]
    [SerializeField, Min(0f)] private float panelShowDelay = 1f;

    private readonly List<DialogueLine> allLines = new List<DialogueLine>();

    private bool isPlaying;
    private bool csvLoaded;

    private class DialogueLine
    {
        public string bossId;
        public string phase;
        public int order;
        public string characterName;
        public string dialogue;
    }

    private class CameraSnapshot
    {
        public Vector3 position;
        public float orthographicSize;
        public bool followWasEnabled;
    }

    private bool[] hudWasActive;
    private float originalGravityScale;
    private bool playerGravityCached;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (cameraFollow == null && targetCamera != null)
            cameraFollow = targetCamera.GetComponent<CameraFollow2D>();

        if (playerController == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerController = player.GetComponent<PlayerController>();

                if (playerBody == null)
                    playerBody = player.GetComponent<Rigidbody2D>();
            }
        }

        if (playerBody == null && playerController != null)
            playerBody = playerController.GetComponent<Rigidbody2D>();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        LoadCsv();
    }

    //============== 외부 호출: 조우 대화 ==============

    public bool TryPlayEncounter(MainBossController boss, string bossId)
    {
        if (isPlaying || boss == null)
            return false;

        List<DialogueLine> lines = GetLines(bossId, "Encounter");

        if (lines.Count == 0)
            return false;

        isPlaying = true;
        boss.SetDialoguePaused(true);

        StartCoroutine(PlayDialogueRoutine(boss, lines, true));
        return true;
    }

    //============== 외부 호출: 처치 후 대화 ==============

    public IEnumerator PlayAfterDefeat(MainBossController boss, string bossId)
    {
        if (boss == null)
            yield break;

        List<DialogueLine> lines = GetLines(bossId, "AfterDefeat");

        if (lines.Count == 0)
            yield break;

        // 다른 대화가 진행 중이면 먼저 끝날 때까지 기다립니다.
        while (isPlaying)
            yield return null;

        isPlaying = true;

        yield return PlayDialogueRoutine(boss, lines, false);
    }

    //============== 대화 진행 ==============
    private IEnumerator PlayDialogueRoutine(
        MainBossController boss,
        List<DialogueLine> lines,
        bool resumeBossAfterDialogue)
    {
        //============== 대화 시작: 플레이어/HUD/카메라 ==============
        CameraSnapshot cameraSnapshot = CaptureCamera();
        HideHud();
        LockPlayer();

        yield return MoveCameraToBoss(boss != null ? boss.transform : null);

        //============== 대사창 표시 전 대기 ==============
        if (panelShowDelay > 0f)
            yield return new WaitForSecondsRealtime(panelShowDelay);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        //============== 대사 출력 ==============
        for (int i = 0; i < lines.Count; i++)
        {
            DialogueLine line = lines[i];

            if (characterNameText != null)
                characterNameText.text = line.characterName;

            if (dialogueText != null)
                yield return TypeLine(line.dialogue);
            else
                yield return new WaitForSecondsRealtime(1f);

            // 마지막 대사는 더 오래 보여준 뒤 대사창을 닫습니다.
            bool isLastLine = (i == lines.Count - 1);
            yield return new WaitForSecondsRealtime(isLastLine ? lastLineHoldTime : lineHoldTime);
        }

        //============== 대화 종료 ==============
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        yield return RestoreCamera(cameraSnapshot);
        RestoreHud();
        UnlockPlayer();

        // 조우 대화가 끝나면 보스를 다시 움직이게 하고, 미뤄 둔 감지 처리를 실행합니다.
        // 처치 후 대화(resumeBossAfterDialogue == false)에서는 호출하지 않습니다.
        if (resumeBossAfterDialogue && boss != null)
        {
            boss.SetDialoguePaused(false);
            boss.OnEncounterDialogueFinished();
        }

        isPlaying = false;
    }

    private IEnumerator TypeLine(string line)
    {
        dialogueText.text = "";

        for (int i = 0; i < line.Length; i++)
        {
            dialogueText.text = line.Substring(0, i + 1);

            if (typeSpeed > 0f)
                yield return new WaitForSecondsRealtime(typeSpeed);
            else
                yield return null;
        }
    }

    //============== 플레이어 제어 ==============

    private void LockPlayer()
    {
        if (playerController != null)
            playerController.PauseKeyboardControl();

        if (playerBody != null)
        {
            originalGravityScale = playerBody.gravityScale;
            playerGravityCached = true;

            playerBody.linearVelocity = Vector2.zero;
            playerBody.angularVelocity = 0f;
            playerBody.gravityScale = 0f;
        }
    }

    private void UnlockPlayer()
    {
        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector2.zero;
            playerBody.angularVelocity = 0f;

            if (playerGravityCached)
                playerBody.gravityScale = originalGravityScale;
        }

        playerGravityCached = false;

        if (playerController != null)
            playerController.ResumeKeyboardControl();
    }

    //============== HUD 표시/복구 ==============

    private void HideHud()
    {
        if (hudObjectsToHide == null)
            return;

        hudWasActive = new bool[hudObjectsToHide.Length];

        for (int i = 0; i < hudObjectsToHide.Length; i++)
        {
            GameObject obj = hudObjectsToHide[i];

            if (obj == null)
                continue;

            hudWasActive[i] = obj.activeSelf;
            obj.SetActive(false);
        }
    }

    private void RestoreHud()
    {
        if (hudObjectsToHide == null || hudWasActive == null)
            return;

        for (int i = 0; i < hudObjectsToHide.Length; i++)
        {
            if (hudObjectsToHide[i] != null)
                hudObjectsToHide[i].SetActive(hudWasActive[i]);
        }

        hudWasActive = null;
    }

    //============== 카메라 제어 ==============

    private CameraSnapshot CaptureCamera()
    {
        if (targetCamera == null)
            return null;

        CameraSnapshot snapshot = new CameraSnapshot
        {
            position = targetCamera.transform.position,
            orthographicSize = targetCamera.orthographicSize,
            followWasEnabled = cameraFollow != null && cameraFollow.enabled
        };

        if (cameraFollow != null)
            cameraFollow.enabled = false;

        return snapshot;
    }

    private IEnumerator MoveCameraToBoss(Transform bossTransform)
    {
        if (targetCamera == null || bossTransform == null)
            yield break;

        Vector3 startPosition = targetCamera.transform.position;
        float startSize = targetCamera.orthographicSize;

        Vector3 targetPosition = bossTransform.position + cameraFocusOffset;
        targetPosition.z = cameraFocusOffset.z;

        float duration = Mathf.Max(0.01f, cameraBlendTime);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            float size = Mathf.Lerp(startSize, cameraZoomSize, t);
            Vector3 position = Vector3.Lerp(startPosition, targetPosition, t);

            position = ClampCamera(position, size);
            targetCamera.orthographicSize = size;
            targetCamera.transform.position = position;

            yield return null;
        }

        targetCamera.orthographicSize = cameraZoomSize;
        targetCamera.transform.position = ClampCamera(targetPosition, cameraZoomSize);
    }

    private IEnumerator RestoreCamera(CameraSnapshot snapshot)
    {
        if (targetCamera == null || snapshot == null)
        {
            RestoreCameraFollow(snapshot);
            yield break;
        }

        Vector3 targetPosition = snapshot.position;

        // 플레이어가 대화 중 이동하지 않으므로 현재 카메라의 원래 위치로 복귀합니다.
        float startSize = targetCamera.orthographicSize;
        Vector3 startPosition = targetCamera.transform.position;
        float duration = Mathf.Max(0.01f, cameraBlendTime);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            float size = Mathf.Lerp(startSize, snapshot.orthographicSize, t);
            Vector3 position = Vector3.Lerp(startPosition, targetPosition, t);

            position = ClampCamera(position, size);
            targetCamera.orthographicSize = size;
            targetCamera.transform.position = position;

            yield return null;
        }

        targetCamera.orthographicSize = snapshot.orthographicSize;
        targetCamera.transform.position = ClampCamera(targetPosition, snapshot.orthographicSize);

        RestoreCameraFollow(snapshot);
    }

    private void RestoreCameraFollow(CameraSnapshot snapshot)
    {
        if (cameraFollow != null && snapshot != null)
            cameraFollow.enabled = snapshot.followWasEnabled;
    }

    private Vector3 ClampCamera(Vector3 position, float orthographicSize)
    {
        if (cameraFollow != null)
            return cameraFollow.ClampToBounds(position, orthographicSize);

        return position;
    }

    //============== CSV 로드 및 파싱 ==============

    private void LoadCsv()
    {
        allLines.Clear();

        TextAsset csv = Resources.Load<TextAsset>(csvResourcePath);

        if (csv == null)
        {
            Debug.LogError(
                $"[MainBossDialogueController] CSV를 찾을 수 없습니다: " +
                $"Assets/Resources/{csvResourcePath}.csv", this);

            csvLoaded = false;
            return;
        }

        List<List<string>> rows = ParseCsv(csv.text);

        if (rows.Count < 2)
        {
            Debug.LogWarning("[MainBossDialogueController] CSV에 대사가 없습니다.", this);
            csvLoaded = true;
            return;
        }

        Dictionary<string, int> columns = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < rows[0].Count; i++)
            columns[rows[0][i].Trim()] = i;

        if (!columns.ContainsKey("bossId")
            || !columns.ContainsKey("phase")
            || !columns.ContainsKey("order")
            || !columns.ContainsKey("characterName")
            || !columns.ContainsKey("dialogue"))
        {
            Debug.LogError(
                "[MainBossDialogueController] CSV 헤더에 " +
                "bossId, phase, order, characterName, dialogue가 필요합니다.", this);

            csvLoaded = false;
            return;
        }

        for (int i = 1; i < rows.Count; i++)
        {
            List<string> row = rows[i];

            string bossId = GetColumn(row, columns, "bossId").Trim();
            string phase = GetColumn(row, columns, "phase").Trim();
            string orderText = GetColumn(row, columns, "order").Trim();
            string characterName = GetColumn(row, columns, "characterName").Trim();
            string dialogue = GetColumn(row, columns, "dialogue").Trim();

            if (string.IsNullOrEmpty(bossId)
                || string.IsNullOrEmpty(phase)
                || string.IsNullOrEmpty(dialogue))
                continue;

            if (!int.TryParse(orderText, out int order))
                order = i;

            allLines.Add(new DialogueLine
            {
                bossId = bossId,
                phase = phase,
                order = order,
                characterName = characterName,
                dialogue = dialogue
            });
        }

        csvLoaded = true;
        Debug.Log($"[MainBossDialogueController] CSV 대사 {allLines.Count}개 로드 완료.", this);
    }

    private string GetColumn(
        List<string> row,
        Dictionary<string, int> columns,
        string columnName)
    {
        if (!columns.TryGetValue(columnName, out int index))
            return "";

        if (index < 0 || index >= row.Count)
            return "";

        return row[index];
    }

    private List<DialogueLine> GetLines(string bossId, string phase)
    {
        List<DialogueLine> result = new List<DialogueLine>();

        if (!csvLoaded)
            LoadCsv();

        foreach (DialogueLine line in allLines)
        {
            if (string.Equals(line.bossId, bossId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(line.phase, phase, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(line);
            }
        }

        result.Sort((a, b) => a.order.CompareTo(b.order));
        return result;
    }

    private List<List<string>> ParseCsv(string text)
    {
        List<List<string>> rows = new List<List<string>>();
        List<string> row = new List<string>();
        StringBuilder field = new StringBuilder();
        bool insideQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '"')
            {
                if (insideQuotes && i + 1 < text.Length && text[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (c == ',' && !insideQuotes)
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if ((c == '\n' || c == '\r') && !insideQuotes)
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    i++;

                row.Add(field.ToString());
                field.Clear();

                if (row.Exists(value => !string.IsNullOrWhiteSpace(value)))
                    rows.Add(row);

                row = new List<string>();
            }
            else
            {
                field.Append(c);
            }
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());

            if (row.Exists(value => !string.IsNullOrWhiteSpace(value)))
                rows.Add(row);
        }

        return rows;
    }
}
