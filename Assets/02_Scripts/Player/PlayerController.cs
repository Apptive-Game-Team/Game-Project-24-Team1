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
    public class PlayerController : MonoBehaviour
    {
        private PlayerState _currentState = PlayerState.Idle;

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
            _motor = GetComponent<PlayerMotor>();
            _detector = GetComponent<PlayerEnvironmentDetector>();
            _animator = GetComponent<PlayerAnimationDriver>();
            _interactor = GetComponent<PlayerInteractor>();
            
            _climbHandler = GetComponentInChildren<PlayerClimbHandler>();
            if (_climbHandler != null)
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
            if (_climbHandler != null && _climbHandler.IsNearLadder && _input.MoveInput.y > 0.1f)
            {
                ChangeState(PlayerState.Climbing);
                return;
            }

            HandleStateTransitions();

            if (_currentState != PlayerState.Climbing && _currentState != PlayerState.ClimbOver)
            {
                _motor.ApplyGravity(Time.deltaTime, _detector.IsInWater);
                _motor.ApplyMovement(Time.deltaTime, _input.MoveInput, _input.IsSprinting, _detector.IsInWater, _detector.IsGrounded, _interactor.GrabbedObject, _detector.HitNormal, _detector.groundLayers);
            }

            UpdateAnimation();
        }

        private void HandleStateTransitions()
        {
            if (_detector.IsGrounded)
            {
                if (_input.IsJumping && !_detector.IsInWater)
                {
                    ChangeState(PlayerState.Jump);
                    _motor.ExecuteJump();
                    _animator.TriggerJump();
                    return;
                }

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
                if (_motor.VerticalVelocity < 0f && !_detector.IsInWater)
                {
                    ChangeState(PlayerState.Fall);
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
