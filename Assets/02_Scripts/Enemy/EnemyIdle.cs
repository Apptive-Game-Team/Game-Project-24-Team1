using UnityEngine;
using UnityEngine.AI;

namespace MushOut.Enemy
{
    /// <summary>
    /// 에너미가 Idle 상태일 때 제자리에 대기하도록 제어하는 스크립트입니다.
    /// </summary>
    [RequireComponent(typeof(EnemyController), typeof(NavMeshAgent))]
    public class EnemyIdle : MonoBehaviour
    {
        private EnemyController _enemyController;
        private NavMeshAgent _agent;
        private Quaternion _initialRotation;

        private void Awake()
        {
            _enemyController = GetComponent<EnemyController>();
            _agent = GetComponent<NavMeshAgent>();
            _initialRotation = transform.rotation;
        }

        private void Update()
        {
            // 컴포넌트가 없거나 NavMesh 위에 없으면 안전하게 리턴
            if (_enemyController == null || _agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
            {
                return;
            }

            // 현재 상태가 Idle이 아니면 아무것도 하지 않음
            if (_enemyController.CurrentState != EnemyController.State.Idle)
            {
                return;
            }

            Transform pointA = _enemyController.PatrolPointA;

            // PatrolPointA가 지정되어 있지 않거나, 이미 도달(충돌 범위 내)했다면 대기
            if (pointA == null || Vector3.Distance(transform.position, pointA.position) <= _agent.stoppingDistance + 0.1f)
            {
                if (Quaternion.Angle(transform.rotation, _initialRotation) > 0.1f)
                {
                    // 처음에 바라보던 방향(_initialRotation)으로 부드럽게 회전합니다.
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation, 
                        _initialRotation, 
                        _agent.angularSpeed * Time.deltaTime // _agent가 없다면 120f 등 직접 숫자를 넣어도 됩니다.
                    );
                }

                if (!_agent.isStopped)
                {
                    _agent.isStopped = true;
                }

                if (_agent.hasPath)
                {
                    _agent.ResetPath();
                }
            }
            else
            {
                // PatrolPointA에 아직 도달하지 않았다면 해당 위치로 이동
                _agent.isStopped = false;
                _agent.speed = _enemyController.MoveSpeed;
                _agent.SetDestination(pointA.position);
            }
        }
    }
}
