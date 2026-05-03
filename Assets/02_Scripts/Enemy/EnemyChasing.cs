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

        private bool _isSearching = false;
        private float _searchTimer = 0f;
        private float _searchDirectionTimer = 0f;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _enemyStatus = GetComponent<EnemyStatus>();
        }

        private void Update()
        {
            // 현 상태가 Chasing이 아닐 경우 관련 플래그 초기화 후 대기
            if (_enemyStatus.CurrentState != EnemyStatus.State.Chasing)
            {
                _isSearching = false;
                _searchTimer = 0f;
                _searchDirectionTimer = 0f;
                return;
            }

            // 시야 검사 결과 가져오기 (EnemySight에서 갱신됨)
            bool canSeePlayer = _enemyStatus.IsPlayerSpotted;

            if (canSeePlayer)
            {
                // 1. 플레이어가 시야에 포착됨 (LPP는 EnemySight에서 이미 갱신됨)
                _isSearching = false;
                _searchTimer = 0f;
                _searchDirectionTimer = 0f;

                // 물리적 도달 검사 (LPP 기준)
                if (_enemyStatus.LatestPlayerPosition.HasValue)
                {
                    float distToPlayer = Vector3.Distance(transform.position, _enemyStatus.LatestPlayerPosition.Value);
                    if (distToPlayer <= _agent.stoppingDistance + 0.5f)
                    {
                        if (!_agent.isStopped)
                        {
                            _agent.isStopped = true;
                            _agent.ResetPath();
                        }
                        // TODO: 도달했을 때 공격 등 추가 로직
                    }
                    else
                    {
                        _agent.isStopped = false;
                        _agent.speed = _enemyStatus.ChaseSpeed;
                        _agent.SetDestination(_enemyStatus.LatestPlayerPosition.Value);
                    }
                }
            }
            else
            {
                // 2. 플레이어를 놓침 (시야에 없음)

                if (_enemyStatus.LatestPlayerPosition.HasValue)
                {
                    // LPP까지의 거리 계산
                    float distToLpp = Vector3.Distance(transform.position, _enemyStatus.LatestPlayerPosition.Value);

                    // LPP에 도달했는지 확인 (거리상 가까워졌거나, 길찾기가 완전히 완료되었을 때)
                    if (distToLpp <= _agent.stoppingDistance + 0.5f || (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance))
                    {
                        // LPP 초기화 및 수색 모드 진입
                        _enemyStatus.LatestPlayerPosition = null;
                        
                        _isSearching = true;
                        _searchTimer = 0f;
                        _searchDirectionTimer = 1f; // 1초 조건에 바로 걸리도록 1로 시작
                    }
                    else
                    {
                        // 아직 LPP에 도달하지 않았으므로, 뛴다(ChaseSpeed)
                        _agent.isStopped = false;
                        _agent.speed = _enemyStatus.ChaseSpeed;
                        _agent.SetDestination(_enemyStatus.LatestPlayerPosition.Value);
                    }
                }
                else
                {
                    // LPP가 null임 == LPP에 도달해서 초기화되었고 수색 중
                    if (_isSearching)
                    {
                        _searchTimer += Time.deltaTime;
                        _searchDirectionTimer += Time.deltaTime;

                        if (_searchTimer >= _searchTime)
                        {
                            // 수색 실패 (시간 초과) -> 이전 상태(ex: Idle, Patrolling)로 복귀
                            _isSearching = false;
                            _searchTimer = 0f;
                            _searchDirectionTimer = 0f;
                            _enemyStatus.ChangeState(_enemyStatus.PreviousState);
                            return;
                        }

                        // 1초마다 랜덤 각도 방향으로 새 이동 명령
                        if (_searchDirectionTimer >= 1f)
                        {
                            _searchDirectionTimer = 0f;
                            StartDirectionalSearch();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 수색 중 _searchFov 내의 랜덤 각도 방향으로 임의의 거리를 이동합니다.
        /// 매 1초마다 반복 호출됩니다.
        /// </summary>
        private void StartDirectionalSearch()
        {
            // 전방을 기준으로 좌/우 _searchFov 내의 임의의 각도 계산
            float randomAngle = Random.Range(-_searchFov * 0.5f, _searchFov * 0.5f);
            Vector3 randomDir = Quaternion.Euler(0, randomAngle, 0) * transform.forward;
            
            // 너무 멀지 않은 위치를 찍어줍니다. (수색이니까)
            float searchDist = 5f;
            Vector3 randomPos = transform.position + randomDir * searchDist;

            // 해당 방향에서 뻗어나간 위치 중 가장 가까운 유효한 NavMesh 지점 찾기
            if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, searchDist, NavMesh.AllAreas))
            {
                _agent.isStopped = false;
                // 수색할 때는 원래 속도(걷기 속도)로 탐색합니다.
                _agent.speed = _enemyStatus.MoveSpeed; 
                _agent.SetDestination(hit.position);
            }
        }

        /// <summary>
        /// 추격 중에도 매 프레임 플레이어가 보이는지 검사하기 위한 함수입니다.
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            // 닿았을 때의 추가 안전장치
            if (_enemyStatus.CurrentState == EnemyStatus.State.Chasing)
            {
                if (collision.transform.CompareTag("Player"))
                {
                    if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
                    {
                        _agent.isStopped = true;
                        _agent.ResetPath();
                    }
                }
            }
        }
    }
}
