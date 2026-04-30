using UnityEngine;

namespace Nexush.Player
{
    /// <summary>
    /// 플레이어의 실제 이동과 물리 처리를 담당하는 컨트롤러 클래스입니다.
    /// CharacterController를 사용하여 이동, 점프, 중력 로직을 수행합니다.
    /// </summary>
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

        [Header("점프 및 중력")]
        [Tooltip("점프 높이입니다.")]
        [SerializeField] private float jumpHeight = 1.2f;
        
        [Tooltip("적용될 중력 값입니다.")]
        [SerializeField] private float gravity = -15.0f;
        
        [Tooltip("점프 후 재점프가 가능해지기까지의 대기 시간입니다.")]
        [SerializeField] private float jumpTimeout = 0.50f;
        
        [Tooltip("낙하 상태로 판정되기까지의 대기 시간입니다.")]
        [SerializeField] private float fallTimeout = 0.15f;

        [Header("지면 체크 설정")]
        [Tooltip("지면으로 인식할 레이어들입니다.")]
        [SerializeField] private LayerMask groundLayers;
        
        [Tooltip("지면 체크 구체의 오프셋입니다.")]
        [SerializeField] private float groundedOffset = -0.14f;
        
        [Tooltip("지면 체크 구체의 반지름입니다.")]
        [SerializeField] private float groundedRadius = 0.28f;

        // 상태 변수
        private float _currentSpeed;
        private float _verticalVelocity;
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;
        private bool _isGrounded = true;

        // 컴포넌트 캐싱
        private CharacterController _controller;
        private PlayerInputHandler _input;
        private Animator _animator;

        // 애니메이터 파라미터 해시 캐싱 (성능 최적화)
        private static readonly int AnimIDSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimIDGrounded = Animator.StringToHash("Grounded");
        private static readonly int AnimIDJump = Animator.StringToHash("Jump");
        private static readonly int AnimIDFreeFall = Animator.StringToHash("FreeFall");
        private static readonly int AnimIDMotionSpeed = Animator.StringToHash("MotionSpeed");

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<PlayerInputHandler>();
            _animator = GetComponent<Animator>();

            _jumpTimeoutDelta = jumpTimeout;
            _fallTimeoutDelta = fallTimeout;

            if (groundLayers.value == 0)
            {
                groundLayers = ~0; // 기본적으로 모든 레이어 체크
                Debug.LogWarning("[PlayerController] groundLayers가 설정되지 않아 모든 레이어를 지면으로 인식합니다.");
            }
        }

        private void Update()
        {
            // 지면 체크 및 점프 입력은 프레임별로 업데이트
            CheckGrounded();
            HandleJumpInput();
            
            // 💡 CharacterController는 시각적 매끄러움(Jitter 방지)을 위해 Update에서 이동을 처리합니다.
            // 물리 로직은 규칙을 준수하되, 실행 타이밍은 렌더링 프레임에 동기화합니다.
            float deltaTime = Time.deltaTime;
            ApplyGravity(deltaTime);
            ApplyMovement(deltaTime);
        }

        /// <summary>
        /// 플레이어 하단의 구체 체크를 통해 지면에 닿아있는지 확인합니다.
        /// </summary>
        private void CheckGrounded()
        {
            Vector3 pos = transform.position;
            Vector3 checkPos = new Vector3(pos.x, pos.y - groundedOffset, pos.z);
            _isGrounded = Physics.CheckSphere(checkPos, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);

            if (_animator)
            {
                _animator.SetBool(AnimIDGrounded, _isGrounded);
            }
        }

        /// <summary>
        /// 점프 입력을 확인하고 쿨다운을 처리합니다.
        /// </summary>
        private void HandleJumpInput()
        {
            if (_isGrounded)
            {
                _fallTimeoutDelta = fallTimeout;
                
                if (_animator)
                {
                    _animator.SetBool(AnimIDJump, false);
                    _animator.SetBool(AnimIDFreeFall, false);
                }

                // 착지 시 수직 속도 초기화
                if (_verticalVelocity < 0f)
                {
                    _verticalVelocity = -2f;
                }

                // 점프 입력 처리
                if (_input.IsJumping && _jumpTimeoutDelta <= 0f)
                {
                    _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    if (_animator)
                    {
                        _animator.SetBool(AnimIDJump, true);
                    }
                }

                if (_jumpTimeoutDelta > 0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                _jumpTimeoutDelta = jumpTimeout;

                if (_fallTimeoutDelta > 0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else if (_animator)
                {
                    _animator.SetBool(AnimIDFreeFall, true);
                }
            }
        }

        /// <summary>
        /// 중력을 수직 속도에 적용합니다.
        /// </summary>
        /// <param name="deltaTime">프레임 증분 시간</param>
        private void ApplyGravity(float deltaTime)
        {
            // 터미널 벨로시티 제한
            if (_verticalVelocity < 53f)
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
            if (_input.MoveInput == Vector2.zero)
            {
                targetSpeed = 0f;
            }

            // 2. 현재 속도를 목표 속도로 보간
            _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, deltaTime * speedChangeRate);

            // 3. 이동 방향 계산 (항상 플레이어가 바라보는 방향 기준)
            Vector3 moveDir = (transform.forward * _input.MoveInput.y + transform.right * _input.MoveInput.x).normalized;

            // 4. 최종 이동 적용 (수직 벨로시티 포함)
            Vector3 movement = moveDir * _currentSpeed + Vector3.up * _verticalVelocity;
            _controller.Move(movement * deltaTime);

            // 5. 애니메이션 파라미터 업데이트
            if (_animator)
            {
                _animator.SetFloat(AnimIDSpeed, _currentSpeed);
                _animator.SetFloat(AnimIDMotionSpeed, _input.MoveInput.magnitude);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _isGrounded ? new Color(0f, 1f, 0f, 0.35f) : new Color(1f, 0f, 0f, 0.35f);
            Vector3 pos = transform.position;
            Gizmos.DrawSphere(new Vector3(pos.x, pos.y - groundedOffset, pos.z), groundedRadius);
        }
    }
}
