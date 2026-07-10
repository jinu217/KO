using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MidBossHealthUI : MonoBehaviour
{
    public static MidBossHealthUI Instance;

    [SerializeField] private GameObject root;
    [SerializeField] private Image hpFill;
    [SerializeField] private TMP_Text bossName;

    private float maxHp;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (root == null)
            root = gameObject;

        root.SetActive(false);
    }

    // HP바 표시
    public void Show(string name, float currentHp, float maxHp)
    {
        root.SetActive(true);

        this.maxHp = maxHp;
        bossName.text = name;
        hpFill.fillAmount = Mathf.Clamp01(currentHp / maxHp);
    }

    // HP 갱신
    public void UpdateHP(float currentHp)
    {
        if (maxHp <= 0) return;

        hpFill.fillAmount = Mathf.Clamp01(currentHp / maxHp);
    }

    // HP바 숨김
    public void Hide()
    {
        root.SetActive(false);
    }
}