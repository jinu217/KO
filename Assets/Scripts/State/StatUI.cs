using TMPro;
using UnityEngine;

public class StatUI : MonoBehaviour
{
    public EquipmentManager equipment;

    public TMP_Text attack;
    public TMP_Text attackSpeed;
    public TMP_Text critChance;
    public TMP_Text critDamage;
    public TMP_Text moveSpeed;
    public TMP_Text defense;
    public TMP_Text hp;

    void Start()
    {
        equipment.onStatsChanged += UpdateUI;
        UpdateUI();
    }

    public void UpdateUI()
    {
        PlayerController playerController = equipment.GetComponent<PlayerController>();
        PlayerStats s = playerController != null
            ? playerController.Stats
            : equipment.stats;

        attack.text = "공격력 : " + s.attackDamage;
        attackSpeed.text = "공격속도 : " + s.attackSpeed;

        critChance.text = "크리확률 : " + (s.critChance * 100f) + "%";
        critDamage.text = "크리데미지 : " + (s.critDamage * 100f) + "%";

        moveSpeed.text = "이동속도 : " + s.moveSpeed;

        defense.text = "방어력 : " + s.defense;
        hp.text = "체력 : " + s.maxHp;
    }
}
