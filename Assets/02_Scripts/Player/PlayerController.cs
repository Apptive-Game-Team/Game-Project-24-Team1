using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("기본 이동 속도입니다.")]
    public float moveSpeed = 5.0f;
    [Tooltip("달리기 시 이동 속도입니다.")]
    public float sprintSpeed = 8.0f;
    [Tooltip("이동 방향 전환 시 부드러움 정도입니다.")]
    public float rotationSmoothTime = 0.12f;
    [Tooltip("속도 변화의 가속도입니다.")]
    public float speedChangeRate = 10.0f;

    [Header("Jump & Gravity")]
    [Tooltip("점프 높이입니다.")]
    public float jumpHeight = 1.2f;
    [Tooltip("중력 가속도입니다.")]
    public float gravity = -15.0f;
    [Tooltip("다시 점프하기까지의 대기 시간입니다.")]
    public float jumpTimeout = 0.50f;
    [Tooltip("낙하 상태로 판정되기까지의 대기 시간입니다.")]
    public float fallTimeout = 0.15f;

    [Header("Player Grounded")]
    [Tooltip("캐릭터가 바닥에 닿아있는지 여부입니다.")]
    public bool grounded = true;
    [Tooltip("바닥 체크 시 사용할 오프셋입니다.")]
    public float groundedOffset = -0.14f;
    [Tooltip("바닥 체크 영역의 반지름입니다.")]
    public float groundedRadius = 0.28f;
    [Tooltip("바닥으로 인식할 레이어입니다.")]
    public LayerMask groundLayers;

    [Header("Character Controller Settings (처리 강도)")]
    [Tooltip("캐릭터가 오를 수 있는 최대 경사 각도입니다. (값이 클수록 가파른 곳을 오름)")]
    [Range(0, 90)] public float slopeLimit = 45f;
    [Tooltip("캐릭터가 넘을 수 있는 턱의 높이입니다. (값이 클수록 높은 턱을 넘음)")]
    public float stepOffset = 0.3f;

    private float _speed;
    private float _animationBlend;
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    private CharacterController _controller;
    private PlayerInputHandler _input;
    private Animator _animator;
    private GameObject _mainCamera;

    private void Awake()
    {
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<PlayerInputHandler>();
        _animator = GetComponent<Animator>();

        _controller.slopeLimit = slopeLimit;
        _controller.stepOffset = stepOffset;

        _jumpTimeoutDelta = jumpTimeout;
        _fallTimeoutDelta = fallTimeout;
    }

    private void Update()
    {
        _controller.slopeLimit = slopeLimit;
        _controller.stepOffset = stepOffset;

        CheckGrounded();
        MovePlayer();
        HandleVerticalMovement();
    }

    private void CheckGrounded()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z);
        grounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);

        if (_animator != null)
        {
            _animator.SetBool("Grounded", grounded);
        }
    }

    private void MovePlayer()
    {
        float targetSpeed = _input.IsSprinting ? sprintSpeed : moveSpeed;
        if (_input.MoveInput == Vector2.zero) targetSpeed = 0.0f;

        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
        float speedOffset = 0.1f;
        float inputMagnitude = _input.MoveInput.magnitude;

        if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * speedChangeRate);
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * speedChangeRate);
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        Vector3 inputDirection = new Vector3(_input.MoveInput.x, 0.0f, _input.MoveInput.y).normalized;

        if (_input.MoveInput != Vector2.zero)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, rotationSmoothTime);

            if (!_input.IsAiming)
            {
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }
        }

        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

        _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

        if (_animator != null)
        {
            _animator.SetFloat("Speed", _animationBlend);
            _animator.SetFloat("MotionSpeed", inputMagnitude);
        }
    }

    private void HandleVerticalMovement()
    {
        if (grounded)
        {
            _fallTimeoutDelta = fallTimeout;

            if (_animator != null)
            {
                _animator.SetBool("Jump", false);
                _animator.SetBool("FreeFall", false);
            }

            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            if (_input.IsJumping && _jumpTimeoutDelta <= 0.0f)
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

                if (_animator != null)
                {
                    _animator.SetBool("Jump", true);
                }
            }

            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            _jumpTimeoutDelta = jumpTimeout;

            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                if (_animator != null)
                {
                    _animator.SetBool("FreeFall", true);
                }
            }
        }

        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += gravity * Time.deltaTime;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z), groundedRadius);
    }
}
