using UnityEngine;

namespace GameProject24.Enemy
{
    /// <summary>
    /// 에너미의 시야를 담당하는 클래스입니다.
    /// 플레이어를 Raycast로 탐지하고 EnemyStatus의 상태를 Chasing으로 변경합니다.
    /// </summary>
    [RequireComponent(typeof(EnemyStatus))]
    public class EnemySight : MonoBehaviour
    {
        /// <summary> 플레이어의 Transform을 캐싱합니다. </summary>
        private Transform _playerTransform;
        
        /// <summary> 상태를 제어하는 EnemyStatus 컴포넌트입니다. </summary>
        private EnemyStatus _enemyStatus;

        private void Start()
        {
            _enemyStatus = GetComponent<EnemyStatus>();
            
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
            if (_playerTransform == null || _enemyStatus == null)
            {
                return;
            }

            // 사망한 상태면 탐지 로직 생략 (성능 최적화)
            if (_enemyStatus.CurrentState == EnemyStatus.State.Dead)
            {
                _enemyStatus.IsPlayerSpotted = false;
                return;
            }

            bool canSee = false;

            Vector3 dirToPlayer = _playerTransform.position - transform.position;
            float distToPlayer = dirToPlayer.magnitude;

            // EnemyStatus에 통합된 시야 변수들을 활용
            float detectionDist = _enemyStatus.SightDistance;
            float fov = _enemyStatus.FieldOfView;

            // 1. 거리 확인
            if (distToPlayer <= detectionDist)
            {
                float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);

                // 2. 시야각 확인
                if (angleToPlayer <= fov * 0.5f)
                {
                    // 바닥/장애물 회피를 위해 Y축을 1 높여서 Ray 발사
                    Vector3 origin = transform.position + Vector3.up * 1.0f;
                    Vector3 targetPos = _playerTransform.position + Vector3.up * 1.0f;
                    Vector3 rayDir = (targetPos - origin).normalized;

                    // 3. Raycast (시야 방해물 확인, CompareTag 사용)
                    if (Physics.Raycast(origin, rayDir, out RaycastHit hit, detectionDist, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.transform.CompareTag("Player"))
                        {
                            canSee = true;
                            
                            // 플레이어의 현재 위치로 LPP 지속 갱신
                            _enemyStatus.LatestPlayerPosition = _playerTransform.position;

                            // Chasing 상태가 아니라면 Chasing으로 전환
                            if (_enemyStatus.CurrentState != EnemyStatus.State.Chasing)
                            {
                                _enemyStatus.ChangeState(EnemyStatus.State.Chasing);
                            }
                        }
                    }
                }
            }

            // 이번 프레임의 시야 확인 결과를 업데이트
            _enemyStatus.IsPlayerSpotted = canSee;
        }

        private void OnDrawGizmosSelected()
        {
            EnemyStatus status = GetComponent<EnemyStatus>();
            if (status == null) return;

            float detectionDist = status.SightDistance;
            float fov = status.FieldOfView;

            // Gizmos.color = Color.red;
            Vector3 origin = transform.position + Vector3.up * 1.0f;
            // Gizmos.DrawWireSphere(origin, detectionDist);

            // 시야각 경계선 (수평 좌/우)
            Vector3 leftBoundary = Quaternion.AngleAxis(-fov * 0.5f, transform.up) * transform.forward * detectionDist;
            Vector3 rightBoundary = Quaternion.AngleAxis(fov * 0.5f, transform.up) * transform.forward * detectionDist;
            
            // 시야각 경계선 (수직 상/하)
            Vector3 topBoundary = Quaternion.AngleAxis(-fov * 0.5f, transform.right) * transform.forward * detectionDist;
            Vector3 bottomBoundary = Quaternion.AngleAxis(fov * 0.5f, transform.right) * transform.forward * detectionDist;
            
            Gizmos.color = Color.cyan;
            
            // 중심에서 각 모서리로 뻗어나가는 선
            Gizmos.DrawLine(origin, origin + leftBoundary);
            Gizmos.DrawLine(origin, origin + rightBoundary);
            Gizmos.DrawLine(origin, origin + topBoundary);
            Gizmos.DrawLine(origin, origin + bottomBoundary);
            
            // 시야 끝부분을 이어주는 다이아몬드 형태 테두리
            Gizmos.DrawLine(origin + leftBoundary, origin + topBoundary);
            Gizmos.DrawLine(origin + topBoundary, origin + rightBoundary);
            Gizmos.DrawLine(origin + rightBoundary, origin + bottomBoundary);
            Gizmos.DrawLine(origin + bottomBoundary, origin + leftBoundary);

            // 플레이어 탐지 시각화
            if (Application.isPlaying && _playerTransform != null)
            {
                Vector3 dirToPlayer = _playerTransform.position - transform.position;
                if (dirToPlayer.magnitude <= detectionDist && Vector3.Angle(transform.forward, dirToPlayer) <= fov * 0.5f)
                {
                    Vector3 targetPos = _playerTransform.position + Vector3.up * 1.0f;
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(origin, targetPos);
                }
            }
        }
    }
}
