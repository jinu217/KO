using UnityEngine;

public class BackgroundLoop : MonoBehaviour
{
    [Header("카메라")]
    public Transform cameraTransform;

    [Header("배경 4개")]
    public Transform[] backgrounds;

    private float width;

    void Start()
    {
        if (backgrounds.Length < 4)
        {
            Debug.LogError("배경은 최소 4개가 필요합니다.");
            return;
        }

        width = backgrounds[0].GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void Update()
    {
        if (cameraTransform == null)
            return;

        MoveRight();
        MoveLeft();
    }

    // 오른쪽으로 이동
    void MoveRight()
    {
        Transform leftmost = GetLeftmost();
        Transform rightmost = GetRightmost();

        // 카메라가 가장 왼쪽 배경을 지나가면
        if (cameraTransform.position.x >
            leftmost.position.x + width)
        {
            leftmost.position = new Vector3(
                rightmost.position.x + width,
                leftmost.position.y,
                leftmost.position.z
            );
        }
    }

    // 왼쪽으로 이동
    void MoveLeft()
    {
        Transform leftmost = GetLeftmost();
        Transform rightmost = GetRightmost();

        // 카메라가 가장 오른쪽 배경을 지나가면
        if (cameraTransform.position.x <
            rightmost.position.x - width)
        {
            rightmost.position = new Vector3(
                leftmost.position.x - width,
                rightmost.position.y,
                rightmost.position.z
            );
        }
    }

    // 가장 왼쪽 배경 찾기
    Transform GetLeftmost()
    {
        Transform result = backgrounds[0];

        foreach (Transform background in backgrounds)
        {
            if (background.position.x < result.position.x)
            {
                result = background;
            }
        }

        return result;
    }

    // 가장 오른쪽 배경 찾기
    Transform GetRightmost()
    {
        Transform result = backgrounds[0];

        foreach (Transform background in backgrounds)
        {
            if (background.position.x > result.position.x)
            {
                result = background;
            }
        }

        return result;
    }
}