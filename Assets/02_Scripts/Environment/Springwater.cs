using System.Collections;
using UnityEngine;

public class Springwater : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("이동할 거리 (y축)")]
    [SerializeField] private float distance = 20f;
    
    [Tooltip("솟구칠 때 속도")]
    [SerializeField] private float upSpeed = 20f;
    
    [Tooltip("내려갈 때 속도")]
    [SerializeField] private float downSpeed = 3f;
    
    [Tooltip("속도가 천천히 높아질지 여부 (가속)")]
    [SerializeField] private bool useAcceleration = true;

    [Header("Wait Times (Top)")]
    [Tooltip("올라간 위치를 유지할 최소 시간 (a)")]
    [SerializeField] private float minWaitTop = 10f;
    
    [Tooltip("올라간 위치를 유지할 최대 시간 (b)")]
    [SerializeField] private float maxWaitTop = 20f;

    [Header("Wait Times (Bottom)")]
    [Tooltip("내려간 후 위치를 유지할 최소 시간 (c)")]
    [SerializeField] private float minWaitBottom = 5f;
    
    [Tooltip("내려간 후 위치를 유지할 최대 시간 (d)")]
    [SerializeField] private float maxWaitBottom = 10f;

    private Vector3 bottomPosition;
    private Vector3 topPosition;

    private void Start()
    {
        // 시작 위치를 최고점(Top)으로 간주합니다.
        topPosition = transform.position;
        bottomPosition = topPosition - Vector3.up * distance;

        StartCoroutine(SpringwaterRoutine());
    }

    private IEnumerator SpringwaterRoutine()
    {
        // 아래로 내려갔다가 위로 솟구치는 반복
        while (true)
        {
            // 1. 현재 위치(최고점)에서 a~b초 대기
            float waitTop = Random.Range(minWaitTop, maxWaitTop);
            yield return new WaitForSeconds(waitTop);

            // 2. 아래로 출발 (b초 전에 출발에 해당)
            // distance만큼 내려감
            yield return MoveRoutine(topPosition, bottomPosition, downSpeed);

            // 3. 내려간 위치(바닥)에서 c~d초 대기
            float waitBottom = Random.Range(minWaitBottom, maxWaitBottom);
            yield return new WaitForSeconds(waitBottom);

            // 4. 위로 출발 (d초 전에 출발에 해당)
            yield return MoveRoutine(bottomPosition, topPosition, upSpeed);
        }
    }

    private IEnumerator MoveRoutine(Vector3 start, Vector3 end, float speed)
    {
        // 이동에 걸리는 전체 시간 계산 (시간 = 거리 / 속력)
        float duration = distance / speed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (useAcceleration)
            {
                // SmoothStep을 사용하여 처음 출발 시 속도가 천천히 높아지고 끝날 때 부드럽게 감속되도록 처리합니다.
                t = t * t * (3f - 2f * t);
            }

            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        // 오차 보정: 정확한 목표 위치로 설정
        transform.position = end;
    }
}
