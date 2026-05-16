using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace MushOut.Enemy
{
    /// <summary>
    /// 에너미가 Patrolling 상태일 때 정해진 지점(A, B)을 순찰하고 멈춰서 두리번거리는 로직입니다.
    /// </summary>
    [RequireComponent(typeof(EnemyStatus), typeof(NavMeshAgent))]
    public class EnemyPatrolling : MonoBehaviour
    {
        private EnemyStatus _enemyStatus;
        private NavMeshAgent _agent;

        private bool _isLookingAround = false;
        private Transform _lastStopPoint = null;

        private void Awake()
        {
            _enemyStatus = GetComponent<EnemyStatus>();
            _agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            // 필수 컴포넌트나 내비메시가 정상 상태가 아니면 대기
            if (_enemyStatus == null || _agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
                return;

            // 현 상태가 Patrolling이 아닐 경우
            if (_enemyStatus.CurrentState != EnemyStatus.State.Patrolling)
            {
                // 두리번거리던 도중 다른 상태(Chasing 등)로 바뀌면 코루틴 즉시 강제 종료 및 설정 복구
                if (_isLookingAround)
                {
                    StopAllCoroutines();
                    _isLookingAround = false;
                    _agent.updateRotation = true;
                }
                return;
            }

            // Stop 포인트에서 두리번거리고 있는 중이라면 이동을 멈춤
            if (_isLookingAround)
                return;

            // 1. 순찰(A <-> B 왕복) 이동 로직 수행
            HandlePatrol();

            // 2. StopPoint 도달 및 충돌 검사
            CheckStopPoints();
        }

        /// <summary>
        /// Target을 A와 B로 번갈아가며 지정하여 순찰하는 로직
        /// </summary>
        private void HandlePatrol()
        {
            // 현재 타겟이 없거나 A, B 둘 다 아니면 A를 기본 목적지로 설정
            if (_enemyStatus.Target != _enemyStatus.PatrolPointA && _enemyStatus.Target != _enemyStatus.PatrolPointB)
            {
                if (_enemyStatus.PatrolPointA != null)
                {
                    _enemyStatus.Target = _enemyStatus.PatrolPointA;
                }
            }

            if (_enemyStatus.Target != null)
            {
                // EnemyStatus에 설정된 순찰 속도(MoveSpeed) 반영
                _agent.speed = _enemyStatus.MoveSpeed;
                _agent.isStopped = false;
                _agent.SetDestination(_enemyStatus.Target.position);

                // 목적지에 도착했는지 확인
                if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
                {
                    if (!_agent.hasPath || _agent.velocity.sqrMagnitude == 0f)
                    {
                        // A에 도착했다면 B로, B에 도착했다면 A로 타겟 교체
                        if (_enemyStatus.Target == _enemyStatus.PatrolPointA)
                        {
                            _enemyStatus.Target = _enemyStatus.PatrolPointB;
                        }
                        else
                        {
                            _enemyStatus.Target = _enemyStatus.PatrolPointA;
                        }

                        // 다음 왕복 시 스탑 포인트에서 다시 멈출 수 있도록 방문 기록 초기화
                        _lastStopPoint = null; 
                    }
                }
            }
        }

        /// <summary>
        /// 콜라이더가 없을 경우를 대비해 물리적 거리(Distance) 기반으로 StopPoint 충돌(도달)을 검사
        /// </summary>
        private void CheckStopPoints()
        {
            if (_enemyStatus.StopPoints == null) return;

            foreach (var sp in _enemyStatus.StopPoints)
            {
                if (sp != null && sp != _lastStopPoint)
                {
                    // 일정 거리(1.5f) 이내로 들어오면 충돌한 것으로 간주하고 정지
                    if (Vector3.Distance(transform.position, sp.position) < 1.5f)
                    {
                        StartCoroutine(LookAroundRoutine(sp));
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 물리 콜라이더의 Trigger 방식을 사용할 경우의 충돌 검사
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (_enemyStatus == null || _enemyStatus.CurrentState != EnemyStatus.State.Patrolling || _isLookingAround)
                return;

            if (_enemyStatus.StopPoints == null) return;

            foreach (var sp in _enemyStatus.StopPoints)
            {
                // 충돌한 오브젝트가 StopPoints 배열에 포함된 오브젝트인지 확인
                if (sp != null && other.transform == sp && sp != _lastStopPoint)
                {
                    StartCoroutine(LookAroundRoutine(sp));
                    break;
                }
            }
        }

        /// <summary>
        /// 5초 동안 정지 후 왼쪽, 오른쪽으로 총 180도를 둘러보는 코루틴
        /// </summary>
        private IEnumerator LookAroundRoutine(Transform stopPoint)
        {
            _isLookingAround = true;
            _lastStopPoint = stopPoint; // 현재 멈춘 지점을 기록하여 연속으로 멈추지 않게 함

            // 내비메시 에이전트 정지 및 자체 회전 막기
            _agent.isStopped = true;
            _agent.updateRotation = false;

            // 총 5초 대기 시퀀스
            float totalDuration = 5f;
            float phaseTime = totalDuration / 4f; // 1.25초씩 4단계로 분할

            Quaternion startRot = transform.rotation;
            Quaternion leftRot = startRot * Quaternion.Euler(0, -90, 0);  // 왼쪽 90도
            Quaternion rightRot = startRot * Quaternion.Euler(0, 90, 0); // 오른쪽 90도 (왼쪽에서는 180도)

            // 1단계: 정면 -> 왼쪽 (1.25초 소요)
            yield return StartCoroutine(RotateOverTime(startRot, leftRot, phaseTime));
            
            // 2단계: 왼쪽 -> 오른쪽 (180도 회전, 2.5초 소요)
            yield return StartCoroutine(RotateOverTime(leftRot, rightRot, phaseTime * 2f));

            // 3단계: 오른쪽 -> 정면 (1.25초 소요)
            yield return StartCoroutine(RotateOverTime(rightRot, startRot, phaseTime));

            // 둘러보기가 끝나면 다시 이동할 수 있도록 복구
            _agent.updateRotation = true;
            _agent.isStopped = false;
            _isLookingAround = false;
        }

        /// <summary>
        /// 주어진 시간(duration) 동안 from 각도에서 to 각도로 부드럽게 회전시키는 코루틴
        /// </summary>
        private IEnumerator RotateOverTime(Quaternion from, Quaternion to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                transform.rotation = Quaternion.Slerp(from, to, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            // 마지막 오차 보정
            transform.rotation = to; 
        }
    }
}
