// ============================================================
// [참고용 스크립트 - 컴파일에서 제외됨]
// 이 파일은 리팩토링 이전의 EnemyChasing 원본입니다. (커밋: 011a184)
// 기능 비교 참조 목적으로만 보관합니다. 절대 실제 코드와 연계하지 마세요.
// ============================================================
#if false
using UnityEngine;
using UnityEngine.AI;

namespace GameProject24.Enemy
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyStatus))]
    public class EnemyChasingLegacy : MonoBehaviour
    {
        private NavMeshAgent _agent;
        private EnemyStatus _enemyStatus;

        [Header("Search Logic")]
        [SerializeField] private float _searchTime = 7f;
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
            if (_enemyStatus.CurrentState != EnemyStatus.State.Chasing)
            {
                _isSearching = false;
                _searchTimer = 0f;
                _searchDirectionTimer = 0f;
                return;
            }

            bool canSeePlayer = _enemyStatus.IsPlayerSpotted;

            if (canSeePlayer)
            {
                _isSearching = false;
                _searchTimer = 0f;
                _searchDirectionTimer = 0f;

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
                if (_enemyStatus.LatestPlayerPosition.HasValue)
                {
                    float distToLpp = Vector3.Distance(transform.position, _enemyStatus.LatestPlayerPosition.Value);

                    if (distToLpp <= _agent.stoppingDistance + 0.5f || (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance))
                    {
                        _enemyStatus.LatestPlayerPosition = null;
                        _isSearching = true;
                        _searchTimer = 0f;
                        _searchDirectionTimer = 1f;
                    }
                    else
                    {
                        _agent.isStopped = false;
                        _agent.speed = _enemyStatus.ChaseSpeed;
                        _agent.SetDestination(_enemyStatus.LatestPlayerPosition.Value);
                    }
                }
                else
                {
                    if (_isSearching)
                    {
                        _searchTimer += Time.deltaTime;
                        _searchDirectionTimer += Time.deltaTime;

                        if (_searchTimer >= _searchTime)
                        {
                            _isSearching = false;
                            _searchTimer = 0f;
                            _searchDirectionTimer = 0f;
                            _enemyStatus.ChangeState(_enemyStatus.PreviousState);
                            return;
                        }

                        if (_searchDirectionTimer >= 1f)
                        {
                            _searchDirectionTimer = 0f;

                            float randomAngle = Random.Range(-_searchFov * 0.5f, _searchFov * 0.5f);
                            Vector3 randomDir = Quaternion.Euler(0, randomAngle, 0) * transform.forward;
                            Vector3 randomPos = transform.position + randomDir * 5f;

                            if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                            {
                                _agent.isStopped = false;
                                _agent.speed = _enemyStatus.MoveSpeed;
                                _agent.SetDestination(hit.position);
                            }
                        }
                    }
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
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
#endif // 참고용 스크립트 끝
