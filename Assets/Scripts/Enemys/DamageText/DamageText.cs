using TMPro;
using UnityEngine;

// - 데미지 텍스트 표시
// - 숫자 설정
// - 랜덤 위치에서 생성
// - 위로 이동
// - 크리티컬 색상/크기 적용
// - 생성 시 확대 후 원래 크기로 복귀
// - 시간에 따라 투명해진 후 자동 삭제

public class DamageText : MonoBehaviour
{
    public TMP_Text damageText;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 1.5f;

    [Header("Lifetime")]
    [SerializeField] float lifeTime = 1f;

    [Header("Scale")]
    [SerializeField] float startScale = 1.3f;
    [SerializeField] float scaleSpeed = 10f;

    Color color;

    /// 데미지 수치 및 크리티컬 여부 설정
    public void SetDamage(int damage, bool critical = false)
    {
        // 데미지 표시
        damageText.text = damage.ToString();

        // 크리티컬 여부에 따라 색상/크기 변경
        if (critical)
        {
            damageText.color = Color.yellow;
            damageText.fontSize = 60;
        }
        else
        {
            damageText.color = Color.white;
            damageText.fontSize = 40;
        }

        // 현재 색상 저장
        color = damageText.color;

        // 생성 위치를 약간 랜덤하게 변경
        transform.position += new Vector3(
            Random.Range(-20f, 20f),
            Random.Range(-5f, 10f),
            0f);

        // 생성 시 살짝 크게 시작
        transform.localScale = Vector3.one * startScale;
    }


    void Update()
    {
        // 위로 이동
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // 원래 크기로 자연스럽게 복귀
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            Vector3.one,
            scaleSpeed * Time.deltaTime);

        // 점점 투명하게 만들기
        color.a -= Time.deltaTime / lifeTime;
        damageText.color = color;

        // 완전히 투명해지면 삭제
        if (color.a <= 0f)
        {
            Destroy(gameObject);
        }
    }
}