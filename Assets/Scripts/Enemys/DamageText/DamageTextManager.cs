using UnityEngine;

// DamageTextManager
// - 데미지 텍스트 생성 관리
// - 월드 좌표를 UI 좌표로 변환
// - 데미지 텍스트 프리팹 생성
// - 크리티컬 여부 전달

public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance;
    public GameObject damagePrefab;
    public Canvas canvas;

    void Awake()
    {
        Instance = this;
    }

    /// 데미지 텍스트 생성
    public void ShowDamage(int damage, Vector3 worldPos, bool critical = false)
    {
        // 월드 좌표를 화면 좌표로 변환
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        // 데미지 텍스트 생성
        GameObject obj = Instantiate(damagePrefab, canvas.transform);

        // 생성 위치 설정
        obj.transform.position = screenPos;

        // 데미지 및 크리티컬 정보 전달
        obj.GetComponent<DamageText>().SetDamage(damage, critical);
    }
}