using UnityEngine;

namespace MushOut.Player
{
    public enum PlayerState
    {
        Idle,
        Move,
        Jump,
        Fall,
        Climbing,
        ClimbOver
    }

    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(PlayerEnvironmentDetector))]
    [RequireComponent(typeof(PlayerAnimationDriver))]
    [RequireComponent(typeof(PlayerInteractor))]
    [RequireComponent(typeof(PlayerEnemyCollisionHandler))]
    public class PlayerController : MonoBehaviour
    {
        private PlayerState _currentState = PlayerState.Idle;

        [Header("Jump Settings")]
        [Tooltip("최대 점프 가능한 횟수입니다. (1: 기본 점프, 2: 이단 점프)")]
        [SerializeField, Range(1, 3)] private int maxJumpCount = 2;
        private int _currentJumpCount;

        [Tooltip("낙하 상태로 판정되기까지의 대기 시간입니다.")]
        [SerializeField] private float fallTimeout = 0.15f;
        private float _fallTimeoutDelta;

        private PlayerInputHandler _input;
        private PlayerMotor _motor;
        private PlayerEnvironmentDetector _detector;
        private PlayerAnimationDriver _animator;
        private PlayerClimbHandler _climbHandler;
        private PlayerInteractor _interactor;

        public PlayerState CurrentState => _currentState;

        private void Awake()
        {
            _input = GetComponent<PlayerInputHandler>();
            _animator = GetComponent<Animator>();
            _weapon = GetComponent<PlayerWeapon>(); // 사격 컴포넌트 캐싱

            if (_weapon == null) Debug.LogError("[PlayerController] PlayerWeapon 컴포넌트를 찾을 수 없습니다! 오브젝트에 추가되어 있나요?");

            if (_animator != null)
            {
                // 스크립트 기반 이동을 위해 Root Motion 비활성화 (점프 높이 및 이동 속도 유지)
                _animator.applyRootMotion = false;

                // X, Z축 고정값(0)을 제거하고 Y축만 modelYOffset으로 변경하여 텔레포트 방지
                //Vector3 currentLocalPos = _animator.transform.localPosition;
                //_animator.transform.localPosition = new Vector3(currentLocalPos.x, modelYOffset, currentLocalPos.z);
            }

            _fallTimeoutDelta = fallTimeout;

            if (groundLayers.value == 0)
            {
                _climbHandler.Initialize(this, _input, _motor.GetController(), _animator.GetComponent<Animator>());
            }
        }

        private void Update()
        {
            _detector.CheckEnvironment();
            _animator.SetGrounded(_detector.IsGrounded);

            if (_currentState == PlayerState.Climbing || _currentState == PlayerState.ClimbOver)
            {
                if (_climbHandler != null)
                {
                    if (_currentState == PlayerState.Climbing)
                    {
                        _climbHandler.HandleClimbingState(Time.deltaTime, _motor.gravity, _motor.jumpHeight, _motor.moveSpeed, _detector.IsGrounded);
                    }
                    else if (_currentState == PlayerState.ClimbOver)
                    {
                        _climbHandler.HandleClimbOver(Time.deltaTime);
                    }
                }
                return;
            }

            // 사다리 타기 진입 체크 (PlayerInputHandler에서 Up 입력이 있을 때)
            // [보정] 공중에서는 점프 중이 아닐 때(낙하 중일 때)만 사다리를 잡을 수 있게 하여 '공중 부양' 현상 방지
            if (_climbHandler != null && _climbHandler.IsNearLadder && _input.MoveInput.y > 0.1f)
            {
                if (_detector.IsGrounded || _motor.VerticalVelocity <= 0f)
                {
                    ChangeState(PlayerState.Climbing);
                    return;
                }
            }

            HandleStateTransitions();
            HandleJumpInput();

            if (_currentState != PlayerState.Climbing && _currentState != PlayerState.ClimbOver)
            {
                _motor.ApplyGravity(Time.deltaTime, _detector.IsInWater, _detector.IsGrounded);
                _motor.ApplyMovement(Time.deltaTime, _input.MoveInput, _input.IsSprinting, _detector.IsInWater, _detector.IsGrounded, _interactor.GrabbedObject, _detector.HitNormal, _detector.groundLayers);
            }

            UpdateAnimation();
        }

        private void HandleStateTransitions()
        {
            if (_detector.IsGrounded)
            {
                _fallTimeoutDelta = fallTimeout;

                if (_input.MoveInput != Vector2.zero)
                {
                    ChangeState(PlayerState.Move);
                }
                else
                {
                    ChangeState(PlayerState.Idle);
                }
            }
            else
            {
                // 걸어서 절벽에서 떨어지는 경우, 첫 번째 점프를 소모한 것으로 간주
                if (_currentJumpCount == 0)
                {
                    _currentJumpCount = 1;
                }

                // 낙하 상태로의 전환 유예 (Fall Timeout)
                if (_fallTimeoutDelta > 0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else if (_motor.VerticalVelocity < 0f && !_detector.IsInWater)
                {
                    ChangeState(PlayerState.Fall);
                }
            }
        }

        /// <summary>
        /// 점프 입력을 처리합니다. (이단 점프 포함)
        /// </summary>
        private void HandleJumpInput()
        {
            // 지면에 닿아있을 때 점프 횟수 초기화
            // 점프 직후 프레임에서 땅 판정이 남아있는 것을 방지하기 위해 수직 속도가 0 이하일 때만 초기화
            if (_detector.IsGrounded && _motor.VerticalVelocity <= 0f)
            {
                _currentJumpCount = 0;
            }

            // 점프 입력 처리 (최대 점프 횟수 내에서만 허용, 물 속이 아닐 때만)
            if (_input.IsJumping && _currentJumpCount < maxJumpCount && !_detector.IsInWater)
            {
                // 점프 속도 계산 (중력과 높이 기반)
                _motor.ExecuteJump();

                _currentJumpCount++;

                // 애니메이션 트리거 (트랜지션을 통해 재생되도록 Trigger 파라미터 사용)
                _animator.TriggerJump();

                // 상태 변경 (이단 점프 시에도 로직 처리를 위해 Jump 상태로 진입 시도)
                if (_currentState != PlayerState.Jump)
                {
                    ChangeState(PlayerState.Jump);
                }
            }
        }

        public void ChangeState(PlayerState newState)
        {
            if (_currentState == newState) return;

            _currentState = newState;
            _animator.SetState(newState);
        }

        private void UpdateAnimation()
        {
            float targetAnimSpeed = 0f;
            if (_currentState == PlayerState.Move)
            {
                if (_interactor.GrabbedObject != null) targetAnimSpeed = _motor.pushPullSpeed;
                else if (_detector.IsInWater) targetAnimSpeed = _motor.waterMoveSpeed;
                else if (_input.IsSprinting) targetAnimSpeed = _motor.sprintSpeed;
                else targetAnimSpeed = _motor.moveSpeed;
            }

            _animator.SetSpeed(targetAnimSpeed, _input.MoveInput.magnitude);
        }

        // --- 외부 연동용 브릿지 ---
        public void AddBuoyancy(float force)
        {
            _motor?.AddBuoyancy(force);
        }

        public void AddExternalForce(Vector3 force)
        {
            _motor?.AddExternalForce(force);
        }

        public Vector3 GetVelocity()
        {
            return _motor != null ? _motor.GetHorizontalVelocity() : Vector3.zero;
        }

        public bool IsInWater => _detector != null && _detector.IsInWater;
        
        public bool IsGrounded => _detector != null && _detector.IsGrounded;

        public void SetVerticalVelocity(float value)
        {
            if (_motor != null)
            {
                _motor.VerticalVelocity = value;
            }
        }
    }
}
