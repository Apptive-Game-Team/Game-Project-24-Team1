using UnityEngine;

public class WaterController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("수위가 변화하는 속도")]
    [SerializeField] private float waterChangeSpeed = 1f;

    // 내부적으로 이동해야 할 목표 Y 좌표
    private float targetY;

    private void Start()
    {
        // 시작 시 현재 Y 좌표를 목표 좌표로 설정
        targetY = transform.position.y;
    }

    private void Update()
    {
        // 현재 Y 좌표와 목표 Y 좌표가 다르면 부드럽게 이동
        if (Mathf.Abs(transform.position.y - targetY) > 0.001f)
        {
            float newY = Mathf.MoveTowards(transform.position.y, targetY, waterChangeSpeed * Time.deltaTime);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    /// <summary>
    /// 수위를 파라미터(height)만큼 올립니다.
    /// </summary>
    /// <param name="height">올릴 수위량</param>
    public void RaiseWater(float height)
    {
        targetY += height;
    }

    /// <summary>
    /// 수위를 파라미터(height)만큼 내립니다.
    /// </summary>
    /// <param name="height">내릴 수위량</param>
    public void LowerWater(float height)
    {
        targetY -= height;
    }
}
