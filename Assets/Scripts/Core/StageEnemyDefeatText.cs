using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class StageEnemyDefeatText : MonoBehaviour
{
    private TMP_Text counterText;
    private int displayedCount = -1;

    private void Awake()
    {
        counterText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        int count = Mathf.Min(StageClearManager.GetDefeatedEnemyCount(gameObject.scene), StageClearManager.RequiredEnemyDefeats);
        if (count == displayedCount)
            return;

        displayedCount = count;
        counterText.text = $"{count}/{StageClearManager.RequiredEnemyDefeats}";
    }
}
