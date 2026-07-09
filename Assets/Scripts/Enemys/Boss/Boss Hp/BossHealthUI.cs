using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthUI : MonoBehaviour
{
    public static BossHealthUI Instance;

    [SerializeField] private GameObject root;
    [SerializeField] private Image hpFill;
    [SerializeField] private TMP_Text bossName;

    private float maxHp;

    private void Awake()
    {
        Instance = this;
        root.SetActive(false);
    }

    // HP바 표시
    public void Show(string name, float currentHp, float maxHp)
    {
        root.SetActive(true);

        this.maxHp = maxHp;

        bossName.text = name;
        hpFill.fillAmount = currentHp / maxHp;
    }

    // HP 갱신
    public void UpdateHP(float currentHp)
    {
        hpFill.fillAmount = currentHp / maxHp;
    }

    // HP바 숨김
    public void Hide()
    {
        root.SetActive(false);
    }
}