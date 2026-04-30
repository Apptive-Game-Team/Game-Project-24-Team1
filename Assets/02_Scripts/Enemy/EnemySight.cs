using UnityEngine;

/// <summary>
/// 에너미의 시야를 담당하는 클래스입니다.
/// 플레이어를 Raycast로 탐지하고 EnemyMove에 전달합니다.
/// </summary>
public class EnemySight : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("플레이어를 탐지할 수 있는 거리입니다.")]
    [SerializeField] private float _detectionDistance = 10f;
    
    [Tooltip("에너미의 시야 각도 (부채꼴)입니다.")]
    [Range(0f, 360f)]
    [SerializeField] private float _fieldOfView = 120f;
    
    /// <summary> 플레이어의 Transform을 캐싱합니다. </summary>
    private Transform _playerTransform;
    
    /// <summary> 이동을 제어하는 EnemyMove 컴포넌트입니다. </summary>
    private EnemyMove _enemyMove;

    private void Start()
    {
        _enemyMove = GetComponent<EnemyMove>();
        
        if (_enemyMove == null)
        {
            Debug.LogWarning("EnemySight requires an EnemyMove component on the same GameObject.");
        }

        // 플레이어 캐싱 (태그가 없으면 이름으로 백업 검색)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            playerObj = GameObject.Find("Player");
        }

        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
        }
    }

    private void Update()
    {
        if (_playerTransform == null || _enemyMove == null)
        {
            return;
        }

        Vector3 dirToPlayer = _playerTransform.position - transform.position;
        float distToPlayer = dirToPlayer.magnitude;

        // 1. 거리 확인
        if (distToPlayer <= _detectionDistance)
        {
            float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);

            // 2. 시야각 확인
            if (angleToPlayer <= _fieldOfView * 0.5f)
            {
                // 바닥/장애물 회피를 위해 Y축을 1 높여서 Ray 발사
                Vector3 origin = transform.position + Vector3.up * 1.0f;
                Vector3 targetPos = _playerTransform.position + Vector3.up * 1.0f;
                Vector3 rayDir = (targetPos - origin).normalized;

                // 3. Raycast (시야 방해물 확인, CompareTag 사용)
                if (Physics.Raycast(origin, rayDir, out RaycastHit hit, _detectionDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform.CompareTag("Player") || hit.transform.name == "Player")
                    {
                        _enemyMove.OnPlayerSpotted(_playerTransform);
                    }
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 origin = transform.position + Vector3.up * 1.0f;
        Gizmos.DrawWireSphere(origin, _detectionDistance);

        // 시야각 경계선
        Vector3 leftBoundary = Quaternion.Euler(0, -_fieldOfView * 0.5f, 0) * transform.forward * _detectionDistance;
        Vector3 rightBoundary = Quaternion.Euler(0, _fieldOfView * 0.5f, 0) * transform.forward * _detectionDistance;
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, origin + leftBoundary);
        Gizmos.DrawLine(origin, origin + rightBoundary);

        // 플레이어 탐지 시각화
        if (Application.isPlaying && _playerTransform != null)
        {
            Vector3 dirToPlayer = _playerTransform.position - transform.position;
            if (dirToPlayer.magnitude <= _detectionDistance && Vector3.Angle(transform.forward, dirToPlayer) <= _fieldOfView * 0.5f)
            {
                Vector3 targetPos = _playerTransform.position + Vector3.up * 1.0f;
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(origin, targetPos);
            }
        }
    }
}
