using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MidBossHealthUI : MonoBehaviour
{
    public static MidBossHealthUI Instance;

    [Header("UI")]
    [SerializeField] private GameObject root;
    [SerializeField] private Image hpFill;
    [SerializeField] private TMP_Text bossName;
    [SerializeField] private TMP_Text hpText;

    [Header("Armor UI")]
    [SerializeField] private BossArmorUI armorUI;

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

        float hpPercent = Mathf.Clamp01(currentHp / maxHp);

        hpFill.fillAmount = hpPercent;

        // HP 텍스트 갱신
        UpdateHPText(currentHp);

        // 갑옷 초기화
        if (armorUI != null)
        {
            armorUI.ResetArmor();
            armorUI.UpdateArmor(hpPercent);
        }
    }

    // HP 갱신
    public void UpdateHP(float currentHp)
    {
        if (maxHp <= 0)
            return;

        float hpPercent = Mathf.Clamp01(currentHp / maxHp);

        hpFill.fillAmount = hpPercent;

        // HP 텍스트 갱신
        UpdateHPText(currentHp);

        // 갑옷 상태 갱신
        if (armorUI != null)
            armorUI.UpdateArmor(hpPercent);
    }

    // HP 텍스트 표시
    private void UpdateHPText(float currentHp)
    {
        if (hpText == null)
            return;

        hpText.text = $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)}";
    }


    // HP바 숨김
    public void Hide()
    {
        root.SetActive(false);
    }
}