using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace MushOut.Enemy
{
    /// <summary>
    /// 적이 Attacking(돌진 공격) 상태일 때 작동하는 로직입니다.
    /// 1. 상태 진입 시 LockState()로 외부 상태 전환 차단 (Stunned/Dead 제외)
    /// 2. preRushDelay 초 대기 후 LPP 방향으로 rushDistance만큼 돌진
    ///    - LPP는 방향 계산에만 사용 (도달해도 상태 변화 없음)
    ///    - SphereCast로 전방 장애물 감지 → 충돌 시 Stunned
    ///    - rushDistance 무충돌 완주 → stateAfterRush (기본: Chasing)
    /// </summary>
    [RequireComponent(typeof(EnemyController), typeof(NavMeshAgent))]
    public class EnemyAttacking : MonoBehaviour
    {
        [Header("Rush Settings")]
        [Tooltip("돌진 거리 (m)")]
        [SerializeField] private float _rushDistance = 8f;

        [Tooltip("돌진 속도 (m/s)")]
        [SerializeField] private float _rushSpeed = 15f;

        [Tooltip("SphereCast 감지 반경 (적 캡슐 반경과 맞추세요)")]
        [SerializeField] private float _castRadius = 0.4f;

        [Tooltip("충돌로 간주할 레이어 (Enemy 레이어, Player 레이어 제외)")]
        [SerializeField] private LayerMask _obstacleLayer;

        [Tooltip("돌진 완료 후 복귀할 상태")]
        [SerializeField] private EnemyController.State _stateAfterRush = EnemyController.State.Chasing;

        [Header("Timing")]
        [Tooltip("Attacking 상태 진입 후 돌진 전 대기 시간 (초). 예고 모션 등에 활용.")]
        [SerializeField] private float _preRushDelay = 2.0f;

        [Tooltip("대기 중 플레이어를 향해 회전하는 최대 각도 속도 (도/초). 360으로 설정하면 즉시 회전.")]
        [SerializeField] private float _lookRotateSpeed = 180f;

        private EnemyController _enemyController;
        private NavMeshAgent _agent;
        private Rigidbody _rigidbody;

        private bool _isRunning = false;

        private void Awake()
        {
            _enemyController = GetComponent<EnemyController>();
            _agent = GetComponent<NavMeshAgent>();
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (_enemyController.CurrentState == EnemyController.State.Attacking && !_isRunning)
            {
                StartCoroutine(AttackRoutine());
            }
        }

        private IEnumerator AttackRoutine()
        {
            _isRunning = true;

            // 1. 상태 잠금: 돌진이 끝날 때까지 외부 상태 전환 차단 (Stunned/Dead 제외)
            _enemyController.LockState();

            // 1-1. 돌진 전 대기: 매 프레임 플레이어를 바라보며 준비 동작
            if (_preRushDelay > 0f)
            {
                float elapsed = 0f;
                while (elapsed < _preRushDelay)
                {
                    // 외부 강제 전환 감지 시 루프 즉시 탈출
                    if (_enemyController.CurrentState != EnemyController.State.Attacking) break;

                    // 플레이어 방향으로 즉시 회전 (수평만)
                    Transform playerTransform = MushOut.Core.GameManager.Instance?.PlayerTransform
                        ?? GameObject.FindWithTag("Player")?.transform;

                    if (playerTransform != null)
                    {
                        Vector3 toPlayer = playerTransform.position - transform.position;
                        toPlayer.y = 0f;
                        if (toPlayer.sqrMagnitude > 0.001f)
                        {
                            Quaternion targetRot = Quaternion.LookRotation(toPlayer);
                            transform.rotation = Quaternion.RotateTowards(
                                transform.rotation,
                                targetRot,
                                _lookRotateSpeed * Time.deltaTime
                            );
                        }
                    }

                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            // 대기 중 외부에서 Stunned/Dead로 강제 전환된 경우 즉시 정리
            if (_enemyController.CurrentState != EnemyController.State.Attacking)
            {
                _enemyController.UnlockState();
                _isRunning = false;
                yield break;
            }

            // 2. 플레이어 위치를 LPP로 강제 지정 (방향 계산용)
            Vector3 rushTarget = Vector3.zero;
            bool hasTarget = false;

            if (MushOut.Core.GameManager.Instance?.PlayerTransform != null)
            {
                rushTarget = MushOut.Core.GameManager.Instance.PlayerTransform.position;
                _enemyController.LatestPlayerPosition = rushTarget;
                hasTarget = true;
            }
            else
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    rushTarget = player.transform.position;
                    _enemyController.LatestPlayerPosition = rushTarget;
                    hasTarget = true;
                }
                else if (_enemyController.LatestPlayerPosition.HasValue)
                {
                    rushTarget = _enemyController.LatestPlayerPosition.Value;
                    hasTarget = true;
                }
            }

            if (!hasTarget)
            {
                _enemyController.UnlockState();
                _isRunning = false;
                _enemyController.ChangeState(_enemyController.InitialState);
                yield break;
            }

            // 3. 돌진 방향 계산 (수평 방향만 사용, LPP는 방향 계산에만 사용)
            Vector3 rushDir = rushTarget - transform.position;
            rushDir.y = 0f;
            rushDir.Normalize();

            if (rushDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(rushDir);
            }

            // 4. 돌진 준비: NavMeshAgent 비활성화, 중력 차단 (비활성화 후 낙하 방지)
            _agent.isStopped = true;
            _agent.enabled = false;

            if (_rigidbody != null)
            {
                _rigidbody.useGravity = false;   // 돌진 중 중력 차단
                _rigidbody.linearVelocity = Vector3.zero; // 잔류 속도 초기화
            }

            // 5. 돌진 루프: SphereCast로 전방 장애물 감지 (OnCollisionEnter 미사용 - 바닥 오감지 방지)
            Vector3 startPos = transform.position;
            float rushY = startPos.y;           // Y축 기준점 고정 (매 프레임 참조 시 누적 오차 방지)
            bool collidedDuringRush = false;

            while (true)
            {
                // 외부에서 Stunned/Dead로 강제 전환된 경우 (마취총 피격 등)
                if (_enemyController.CurrentState != EnemyController.State.Attacking) break;

                float distanceTraveled = Vector3.Distance(startPos, transform.position);

                // rushDistance 완주 → 충돌 없음
                if (distanceTraveled >= _rushDistance) break;

                float moveStep = _rushSpeed * Time.deltaTime;

                // SphereCast로 이동 전 전방 장애물 검사
                // obstacleLayer에 설정된 레이어만 충돌로 간주 (Player/Enemy 레이어 제외 권장)
                if (_obstacleLayer.value != 0 &&
                    Physics.SphereCast(transform.position, _castRadius, rushDir, out RaycastHit hit, moveStep + 0.05f, _obstacleLayer))
                {
                    collidedDuringRush = true;
                    break;
                }

                // 수평 이동 (Y축은 돌진 시작 시 고정값 rushY로 유지 - 누적 낙하 방지)
                Vector3 newPos = transform.position + rushDir * moveStep;
                newPos.y = rushY;

                if (_rigidbody != null)
                {
                    _rigidbody.MovePosition(newPos);
                }
                else
                {
                    transform.position = newPos;
                }

                yield return null;
            }

            // 6. NavMeshAgent 복구, 중력 원상 복원
            if (_rigidbody != null)
            {
                _rigidbody.useGravity = true;
                _rigidbody.linearVelocity = Vector3.zero;
            }
            _agent.enabled = true;
            _agent.isStopped = false;

            // 7. 잠금 해제
            _enemyController.UnlockState();
            _isRunning = false;

            // 8. 아직 Attacking 상태면(외부 전환 없음) 결과에 따라 상태 결정
            if (_enemyController.CurrentState == EnemyController.State.Attacking)
            {
                _enemyController.ChangeState(collidedDuringRush
                    ? EnemyController.State.Stunned
                    : _stateAfterRush);
            }
            // else: 이미 Stunned/Dead로 전환됨 → 추가 처리 불필요
        }
    }
}
