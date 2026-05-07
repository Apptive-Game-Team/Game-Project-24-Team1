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
        Climbing
    }

    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerController : MonoBehaviour
    {
        [Header("이동 설정")]
        [Tooltip("기본 이동 속도입니다.")]
        [SerializeField] private float moveSpeed = 5.0f;

        [Tooltip("전력 질주 시 속도입니다.")]
        [SerializeField] private float sprintSpeed = 8.0f;

        [Tooltip("속도 변화의 가속도 계수입니다.")]
        [SerializeField] private float speedChangeRate = 10.0f;

        [Tooltip("사다리 등반 속도입니다.")]
        [SerializeField] private float climbSpeed = 3.0f;

        [Header("점프 및 중력")]
        [Tooltip("점프 높이입니다.")]
        [SerializeField] private float jumpHeight = 1.2f;

        [Tooltip("적용될 중력 값입니다.")]
        [SerializeField] private float gravity = -15.0f;

        [Tooltip("낙하 상태로 판정되기까지의 대기 시간입니다.")]
        [SerializeField] private float fallTimeout = 0.15f;

        [Header("지면 체크 설정")]
        [Tooltip("지면으로 인식할 레이어 설정입니다.")]
        [SerializeField] private LayerMask groundLayers;

        [Header("시각적 보정")]
        [Tooltip("캐릭터가 공중에 떠 보일 경우 모델을 아래로 내리는 오프셋입니다. (보통 -0.08 권장)")]
        [SerializeField] private float modelYOffset = -0.08f;

        // 상태 변수
        private float _currentSpeed;
        private float _verticalVelocity;
        private float _fallTimeoutDelta;
        private bool _isGrounded = true;
        private bool _isNearLadder = false;
        private PlayerState _currentState;
        private PlayerState _previousState;
        private Vector3 _hitNormal = Vector3.up; // 지면의 기울기(법선) 정보
        private Vector3 _ladderForward = Vector3.forward; // 사다리가 바라보는 방향

        // 사다리 꼭대기 climb-over 상태 변수
        private bool _hasLadderTop = false;       // topPoint가 유효한지 여부
        private Vector3 _ladderTopPoint;          // 꼭대기 도달 목표 월드 좌표
        private bool _isClimbingOver = false;     // 꼭대기 올라가는 모션 중인지

        [Tooltip("꼭대기 판정 거리 (플레이어 Y 기준, 미터 단위)입니다.")]
        [SerializeField] private float climbTopThreshold = 0.5f;

        // 컴포넌트 캐싱
        private CharacterController _controller;
        private PlayerInputHandler _input;
        private Animator _animator;
        private PlayerWeapon _weapon; // 사격 컴포넌트 참조 추가

        // 애니메이터 파라미터 해시 캐싱 (성능 최적화)
        private static readonly int AnimIDSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimIDGrounded = Animator.StringToHash("Grounded");
        private static readonly int AnimIDJump = Animator.StringToHash("Jump");
        private static readonly int AnimIDFreeFall = Animator.StringToHash("FreeFall");
        private static readonly int AnimIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        private static readonly int AnimIDClimbing = Animator.StringToHash("Climbing");
        private static readonly int AnimIDClimbOver = Animator.StringToHash("ClimbOver"); // 꼭대기 climb-over 트리거

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

                // 물리 엔진의 Skin Width만큼 모델을 아래로 내려서 발을 땅에 붙임
                _animator.transform.localPosition = new Vector3(0f, modelYOffset, 0f);
            }

            _fallTimeoutDelta = fallTimeout;

            if (groundLayers.value == 0)
            {
                groundLayers = ~0; // 기본적으로 모든 레이어 체크
                Debug.LogWarning("[PlayerController] groundLayers가 설정되지 않아 모든 레이어를 지면으로 인식합니다.");
            }

            // 초기 상태 설정
            ChangeState(PlayerState.Idle);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            // ── climb-over는 state machine보다 먼저, 독립적으로 실행 ──
            // state가 Climbing 밖으로 바뀌더라도 중단되지 않습니다.
            if (_isClimbingOver)
            {
                HandleClimbOver(deltaTime);
                return;
            }

            // 모든 상태에서 공통적으로 필요한 처리
            CheckGrounded();
            HandleShootingInput();

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
                    HandleClimbingState(deltaTime);
                    break;
            }
        }

        /// <summary>
        /// 꼭대기 climb-over 모션을 독립적으로 처리합니다.
        /// Update() 최상단에서 호출되므로 state에 무관하게 실행됩니다.
        /// 종료 조건: Y 위치가 topPoint에 도달하면 자동 종료.
        /// </summary>
        private void HandleClimbOver(float deltaTime)
        {
            // 사다리를 바라보는 방향 유지
            Vector3 lookDirCO = new Vector3(_ladderForward.x, 0f, _ladderForward.z);
            if (lookDirCO.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotCO = Quaternion.LookRotation(-lookDirCO);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotCO, deltaTime * 15f);
            }

            // Y만 topPoint 높이로 이동 (X, Z 고정)
            float currentY = transform.position.y;
            float targetY = _ladderTopPoint.y;
            float newY = Mathf.MoveTowards(currentY, targetY, climbSpeed * 1.5f * deltaTime);
            _controller.Move(new Vector3(0f, newY - currentY, 0f));

            // 클라이밍 애니메이션 파라미터 강제 유지
            if (_animator)
            {
                _animator.SetBool(AnimIDClimbing, true);
                _animator.SetFloat(AnimIDSpeed, climbSpeed);
                _animator.SetFloat(AnimIDMotionSpeed, 1f);
            }

            // Y가 topPoint에 도달하면 종료
            if (Mathf.Abs(transform.position.y - targetY) < 0.01f)
            {
                FinishClimbOver();
            }
        }

        /// <summary>
        /// climb-over를 종료하고 정상 상태로 복귀합니다.
        /// HandleClimbOver에서 Y 도달 시 자동 호출됩니다.
        /// 또한 Animation Event에서 직접 호출해도 됩니다.
        /// </summary>
        public void FinishClimbOver()
        {
            if (!_isClimbingOver) return; // 중복 호출 방지

            _input.EnableInput();
            _isClimbingOver = false;
            ChangeState(PlayerState.Idle);
        }

        /// <summary>
        /// 플레이어의 상태를 변경하고 필요한 초기화 로직을 수행합니다.
        /// </summary>
        /// <param name="newState">새로운 상태</param>
        private void ChangeState(PlayerState newState)
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
                        _animator.SetBool(AnimIDJump, false);
                        _animator.SetBool(AnimIDFreeFall, false);
                        _animator.SetBool(AnimIDClimbing, false);
                    }
                    break;
                case PlayerState.Move:
                    if (_animator)
                    {
                        _animator.SetBool(AnimIDJump, false);
                        _animator.SetBool(AnimIDFreeFall, false);
                        _animator.SetBool(AnimIDClimbing, false);
                    }
                    break;
                case PlayerState.Jump:
                    if (_animator)
                    {
                        _animator.SetBool(AnimIDJump, true);
                        _animator.SetBool(AnimIDClimbing, false);
                    }
                    break;
                case PlayerState.Fall:
                    if (_animator)
                    {
                        _animator.SetBool(AnimIDFreeFall, true);
                        _animator.SetBool(AnimIDClimbing, false);
                    }
                    break;
                case PlayerState.Climbing:
                    _verticalVelocity = 0f; // 사다리 진입 시 속도 초기화
                    if (_animator)
                    {
                        _animator.SetBool(AnimIDClimbing, true);
                        _animator.SetBool(AnimIDJump, false);
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
            if (_verticalVelocity < 0f) _verticalVelocity = -2f;

            ApplyMovement(deltaTime);
            HandleJumpInput();

            // 사다리 타기 체크
            if (_isNearLadder && Mathf.Abs(_input.MoveInput.y) > 0.1f)
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

            // 사다리 타기 체크
            if (_isNearLadder && Mathf.Abs(_input.MoveInput.y) > 0.1f)
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

        private void HandleJumpInput()
        {
            if (_isGrounded)
            {
                // 지면에 닿아있으면 낙하 유예 시간 초기화
                _fallTimeoutDelta = fallTimeout;

                // 점프 입력 처리 (쿨다운 없이 즉시 실행)
                if (_input.IsJumping)
                {
                    _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
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
            float targetSpeed = _input.IsSprinting ? sprintSpeed : moveSpeed;
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

            // 미끄러짘 체크용 레이 (중심)
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


        /// <summary>
        /// 사다리 타기 상태의 로직을 처리합니다.
        /// </summary>
        private void HandleClimbingState(float deltaTime)
        {
            // ── 1. 사다리를 바라보도록 캐릭터 방향 고정 ──
            Vector3 lookDir = new Vector3(_ladderForward.x, 0f, _ladderForward.z);
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(-lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, deltaTime * 15f);
            }

            // ── 2. 스페이스바 → 뒤로 점프 후 클라이밍 취소 ──
            if (_input.IsJumping)
            {
                Vector3 jumpBackDir = lookDir.normalized;
                float horizontalForce = moveSpeed * 1.2f;
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                _controller.Move(jumpBackDir * horizontalForce * deltaTime + Vector3.up * _verticalVelocity * deltaTime);
                ChangeState(PlayerState.Jump);
                return;
            }

            // ── 3. 사다리 영역 이탈 시 자동 탈출 ──
            if (!_isNearLadder)
            {
                ChangeState(_isGrounded ? PlayerState.Idle : PlayerState.Fall);
                return;
            }

            // ── 4. 사다리 수직 이동 ──
            float verticalMove = _input.MoveInput.y * climbSpeed;
            _controller.Move(Vector3.up * verticalMove * deltaTime);

            // ── 5. 꼭대기 도달 체크 (topPoint 유효 + 위로 이동 중) ──
            if (_hasLadderTop && verticalMove > 0.1f)
            {
                float distToTop = _ladderTopPoint.y - transform.position.y;
                if (distToTop <= climbTopThreshold)
                {
                    // climb-over 시작: 입력 차단
                    _isClimbingOver = true;
                    _input.DisableInput();
                    if (_animator) _animator.SetTrigger(AnimIDClimbOver);
                    // 이후 프레임부터 Update()에서 HandleClimbOver()가 수행
                    return;
                }
            }

            // ── 6. 애니메이션 파라미터 업데이트 ──
            if (_animator)
            {
                _animator.SetFloat(AnimIDSpeed, Mathf.Abs(verticalMove));
                float motionSpeed = 0f;
                if (_input.MoveInput.y > 0.1f)       motionSpeed =  1f;
                else if (_input.MoveInput.y < -0.1f) motionSpeed = -1f;
                _animator.SetFloat(AnimIDMotionSpeed, motionSpeed);
            }

            // ── 7. 바닥에서 아래 입력 시 하강 완료 탈출 ──
            if (_isGrounded && verticalMove < -0.1f)
            {
                ChangeState(PlayerState.Idle);
            }
        }

        /// <param name="ladderForward">사다리 Transform.forward</param>
        /// <param name="topPoint">사다리 꼭대기 월드 좌표 (hasTop=false 이면 무시)</param>
        /// <param name="hasTop">topPoint 가 유효한지 여부</param>
        public void SetNearLadder(bool value, Vector3 ladderForward, Vector3 topPoint, bool hasTop)
        {
            _isNearLadder = value;

            if (value)
            {
                // 사다리의 정면 방향을 캐싱 (클라이밍 중 방향 고정에 사용)
                _ladderForward = ladderForward;
                _ladderTopPoint = topPoint;
                _hasLadderTop = hasTop;
            }
            else
            {
                _hasLadderTop = false;
                // climb-over 중이면 아무것도 건드리지 않음
                // (꼭대기를 넘어가는 도중 trigger가 꺼져도 모션 유지)
            }

            // 사다리에서 멀어졌는데 아직 Climbing 상태라면 상태 변경
            // (climb-over 진행 중에는 상태 변경 건너뜀)
            if (!value && _currentState == PlayerState.Climbing && !_isClimbingOver)
            {
                ChangeState(_isGrounded ? PlayerState.Idle : PlayerState.Fall);
            }
        }

        #region Animation Events

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
