using UnityEngine;

namespace GameProject24.Enemy
{
    /// <summary>
    /// 적의 상태와 체력, 이동 속도 등 기본 스탯을 관리하는 클래스입니다.
    /// </summary>
    public class EnemyStatus : MonoBehaviour
    {
        /// <summary>
        /// 적의 상태들을 열거형(enum)으로 정의합니다. (bool 여러 개를 대체)
        /// </summary>
        public enum State
        {
            Idle,       // 대기 (_isStanding)
            Patrolling, // 정찰 (_isPatrolling)
            Chasing,    // 추격 (_isChasing)
            Attacking,  // 공격 중
            Damaged,    // 피격 당함
            Stunned,    // 기절 (_isStunned)
            Dead        // 사망 (_isDead)
        }

        [Header("Current State")]
        [Tooltip("현재 적의 상태를 나타냅니다.")]
        [SerializeField] private State _currentState;

        [Header("Enemy Stats")]
        [Tooltip("적의 최대 체력입니다.")]
        [Range(1f, 100f)]
        [SerializeField] private float _maxHp = 100f;

        [Tooltip("적의 현재 체력입니다.")]
        [SerializeField] private float _currentHp;

        [Tooltip("적의 기본 이동 속도입니다.")]
        [SerializeField] private float _moveSpeed = 3f;

        [Tooltip("플레이어를 추격할 때의 이동 속도입니다.")]
        [SerializeField] private float _chaseSpeed = 6f;

        [Header("Target Info")]
        [Tooltip("추격할 플레이어의 위치를 저장할 변수입니다.")]
        [SerializeField] private Transform _target;

        /// <summary> 현재 상태를 반환합니다. </summary>
        public State CurrentState => _currentState;

        /// <summary> 현재 체력을 반환합니다. </summary>
        public float CurrentHp => _currentHp;

        /// <summary> 최대 체력을 반환합니다. </summary>
        public float MaxHp => _maxHp;

        /// <summary> 기본 이동 속도를 반환합니다. </summary>
        public float MoveSpeed => _moveSpeed;

        /// <summary> 추격 시 이동 속도를 반환합니다. </summary>
        public float ChaseSpeed => _chaseSpeed;

        /// <summary> 타겟(플레이어)을 반환합니다. </summary>
        public Transform Target => _target;

        /// <summary>
        /// 게임이 시작될 때 (또는 이 오브젝트가 생성될 때) 1회 실행됩니다.
        /// </summary>
        private void Awake()
        {
            // 체력 초기화 및 기본 상태 설정
            if (_currentHp == 0) {
                _currentHp = _maxHp;
            }
            _currentState = State.Idle;
        }

        /// <summary>
        /// 상태를 변경할 때 사용할 함수입니다. (다른 스크립트에서 호출하기 편함)
        /// </summary>
        /// <param name="newState">변경할 새로운 상태</param>
        public void ChangeState(State newState)
        {
            // 이미 죽었다면 다른 상태로 변경하지 못하게 막음
            if (_currentState == State.Dead)
            {
                return;
            }

            _currentState = newState;
        }

        /// <summary>
        /// 외부에서 데미지를 입었을 때 호출할 함수입니다.
        /// </summary>
        /// <param name="damage">입힐 데미지 양</param>
        public void TakeDamage(float damage)
        {
            if (_currentState == State.Dead)
            {
                return;
            }

            // 체력이 0 미만으로 떨어지지 않도록 Clamp 사용
            _currentHp = Mathf.Clamp(_currentHp - damage, 0, _maxHp);

            if (_currentHp <= 0)
            {
                ChangeState(State.Dead);
                // TODO: 사망 애니메이션 재생 및 아이템 드랍 로직
            }
            else
            {
                ChangeState(State.Damaged);
                // TODO: 피격 애니메이션 재생
            }
        }
    }
}