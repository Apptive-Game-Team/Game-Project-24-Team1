using UnityEngine;

namespace Nexush.Player
{
    /// <summary>
    /// 플레이어의 실제 이동과 물리 처리를 담당하는 컨트롤러 클래스입니다.
    /// CharacterController를 사용하여 이동, 점프, 중력 로직을 수행합니다.
    /// </summary>
    public enum PlayerState
    {
        Idle,
        Move,
        Jump,
        Fall,
        Climbing,
        ClimbOver
    }

    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(PlayerClimbHandler))]
    public class PlayerController : MonoBehaviour
    {
        [Header("이동 설정")]
        [Tooltip("기본 이동 속도입니다.")]
        [SerializeField] private float moveSpeed = 5.0f;

        [Tooltip("전력 질주 시 속도입니다.")]
        [SerializeField] private float sprintSpeed = 8.0f;

        [Tooltip("속도 변화의 가속도 계수입니다.")]
        [SerializeField] private float speedChangeRate = 10.0f;


        [Header("점프 및 중력")]
        [Tooltip("점프 높이입니다.")]
        [SerializeField] private float jumpHeight = 1.2f;

        [Tooltip("적용될 중력 값입니다.")]
        [SerializeField] private float gravity = -15.0f;

        [Tooltip("낙하 상태로 판정되기까지의 대기 시간입니다.")]
        [SerializeField] private float fallTimeout = 0.15f;

        [Tooltip("최대 점프 가능한 횟수입니다. (1: 기본 점프, 2: 이단 점프)")]
        [SerializeField, Range(1, 3)] private int maxJumpCount = 2;

        [Header("지면 체크 설정")]
        [Tooltip("지면으로 인식할 레이어 설정입니다.")]
        [SerializeField] private LayerMask groundLayers;

        [Header("물(Water) 설정")]
        [Tooltip("물 판정을 위한 레이어 설정입니다.")]
        [SerializeField] private LayerMask waterLayer;

        [Tooltip("물 속에서의 이동 속도입니다.")]
        [SerializeField] private float waterMoveSpeed = 2.0f;

        [Tooltip("물 속에서 받는 수직 저항력(Drag)입니다. (클수록 수직 이동이 둔해짐)")]
        [SerializeField] private float waterDrag = 3.0f;

        [Header("시각적 보정")]
        [Tooltip("캐릭터가 공중에 떠 보일 경우 모델을 아래로 내리는 오프셋입니다. (보통 -0.08 권장)")]
        [SerializeField] private float modelYOffset = -0.08f;

        // 상태 변수
        private float _currentSpeed;
        private float _verticalVelocity;
        private float _fallTimeoutDelta;
        private bool _isGrounded = true;
        private PlayerState _currentState;
        private PlayerState _previousState;
        private Vector3 _hitNormal = Vector3.up; // 지면의 기울기(법선) 정보
        private bool _isSprintingInternal;      // 공중에서 상태 고정을 위한 내부 스프린트 판정 변수
        private int _currentJumpCount;          // 현재 수행한 점프 횟수
        private bool _isInWater;                // 물 안에 있는지 여부

        public PlayerState CurrentState => _currentState;
        public bool IsGrounded => _isGrounded;

        // 컴포넌트 캐싱
        private CharacterController _controller;
        private PlayerInputHandler _input;
        private Animator _animator;
        private PlayerWeapon _weapon; // 사격 컴포넌트 참조 추가
        private PlayerClimbHandler _climbHandler;

        // 애니메이터 파라미터 해시 캐싱 (성능 최적화)
        private static readonly int AnimIDSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimIDGrounded = Animator.StringToHash("Grounded");
        private static readonly int AnimIDJump = Animator.StringToHash("Jump");
        private static readonly int AnimIDFreeFall = Animator.StringToHash("FreeFall");
        private static readonly int AnimIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        private static readonly int AnimIDClimbing = Animator.StringToHash("Climbing");
        private static readonly int AnimIDClimbOver = Animator.StringToHash("ClimbOver");

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
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
                groundLayers = ~0; // 기본적으로 모든 레이어 체크
                Debug.LogWarning("[PlayerController] groundLayers가 설정되지 않아 모든 레이어를 지면으로 인식합니다.");
            }

            // 클라이밍 핸들러 캐싱
            _climbHandler = GetComponent<PlayerClimbHandler>();
            _climbHandler.Initialize(this, _input, _controller, _animator);

            // 초기 상태 설정
            ChangeState(PlayerState.Idle);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            // 현재 상태에 따른 로직 실행 전 공통 처리 (ClimbOver 상태 제외)
            if (_currentState != PlayerState.ClimbOver)
            {
                CheckGrounded();
                CheckWater();
                HandleShootingInput();
            }

            // 현재 상태에 따른 로직 실행
            switch (_currentState)
            {
                case PlayerState.Idle:
                case PlayerState.Move:
                    HandleGroundedState(deltaTime);
                    break;
                case PlayerState.Jump:
                case PlayerState.Fall:
                    HandleAirborneState(deltaTime);
                    break;
                case PlayerState.Climbing:
                    _climbHandler.HandleClimbingState(deltaTime, gravity, jumpHeight, moveSpeed, _isGrounded);
                    break;
                case PlayerState.ClimbOver:
                    _climbHandler.HandleClimbOver(deltaTime);
                    break;
            }
        }


        /// <summary>
        /// 플레이어의 상태를 변경하고 필요한 초기화 로직을 수행합니다.
        /// </summary>
        /// <param name="newState">새로운 상태</param>
        public void ChangeState(PlayerState newState)
        {
            if (_currentState == newState) return;

            _previousState = _currentState;
            _currentState = newState;

            // 상태 진입 시 일회성 로직 (애니메이션 등)
            switch (_currentState)
            {
                case PlayerState.Idle:
                    if (_animator)
                    {
                        _animator.SetBool(AnimIDFreeFall, false);
                        _animator.SetBool(AnimIDClimbing, false);
                        _animator.SetBool(AnimIDClimbOver, false);
                    }
                    break;
                case PlayerState.Move:
                    if (_animator)
                    {
                        _animator.SetBool(AnimIDFreeFall, false);
                        _animator.SetBool(AnimIDClimbing, false);
                        _animator.SetBool(AnimIDClimbOver, false);
                    }
                    break;
                case PlayerState.Jump:
                    if (_animator)
                    {
                        _animator.SetBool(AnimIDClimbing, false);
                        _animator.SetBool(AnimIDClimbOver, false);
                    }
                    break;
                case PlayerState.Fall:
                    if (_animator)
                    {
                        _animator.SetBool(AnimIDFreeFall, true);
                        _animator.SetBool(AnimIDClimbing, false);
                        _animator.SetBool(AnimIDClimbOver, false);
                    }
                    break;
                case PlayerState.Climbing:
                    _verticalVelocity = 0f; // 사다리 진입 시 속도 초기화
                    if (_animator)
                    {
                        _animator.SetBool(AnimIDClimbing, true);
                        _animator.SetBool(AnimIDFreeFall, false);
                        _animator.SetBool(AnimIDClimbOver, false);
                    }
                    break;
                case PlayerState.ClimbOver:
                    _verticalVelocity = 0f; // 넘기 동작 진입 시 속도 초기화
                    if (_animator)
                    {
                        _animator.SetBool(AnimIDClimbOver, true);
                        _animator.SetBool(AnimIDClimbing, false);
                        _animator.SetBool(AnimIDFreeFall, false);
                    }
                    break;
            }
        }

        /// <summary>
        /// 지면에 있을 때의 로직을 처리합니다.
        /// </summary>
        private void HandleGroundedState(float deltaTime)
        {
            // 중력 적용 (착지 상태 유지용)
            if (_isInWater)
            {
                // 물 속에서는 정상적으로 중력과 부력이 싸우도록 ApplyGravity 호출
                ApplyGravity(deltaTime);
            }
            else if (_verticalVelocity < 0f) 
            {
                // 지면 착지 유지용 (물 밖일 때만)
                _verticalVelocity = -2f;
            }

            _currentJumpCount = 0; // 지면에 닿아있으므로 점프 횟수 초기화

            ApplyMovement(deltaTime);
            HandleJumpInput();

            // 사다리 타기 체크 (사용자 요청: 절대적으로 트리거 박스로만 진입)
            if (_climbHandler.IsNearLadder && Mathf.Abs(_input.MoveInput.y) > 0.1f)
            {
                ChangeState(PlayerState.Climbing);
                return;
            }

            // 점프 입력 등으로 인해 상태가 이미 변경되었다면 지면 전이 로직 무시
            if (_currentState != PlayerState.Idle && _currentState != PlayerState.Move) return;

            // 상태 전이 체크
            if (!_isGrounded)
            {
                ChangeState(PlayerState.Fall);
            }
            else
            {
                PlayerState nextState = (_input.MoveInput == Vector2.zero) ? PlayerState.Idle : PlayerState.Move;
                ChangeState(nextState);
            }
        }

        /// <summary>
        /// 공중에 있을 때의 로직을 처리합니다.
        /// </summary>
        private void HandleAirborneState(float deltaTime)
        {
            ApplyGravity(deltaTime);
            ApplyMovement(deltaTime); // 공중 제어 포함
            HandleJumpInput();        // 공중 점프(이단 점프) 입력 체크

            // 사다리 타기 체크 (사용자 요청: 절대적으로 트리거 박스로만 진입)
            // [보정] 공중에서는 점프 중이 아닐 때(낙하 중일 때)만 사다리를 잡을 수 있게 하여 '공중 부양' 현상 방지
            if (_climbHandler.IsNearLadder && Mathf.Abs(_input.MoveInput.y) > 0.1f && _verticalVelocity <= 0f)
            {
                ChangeState(PlayerState.Climbing);
                return;
            }

            // 상태 전이 체크
            // 점프 직후 바로 착지 판정이 나는 것을 방지하기 위해 수직 속도가 0 이하일 때만 착지 체크
            if (_isGrounded && _verticalVelocity <= 0.1f)
            {
                ChangeState(PlayerState.Idle);
            }
            else
            {
                // 낙하 상태로의 전환 유예 (Fall Timeout)
                if (_fallTimeoutDelta > 0f)
                {
                    _fallTimeoutDelta -= deltaTime;
                }
                else if (_verticalVelocity < 0f && _currentState != PlayerState.Fall)
                {
                    ChangeState(PlayerState.Fall);
                }
            }
        }

        private void HandleShootingInput()
        {
            if (_input.IsFiring && _weapon != null)
            {
                _weapon.FireWeapon();
            }
        }

        /// <summary>
        /// 플레이어 하단의 구체 체크를 통해 지면에 닿아있는지 확인합니다.
        /// </summary>
        private void CheckGrounded()
        {
            // CharacterController의 내장 판정과 레이캐스트를 조합하여 정밀하게 지면 체크
            // 발 위치(transform.position)에서 아래로 짧은 광선을 쏩니다.
            float rayLength = _controller != null ? _controller.skinWidth + 0.1f : 0.2f;
            Vector3 rayOrigin = transform.position + Vector3.up * 0.05f;

            bool hitGround = Physics.Raycast(rayOrigin, Vector3.down, rayLength, groundLayers, QueryTriggerInteraction.Ignore);
            
            // 두 판정 중 하나라도 참이면 지면으로 간주
            _isGrounded = (_controller != null && _controller.isGrounded) || hitGround;

            if (_animator)
            {
                _animator.SetBool(AnimIDGrounded, _isGrounded);
            }
        }

        /// <summary>
        /// 캐릭터의 중심점 부근에 물 오브젝트가 겹쳐 있는지 실시간으로 확인합니다.
        /// (움직이는 물이나 수위 변화에도 정확하게 대응하기 위해 사용)
        /// </summary>
        private void CheckWater()
        {
            if (_controller == null) return;

            // 캐릭터의 중심점과 반지름을 사용하여 오버랩 체크 (Trigger 포함)
            Vector3 center = transform.position + _controller.center;
            _isInWater = Physics.CheckSphere(center, _controller.radius + 0.1f, waterLayer, QueryTriggerInteraction.Collide);
        }

        public void SetVerticalVelocity(float value) => _verticalVelocity = value;

        /// <summary>
        /// 점프 입력을 처리합니다. (이단 점프 포함)
        /// </summary>
        private void HandleJumpInput()
        {
            // 지면에 있을 때 낙하 유예 시간 초기화 (HandleGroundedState에서 호출될 때)
            if (_isGrounded)
            {
                _fallTimeoutDelta = fallTimeout;
            }

            // 점프 입력 처리 (최대 점프 횟수 내에서만 허용, 물 속이 아닐 때만)
            if (_input.IsJumping && _currentJumpCount < maxJumpCount && !_isInWater)
            {
                // 점프 속도 계산 (중력과 높이 기반)
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                
                _currentJumpCount++;

                // 애니메이션 트리거 (트랜지션을 통해 재생되도록 Trigger 파라미터 사용)
                if (_animator)
                {
                    _animator.SetTrigger(AnimIDJump);
                }

                // 상태 변경 (이단 점프 시에도 로직 처리를 위해 Jump 상태로 진입 시도)
                if (_currentState != PlayerState.Jump)
                {
                    ChangeState(PlayerState.Jump);
                }
            }
        }

        /// <summary>
        /// 중력을 수직 속도에 적용합니다.
        /// </summary>
        /// <param name="deltaTime">프레임 증분 시간</param>
        private void ApplyGravity(float deltaTime)
        {
            // 물 속에 있다면 물의 수직 저항(Drag) 적용
            if (_isInWater)
            {
                _verticalVelocity = Mathf.Lerp(_verticalVelocity, 0f, waterDrag * deltaTime);
            }

            // 터미널 벨로시티 제한 (최대 낙하 속도)
            if (_verticalVelocity > -53f)
            {
                _verticalVelocity += gravity * deltaTime;
            }
        }

        /// <summary>
        /// 입력에 따른 수평 이동을 계산하고 적용합니다.
        /// </summary>
        /// <param name="deltaTime">프레임 증분 시간</param>
        private void ApplyMovement(float deltaTime)
        {
            // 1. 목표 속도 결정
            // 지면에 있을 때만 스프린트 상태를 갱신하여, 공중에서는 직전 상태를 유지하게 합니다.
            if (_isGrounded)
            {
                _isSprintingInternal = _input.IsSprinting;
            }

            float targetSpeed = _isSprintingInternal ? sprintSpeed : moveSpeed;
            if (_isInWater) targetSpeed = waterMoveSpeed; // 물 속일 때 이동 속도 덮어쓰기
            if (_input.MoveInput == Vector2.zero) targetSpeed = 0f;

            // 2. 현재 속도를 목표 속도로 보간
            _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, deltaTime * speedChangeRate);

            // 3. 이동 방향 계산 및 캐릭터 회전
            Vector3 moveDir = Vector3.zero;
            if (_input.MoveInput != Vector2.zero)
            {
                // 입력 방향 (X, Z 평면)
                Vector3 inputDirection = new Vector3(_input.MoveInput.x, 0.0f, _input.MoveInput.y).normalized;

                // 메인 카메라 기준 회전값 계산
                float targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
                if (Camera.main != null)
                {
                    targetRotation += Camera.main.transform.eulerAngles.y;
                }

                // 캐릭터 부드럽게 회전 (이동 방향 바라보기)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0.0f, targetRotation, 0.0f), deltaTime * 10f);
                
                // 최종 이동 방향 벡터 계산
                moveDir = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;
            }

            // 4. 최종 이동 적용 (수직 벨로시티 포함)
            Vector3 movement = moveDir * _currentSpeed + Vector3.up * _verticalVelocity;
            
            // [추가] 경사면 및 모서리 미끄러짐 보정 적용
            ApplySliding(ref movement);

            _controller.Move(movement * deltaTime);

            // 5. 애니메이션 파라미터 업데이트
            if (_animator)
            {
                // 실제 물리 속도를 기반으로 애니메이션 속도 결정 (수직 속도 제외)
                Vector3 horizontalVelocity = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z);
                float actualSpeed = horizontalVelocity.magnitude;

                // 벽에 막히는 등 실제 이동이 없을 경우 애니메이션이 멈추도록 함
                _animator.SetFloat(AnimIDSpeed, actualSpeed);
                _animator.SetFloat(AnimIDMotionSpeed, _input.MoveInput.magnitude);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Vector3 rayOrigin = transform.position + Vector3.up * 0.05f;
            float rayLength = (_controller != null) ? _controller.skinWidth + 0.1f : 0.2f;

            Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * rayLength);
            Gizmos.DrawWireSphere(rayOrigin + Vector3.down * rayLength, 0.05f);

            // 미끄러짐 체크용 레이 (중심)
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, Vector3.down * 0.2f);
        }

        /// <summary>
        /// 컨트롤러가 물체와 충돌할 때 호출되어 지면의 기울기 정보를 수집합니다.
        /// </summary>
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            _hitNormal = hit.normal;
        }

        /// <summary>
        /// 경사면이나 모서리에 있을 때 캐릭터를 미끄러뜨립니다.
        /// </summary>
        private void ApplySliding(ref Vector3 movement)
        {
            if (!_isGrounded) return;

            // 1. 경사면 체크 (CharacterController에 설정된 slopeLimit 기준)
            float slopeAngle = Vector3.Angle(Vector3.up, _hitNormal);
            bool isSteep = slopeAngle > (_controller != null ? _controller.slopeLimit : 45f);

            // 2. 모서리 체크 (캐릭터 중심 아래에 땅이 없는지)
            bool isOnEdge = !Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.25f, groundLayers, QueryTriggerInteraction.Ignore);

            if (isSteep || isOnEdge)
            {
                // 미끄러질 방향 (법선의 수평 성분)
                Vector3 slideDir = new Vector3(_hitNormal.x, 0f, _hitNormal.z);

                // 미끄러지는 강도 (경사면은 강하게, 모서리는 적당히)
                float slideSpeed = isSteep ? 5f : 2.5f;

                movement += slideDir * slideSpeed;

                // 미끄러지는 동안에는 아래로 살짝 힙을 주어 자연스러운 낙하 유도
                movement.y -= 2f;
            }
        }



        #region Animation Events

        /// <summary>
        /// 외부(Buoyancy 스크립트 등)에서 부력을 받아 수직 속도에 적용합니다.
        /// </summary>
        public void AddBuoyancy(float force)
        {
            // 부력을 수직 속도에 추가합니다. (OnTriggerStay의 FixedUpdate 주기를 고려해 보정)
            _verticalVelocity += force * Time.fixedDeltaTime;
        }

        /// <summary>
        /// 애니메이션 이벤트에서 호출되는 발소리 이벤트입니다.
        /// </summary>
        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                // TODO: 발소리 사운드 재생 로직 연결
                // Debug.Log("[PlayerController] Footstep Event Received");
            }
        }

        /// <summary>
        /// 애니메이션 이벤트에서 호출되는 착지 이벤트입니다.
        /// </summary>
        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                // TODO: 착지 사운드 재생 로직 연결
                // Debug.Log("[PlayerController] Land Event Received");
            }
        }

        #endregion
    }
}
