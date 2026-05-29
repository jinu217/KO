using TMPro;
using UnityEngine;

public class PotionUI : MonoBehaviour
{
    public PotionBag bag;

    public TMP_Text healText;
    public TMP_Text bpmText;

    void Update()
    {
        healText.text = bag.healPotionCount.ToString();

        bpmText.text = bag.bpmPotionCount.ToString();
    }
}