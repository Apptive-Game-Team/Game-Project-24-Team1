using UnityEngine;
using UnityEngine.AI;

namespace MushOut.Enemy
{
    /// <summary>
    /// EnemyController의 상태와 NavMeshAgent의 속도를 읽어
    /// Animator 파라미터를 자동으로 제어해주는 스크립트입니다.
    /// </summary>
    public class EnemyAnimation : MonoBehaviour
    {
        private Animator _animator;
        private NavMeshAgent _agent;
        private EnemyController _enemyController;
        
        private EnemyController.State _lastState;

        private void Awake()
        {
            // 이 스크립트가 붙은 곳(또는 자식)에서 Animator를 찾음
            _animator = GetComponentInChildren<Animator>();
            
            // AI 컴포넌트들은 보통 최상위 부모에 있으므로 부모에서 찾음
            _agent = GetComponentInParent<NavMeshAgent>();
            _enemyController = GetComponentInParent<EnemyController>();
        }

        private void Start()
        {
            if (_enemyController != null)
            {
                _lastState = _enemyController.CurrentState;
            }
            else
            {
                Debug.LogWarning("[EnemyAnimation] EnemyController를 찾을 수 없습니다! 모델 최상단 오브젝트를 확인해 주세요.");
            }
        }

        private void Update()
        {
            if (_animator == null || _enemyController == null) return;

            // 1. 걷기/뛰기 속도 동기화 (Movement Blend Tree 용)
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                // NavMeshAgent의 실제 이동 속도(Magnitude)를 Animator의 Speed 파라미터로 전달
                _animator.SetFloat("Speed", _agent.velocity.magnitude);
            }
            else
            {
                _animator.SetFloat("Speed", 0f);
            }

            // 2. 상태 변화 감지 및 트리거 실행
            if (_lastState != _enemyController.CurrentState)
            {
                HandleStateChange(_lastState, _enemyController.CurrentState);
                _lastState = _enemyController.CurrentState;
            }
        }

        private void HandleStateChange(EnemyController.State oldState, EnemyController.State newState)
        {
            // 돌진(Attacking) 시작
            if (newState == EnemyController.State.Attacking)
            {
                _animator.SetTrigger("DoCharge");
            }
            // 기절(Stunned) 시작
            else if (newState == EnemyController.State.Stunned)
            {
                _animator.SetTrigger("DoStun");
            }
            
            // 돌진 로직이 종료되었을 때 (기절하지 않고 그냥 추격/대기로 돌아가는 경우)
            if (oldState == EnemyController.State.Attacking && newState != EnemyController.State.Stunned)
            {
                // 앞서 만들어둔 EndCharge 트리거를 쏴서 즉시 걷기/뛰기로 돌아감
                _animator.SetTrigger("EndCharge");
            }

            // 기절 로직이 종료되었을 때 (Stunned 상태에서 벗어날 때)
            if (oldState == EnemyController.State.Stunned)
            {
                _animator.SetTrigger("EndStun");
            }
        }
    }
}
