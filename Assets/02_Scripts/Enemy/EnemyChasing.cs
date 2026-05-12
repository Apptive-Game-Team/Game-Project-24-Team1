using UnityEngine;
using UnityEngine.AI;

namespace GameProject24.Enemy
{
    /// <summary>
    /// 적이 Chasing(추격) 상태일 때 작동하는 로직입니다.
    /// 플레이어의 마지막 목격 위치(LPP)를 추적하며, 놓쳤을 경우 일정 시간 동안 주변을 수색합니다.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyStatus))]
    public class EnemyChasing : MonoBehaviour
    {
        private NavMeshAgent _agent;
        private EnemyStatus _enemyStatus;

        [Header("Search Logic")]
        [Tooltip("마지막 발견 위치 도착 후 수색을 진행할 시간")]
        [SerializeField] private float _searchTime = 7f;
        
        [Tooltip("수색 시 목적지를 찾을 시야각(전방 기준 FOV)")]
        [SerializeField] private float _searchFov = 90f;

        private float _searchTimer = 0f;
        private float _entryDelayTimer = 0f;
        private Vector3 _targetPoint;
        private float _speed;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _enemyStatus = GetComponent<EnemyStatus>();
        }

        private void Update()
        {
            // ==========================================
            // 메인 로직 루프 (Update - 매 프레임 실행)
            // ==========================================
            if (_enemyStatus.CurrentState != EnemyStatus.State.Chasing)
            {
                _searchTimer = 0f;
                _entryDelayTimer = 0f;
                return;
            }

            // 추격 진입 시 2초 동안 플레이어를 바라보며 대기
            if (_entryDelayTimer < 2.0f)
            {
                _entryDelayTimer += Time.deltaTime;
                _agent.speed = 0f;
                
                if (_agent.isOnNavMesh && _agent.hasPath)
                {
                    _agent.ResetPath();
                }

                if (_enemyStatus.LatestPlayerPosition.HasValue)
                {
                    Vector3 dir = (_enemyStatus.LatestPlayerPosition.Value - transform.position).normalized;
                    dir.y = 0;
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(dir);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
                    }
                }
                return;
            }

            // 1. 상황 판단 및 목표/속도 설정
            if (_enemyStatus.IsPlayerSpotted)
            {
                // [시야에 있음 : 추격]
                _searchTimer = 0f;

                if (_enemyStatus.LatestPlayerPosition.HasValue)
                {
                    float distToLpp = Vector3.Distance(transform.position, _enemyStatus.LatestPlayerPosition.Value);
                    // 콜라이더 반경 때문에 1.0f 미만으로 좁혀지지 않을 수 있으므로, 명시적인 AttackRange를 사용합니다.
                    if (distToLpp <= _enemyStatus.AttackRadius)
                    {
                        _speed = 0f;
                    }
                    else
                    {
                        _targetPoint = _enemyStatus.LatestPlayerPosition.Value;
                        _speed = _enemyStatus.ChaseSpeed;
                    }
                }
            }
            else
            {
                // [시야에서 놓침 : 수색]
                _searchTimer += Time.deltaTime;

                if (_searchTimer >= _searchTime)
                {
                    _enemyStatus.ChangeState(_enemyStatus.InitialState);
                    return;
                }

                // 현재 위치에서 targetPoint까지 도착했다면
                float distToTarget = Vector3.Distance(transform.position, _targetPoint);
                if (distToTarget <= _agent.stoppingDistance + 0.5f || (_agent.hasPath && !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance))
                {
                    // [새로운 수색 지점 탐색]
                    _speed = _enemyStatus.MoveSpeed; // 수색할 때는 걷는 속도로 변경

                    float randomAngle = Random.Range(-_searchFov * 0.5f, _searchFov * 0.5f);
                    Vector3 randomDir = Quaternion.Euler(0, randomAngle, 0) * transform.forward;
                    Vector3 randomPos = transform.position + randomDir * 5f;

                    if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                    {
                        _targetPoint = hit.position;
                    }
                    else
                    {
                        _targetPoint = randomPos;
                    }
                }
            }

            // 2. 실제 이동 명령 수행 (Update의 마지막)
            if (_speed == 0f)
            {
                _agent.isStopped = true;
            }
            else
            {
                _agent.isStopped = false;
                _agent.speed = _speed;
                _agent.SetDestination(_targetPoint);
            }
        }
    }
}
