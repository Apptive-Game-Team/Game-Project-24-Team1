using UnityEngine;
using Nexush.Interfaces;

namespace GameProject24.Enemy
{
    /// <summary>
    /// 적의 상태와 체력, 이동 속도 등 기본 스탯을 관리하는 클래스입니다.
    /// </summary>
    public class EnemyStatus : MonoBehaviour, IHittable
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

        [Tooltip("상태가 변경되기 직전의 상태를 저장합니다.")]
        [SerializeField] private State _previousState;

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
        [Tooltip("현재 향하고 있는 목표 오브젝트를 저장합니다.")]
        [SerializeField] private Transform _target;

        [Tooltip("적의 시야 범위입니다.")]
        [Range(0f, 360f)]
        [SerializeField] private float _fieldOfView = 60f;

        [Tooltip("적의 시야 거리입니다.")]
        [Range(0f, 100f)]
        [SerializeField] private float _sightDistance = 10f;

        [Tooltip("플레이어를 추격하는 총 시간입니다.")]
        [Range(0f, 100f)]
        [SerializeField] private float _chasingTime = 10f;

        [Header("Patrol Info")]
        [Tooltip("순찰 경로의 양 끝점입니다.")]
        [SerializeField] private Transform _patrolPointA;
        [SerializeField] private Transform _patrolPointB;

        [Tooltip("순찰 중 멈춰설 지점들입니다.")]
        [SerializeField] private Transform[] _stopPoints;

        /// <summary>
        /// 프로퍼티 (getters)
        /// </summary>
        /// <summary> 현재 상태를 반환합니다. </summary>
        public State CurrentState => _currentState;

        /// <summary> 상태가 변경되기 직전의 이전 상태를 반환합니다. </summary>
        public State PreviousState => _previousState;

        /// <summary> 현재 체력을 반환합니다. </summary>
        public float CurrentHp => _currentHp;

        /// <summary> 최대 체력을 반환합니다. </summary>
        public float MaxHp => _maxHp;

        /// <summary> 기본 이동 속도를 반환합니다. </summary>
        public float MoveSpeed => _moveSpeed;

        /// <summary> 추격 시 이동 속도를 반환합니다. </summary>
        public float ChaseSpeed => _chaseSpeed;

        /// <summary> 타겟(현재 향하고 있는 목표)을 반환하거나 설정합니다. </summary>
        public Transform Target 
        { 
            get => _target; 
            set => _target = value; 
        }

        [Header("Runtime Info (ReadOnly)")]
        [Tooltip("현재 프레임에서 플레이어가 시야에 포착되었는지 여부")]
        [SerializeField] private bool _isPlayerSpotted = false;

        [Tooltip("LPP가 유효한지(탐지된 적이 있는지) 여부")]
        [SerializeField] private bool _hasLatestPlayerPosition = false;

        [Tooltip("마지막으로 플레이어를 탐지한 위치 (LPP)")]
        [SerializeField] private Vector3 _latestPlayerPosition = Vector3.zero;

        /// <summary> 마지막으로 플레이어를 탐지한 위치 (LPP) </summary>
        public Vector3? LatestPlayerPosition
        {
            get
            {
                if (_hasLatestPlayerPosition) return _latestPlayerPosition;
                return null;
            }
            set
            {
                if (value.HasValue)
                {
                    _hasLatestPlayerPosition = true;
                    _latestPlayerPosition = value.Value;
                }
                else
                {
                    _hasLatestPlayerPosition = false;
                }
            }
        }

        /// <summary> 현재 프레임에서 플레이어가 시야에 포착되었는지 여부 </summary>
        public bool IsPlayerSpotted
        {
            get => _isPlayerSpotted;
            set => _isPlayerSpotted = value;
        }

        /// <summary> 적의 시야 범위를 반환합니다. </summary>
        public float FieldOfView => _fieldOfView;

        /// <summary> 적의 시야 거리를 반환합니다. </summary>
        public float SightDistance => _sightDistance;

        /// <summary> 플레이어 추격 유지 시간을 반환합니다. </summary>
        public float ChasingTime => _chasingTime;

        /// <summary> 순찰 경로의 시작점(A)을 반환합니다. </summary>
        public Transform PatrolPointA => _patrolPointA;

        /// <summary> 순찰 경로의 끝점(B)을 반환합니다. </summary>
        public Transform PatrolPointB => _patrolPointB;

        /// <summary> 순찰 중 멈춰설 지점들을 반환합니다. </summary>
        public Transform[] StopPoints => _stopPoints;

        /// <summary>
        /// 게임이 시작될 때 (또는 이 오브젝트가 생성될 때) 1회 실행됩니다.
        /// </summary>
        private void Awake()
        {
            // 체력 초기화 및 기본 상태 설정
            if (_currentHp == 0) {
                _currentHp = _maxHp;
            }
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

            // 동일한 상태로의 전환은 무시 (PreviousState 오염 방지)
            if (_currentState == newState)
            {
                return;
            }

            // 새로운 상태로 변경하기 전, 현재 상태를 백업
            _previousState = _currentState;
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

        /// <summary>
        /// [Rule C] IHittable 인터페이스 구현: 마취총 등에 피격 시 호출됩니다.
        /// </summary>
        /// <param name="hitInfo">피격 정보 (마취 수치 등)</param>
        public void OnHit(HitInfo hitInfo)
        {
            if (_currentState == State.Dead)
            {
                return;
            }

            // 마취 수치가 있다면 기절 상태로 전환
            if (hitInfo.amount > 0)
            {
                ChangeState(State.Stunned);
                Debug.Log($"[EnemyStatus] 마취총 피격! 기절 상태로 변경됩니다. (마취 수치: {hitInfo.amount})");
                // TODO: 기절 애니메이션 재생 및 기절 지속 시간 처리 로직
            }
        }
    }
}

#if UNITY_EDITOR
namespace GameProject24.Enemy
{
    using UnityEditor;

    [CustomEditor(typeof(EnemyStatus))]
    public class EnemyStatusEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.name == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }
                    continue;
                }

                if (iterator.name == "_patrolPointB")
                {
                    SerializedProperty currentStateProp = serializedObject.FindProperty("_currentState");
                    if (currentStateProp != null)
                    {
                        // 현 상태가 Idle이면 _patrolPointB를 인스펙터에서 숨김
                        if (currentStateProp.enumValueIndex == (int)EnemyStatus.State.Idle)
                        {
                            continue;
                        }
                    }
                }

                EditorGUILayout.PropertyField(iterator, true);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif