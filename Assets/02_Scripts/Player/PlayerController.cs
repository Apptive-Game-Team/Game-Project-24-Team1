using UnityEngine;

/// <summary>
/// 플레이어 이동 컨트롤러
/// - 회전은 PlayerCameraController에서 전담합니다.
/// - 오직 WASD 입력에 따른 물리적 이동과 점프/중력만 처리합니다.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 5.0f;
    public float sprintSpeed = 8.0f;
    public float speedChangeRate = 10.0f;

    [Header("점프 / 중력")]
    public float jumpHeight = 1.2f;
    public float gravity = -15.0f;
    public float jumpTimeout = 0.50f;
    public float fallTimeout = 0.15f;

    [Header("지면 판정")]
    public bool grounded = true;
    public float groundedOffset = -0.14f;
    public float groundedRadius = 0.28f;
    [Tooltip("비워두면 모든 레이어를 지면으로 인식")]
    public LayerMask groundLayers;

    private float _currentSpeed;
    private float _verticalVelocity;
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    private CharacterController _controller;
    private PlayerInputHandler _input;
    private Animator _animator;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<PlayerInputHandler>();
        _animator = GetComponent<Animator>();

        _jumpTimeoutDelta = jumpTimeout;
        _fallTimeoutDelta = fallTimeout;

        if (groundLayers.value == 0)
        {
            groundLayers = ~0;
            Debug.LogWarning("[PlayerController] groundLayers 미설정 → 모든 레이어를 지면으로 인식합니다.");
        }
    }

    private void Update()
    {
        CheckGrounded();
        HandleGravityAndJump();
        Move();
    }

    private void CheckGrounded()
    {
        Vector3 pos = transform.position;
        Vector3 checkPos = new Vector3(pos.x, pos.y - groundedOffset, pos.z);
        grounded = Physics.CheckSphere(checkPos, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);

        if (_animator) _animator.SetBool("Grounded", grounded);
    }

    private void HandleGravityAndJump()
    {
        if (grounded)
        {
            _fallTimeoutDelta = fallTimeout;
            if (_animator)
            {
                _animator.SetBool("Jump", false);
                _animator.SetBool("FreeFall", false);
            }

            if (_verticalVelocity < 0f)
                _verticalVelocity = -2f;

            if (_input.IsJumping && _jumpTimeoutDelta <= 0f)
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (_animator) _animator.SetBool("Jump", true);
            }

            if (_jumpTimeoutDelta > 0f)
                _jumpTimeoutDelta -= Time.deltaTime;
        }
        else
        {
            _jumpTimeoutDelta = jumpTimeout;

            if (_fallTimeoutDelta > 0f)
                _fallTimeoutDelta -= Time.deltaTime;
            else if (_animator)
                _animator.SetBool("FreeFall", true);
        }

        if (_verticalVelocity < 53f)
            _verticalVelocity += gravity * Time.deltaTime;
    }

    private void Move()
    {
        // 1. 목표 속도 결정
        float targetSpeed = _input.IsSprinting ? sprintSpeed : moveSpeed;
        if (_input.MoveInput == Vector2.zero) targetSpeed = 0f;

        // 2. 현재 속도를 목표 속도로 부드럽게 보간
        _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, Time.deltaTime * speedChangeRate);

        // 3. 이동 방향 계산 (항상 플레이어가 바라보는 방향 기준)
        // transform.forward는 카메라가 바라보는 전방 방향과 같습니다 (CameraController에서 그렇게 회전시키기 때문)
        Vector3 moveDir = (transform.forward * _input.MoveInput.y + transform.right * _input.MoveInput.x).normalized;

        // 4. 최종 이동
        Vector3 movement = moveDir * _currentSpeed + Vector3.up * _verticalVelocity;
        _controller.Move(movement * Time.deltaTime);

        // 5. 애니메이션
        if (_animator)
        {
            _animator.SetFloat("Speed", _currentSpeed);
            _animator.SetFloat("MotionSpeed", _input.MoveInput.magnitude);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = grounded ? new Color(0f, 1f, 0f, 0.35f) : new Color(1f, 0f, 0f, 0.35f);
        Vector3 pos = transform.position;
        Gizmos.DrawSphere(new Vector3(pos.x, pos.y - groundedOffset, pos.z), groundedRadius);
    }
}
