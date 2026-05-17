using UnityEngine;
using UnityEngine.AI;

namespace MushOut.Enemy
{
    /// <summary>
    /// 적이 Chasing(추격) 상태일 때 작동하는 로직입니다.
    /// 플레이어의 마지막 목격 위치(LPP)를 추적하며, 놓쳤을 경우 일정 시간 동안 주변을 수색합니다.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyController))]
    public class EnemyChasing : MonoBehaviour
    {
        private NavMeshAgent _agent;
        private EnemyController _enemyController;

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
            _enemyController = GetComponent<EnemyController>();
        }

        private void Update()
        {
            // ==========================================
            // 메인 로직 루프 (Update - 매 프레임 실행)
            // ==========================================
            if (_enemyController.CurrentState != EnemyController.State.Chasing)
            {
                _searchTimer = 0f;
                _entryDelayTimer = 0f;
                return;
            }

            // 추격 진입 시 0.1초 동안 플레이어를 바라보며 대기
            if (_entryDelayTimer < 0.1f)
            {
                _entryDelayTimer += Time.deltaTime;
                _agent.speed = 0f;
                
                if (_agent.isOnNavMesh && _agent.hasPath)
                {
                    _agent.ResetPath();
                }

                if (_enemyController.LatestPlayerPosition.HasValue)
                {
                    Vector3 dir = (_enemyController.LatestPlayerPosition.Value - transform.position).normalized;
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
            if (_enemyController.IsPlayerSpotted)
            {
                // [시야에 있음 : 추격]
                _searchTimer = 0f;

                if (_enemyController.LatestPlayerPosition.HasValue)
                {
                    float distToLpp = Vector3.Distance(transform.position, _enemyController.LatestPlayerPosition.Value);
                    if (distToLpp <= _enemyController.AttackRadius)
                    {
                        _enemyController.ChangeState(EnemyController.State.Attacking);
                    }
                    else
                    {
                        _targetPoint = _enemyController.LatestPlayerPosition.Value;
                        _speed = _enemyController.ChaseSpeed;
                    }
                }
            }
            else
            {
                // [시야에서 놓침 : 1단계 - 마지막 목격 위치(LPP)로 이동, 2단계 - 랜덤 수색]
                _searchTimer += Time.deltaTime;

                if (_searchTimer >= _searchTime)
                {
                    _enemyController.ChangeState(_enemyController.InitialState);
                    return;
                }

                if (_enemyController.LatestPlayerPosition.HasValue)
                {
                    // [1단계] LPP가 있으면 먼저 그 위치로 ChaseSpeed로 이동
                    float distToLpp = Vector3.Distance(transform.position, _enemyController.LatestPlayerPosition.Value);
                    bool arrivedAtLpp = distToLpp <= _agent.stoppingDistance + 0.5f ||
                                       (_agent.hasPath && !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance);

                    if (arrivedAtLpp)
                    {
                        // LPP 도달 완료: 클리어 후 다음 프레임부터 2단계 수색으로 전환
                        _enemyController.LatestPlayerPosition = null;
                    }
                    else
                    {
                        // 아직 미도달: LPP를 목표로 설정하고 빠르게 이동
                        _targetPoint = _enemyController.LatestPlayerPosition.Value;
                        _speed = _enemyController.ChaseSpeed;
                    }
                }
                else
                {
                    // [2단계] LPP 없음(도착 완료) : 주변 랜덤 수색
                    float distToTarget = Vector3.Distance(transform.position, _targetPoint);
                    bool arrivedAtTarget = distToTarget <= _agent.stoppingDistance + 0.5f ||
                                          (_agent.hasPath && !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance);

                    if (arrivedAtTarget)
                    {
                        // 현재 수색 목표 도달: 새로운 랜덤 수색 지점 탐색
                        _speed = _enemyController.MoveSpeed;

                        float randomAngle = Random.Range(-_searchFov * 0.5f, _searchFov * 0.5f);
                        Vector3 randomDir = Quaternion.Euler(0, randomAngle, 0) * transform.forward;
                        Vector3 randomPos = transform.position + randomDir * 5f;

                        if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                        {
                            _targetPoint = hit.position;
                        }
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
