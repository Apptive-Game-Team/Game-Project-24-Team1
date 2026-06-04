using UnityEngine;

namespace MushOut.Player
{
    /// <summary>
    /// 플레이어의 이동, 점프, 대시, 물리 처리를 담당하는 모터 컴포넌트입니다.
    /// CharacterController를 통해 실제 이동을 수행하며, 중력·부력·외부 힘 등을 통합 관리합니다.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("이동 설정")]
        [Tooltip("기본 이동 속도입니다.")]
        [SerializeField] private float _moveSpeed = 5.0f;

        [Tooltip("대시 거리입니다.")]
        [SerializeField] private float _dashDistance = 3.0f;

        [Tooltip("대시 소요 시간입니다.")]
        [SerializeField] private float _dashDuration = 0.2f;

        [Tooltip("속도 변화의 가속도 계수입니다.")]
        [SerializeField] private float _speedChangeRate = 10.0f;

        [Header("밀고 당기기 설정")]
        [Tooltip("밀고 당길 때의 이동 속도입니다.")]
        [SerializeField] private float _pushPullSpeed = 2.0f;

        [Header("점프 및 중력")]
        [Tooltip("점프 높이입니다.")]
        [SerializeField] private float _jumpHeight = 1.2f;

        [Tooltip("적용될 중력 값입니다.")]
        [SerializeField] private float _gravity = -15.0f;

        [Header("물(Water) 설정")]
        [Tooltip("물 속에서의 이동 속도입니다.")]
        [SerializeField] private float _waterMoveSpeed = 2.0f;

        [Tooltip("물 속에서 받는 수직 저항력(Drag)입니다.")]
        [SerializeField] private float _waterDrag = 3.0f;

        [Header("벽 붙기 설정")]
        [Tooltip("벽으로 인식할 레이어입니다.")]
        [SerializeField] private LayerMask _wallLayer;
        [Tooltip("벽을 감지할 최대 거리입니다.")]
        [SerializeField] private float _wallClingDistance = 0.5f;
        [Tooltip("벽에 붙어있을 때 수평 이동(WASD)을 허용할지 여부입니다.")]
        [SerializeField] private bool _allowHorizontalMovementWhileClinging = false;

        public float VerticalVelocity { get; set; }
        public Vector3 ExternalVelocity { get; set; }

        public bool IsClinging { get; private set; }

        /// <summary>중력 값을 외부에서 읽을 수 있는 프로퍼티입니다.</summary>
        public float Gravity => _gravity;

        /// <summary>점프 높이를 외부에서 읽을 수 있는 프로퍼티입니다.</summary>
        public float JumpHeight => _jumpHeight;

        /// <summary>기본 이동 속도를 외부에서 읽을 수 있는 프로퍼티입니다.</summary>
        public float MoveSpeed => _moveSpeed;

        /// <summary>밀고 당기기 이동 속도를 외부에서 읽을 수 있는 프로퍼티입니다.</summary>
        public float PushPullSpeed => _pushPullSpeed;

        /// <summary>물 속 이동 속도를 외부에서 읽을 수 있는 프로퍼티입니다.</summary>
        public float WaterMoveSpeed => _waterMoveSpeed;

        private float _buoyancyTargetY = float.MinValue;
        private float _buoyancyPower;
        private float _buoyancyTime;

        private CharacterController _controller;
        private float _currentSpeed;

        private bool _isDashing;
        private bool _wasSprinting;

        private AbilityController _abilityController;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _abilityController = GetComponent<AbilityController>();
        }

        /// <summary>
        /// 벽 붙기 상태를 업데이트합니다. (공중에 있을 때만 가능)
        /// </summary>
        /// <param name="isClingKeyPressed">붙기 키(좌클릭 등) 입력 여부</param>
        /// <param name="isGrounded">지면 접촉 여부</param>
        /// <param name="isInWater">물 속 여부</param>
        public void UpdateWallCling(bool isClingKeyPressed, bool isGrounded, bool isInWater)
        {
            // 키를 누르지 않았거나, 땅에 있거나, 물 속에 있으면 벽 붙기 불가
            if (!isClingKeyPressed || isGrounded || isInWater)
            {
                IsClinging = false;
                return;
            }

            // 플레이어 중심에서 구형 캐스트를 통해 벽 레이어를 감지
            float checkRadius = _controller.radius + _wallClingDistance;
            Vector3 center = transform.position + _controller.center;
            IsClinging = Physics.CheckSphere(center, checkRadius, _wallLayer);
        }

        /// <summary>
        /// 중력을 수직 속도에 누적 적용합니다.
        /// 물 속에서는 수직 저항(Drag)으로 대체하고, 지면 위에서는 소량의 하방 속도를 유지합니다.
        /// </summary>
        /// <param name="deltaTime">프레임 델타 시간</param>
        /// <param name="isInWater">물 속 여부</param>
        /// <param name="isGrounded">지면 접촉 여부</param>
        public void ApplyGravity(float deltaTime, bool isInWater, bool isGrounded)
        {
            if (IsClinging)
            {
                VerticalVelocity = 0f;
                return; // 벽에 붙어있을 때는 중력 무시
            }

            if (isInWater)
            {
                // 물 속에서는 수직 저항(Drag) 적용
                VerticalVelocity = Mathf.Lerp(VerticalVelocity, 0f, _waterDrag * deltaTime);
            }
            else if (isGrounded && VerticalVelocity < 0f)
            {
                // 지면 착지 유지용: CharacterController를 지면에 고정시키는 소량의 하방 속도
                // 이 값이 없으면 지면을 걷는 동안 중력이 계속 누적되어 절벽에서 즉시 추락하는 현상 발생
                VerticalVelocity = -2f;
            }

            bool applyGravity = true;

            // 물 속이고 최근(0.1초 내)에 부력 정보가 업데이트 되었다면, PlayerMotor에서 부드럽게 부력 적용 (덜덜 떨림 방지)
            if (isInWater && Time.time - _buoyancyTime < 0.1f)
            {
                float bottomY = transform.position.y;
                
                if (bottomY < _buoyancyTargetY)
                {
                    float diff = _buoyancyTargetY - bottomY;
                    float targetRiseVelocity = Mathf.Min(diff / deltaTime, _buoyancyPower);
                    float upwardAcceleration = _buoyancyPower * 5f;

                    if (VerticalVelocity < targetRiseVelocity)
                    {
                        VerticalVelocity = Mathf.MoveTowards(VerticalVelocity, targetRiseVelocity, upwardAcceleration * deltaTime);
                    }
                    else
                    {
                        VerticalVelocity = targetRiseVelocity;
                    }
                    
                    applyGravity = false; // 부력으로 상승 중일 때는 중력을 무시
                }
                else if (bottomY <= _buoyancyTargetY + 0.05f)
                {
                    VerticalVelocity = 0f;
                    applyGravity = false; // 수면 고정 시 중력 무시
                }
            }

            if (applyGravity && VerticalVelocity > -53f)
            {
                VerticalVelocity += _gravity * deltaTime;
            }
        }

        /// <summary>
        /// 입력 값과 현재 상태를 바탕으로 플레이어를 이동시킵니다.
        /// 대시·밀고당기기·물 속 이동·미끄러짐 보정 등을 통합 처리합니다.
        /// </summary>
        /// <param name="deltaTime">프레임 델타 시간</param>
        /// <param name="moveInput">수평 이동 입력 (X: 좌우, Y: 앞뒤)</param>
        /// <param name="isSprinting">대시(스프린트) 입력 여부</param>
        /// <param name="isInWater">물 속 여부</param>
        /// <param name="isGrounded">지면 접촉 여부</param>
        /// <param name="grabbedObject">현재 잡고 있는 밀고당기기 오브젝트</param>
        /// <param name="hitNormal">지면의 법선 벡터</param>
        /// <param name="groundLayers">지면으로 인식할 레이어 마스크</param>
        public void ApplyMovement(float deltaTime, Vector2 moveInput, bool isSprinting, bool isInWater, bool isGrounded, MushOut.Interactables.PushPullInteractable grabbedObject, Vector3 hitNormal, LayerMask groundLayers)
        {
            if (IsClinging && !_allowHorizontalMovementWhileClinging)
            {
                moveInput = Vector2.zero;
            }

            if (isSprinting && !_wasSprinting && !_isDashing && grabbedObject == null)
            {
                bool canDash = true;
                if (_abilityController != null)
                {
                    if (_abilityController.DashCount > 0)
                    {
                        _abilityController.UseDash();
                    }
                    else
                    {
                        canDash = false;
                    }
                }

                if (canDash)
                {
                    StartCoroutine(DashRoutine());
                }
            }
            _wasSprinting = isSprinting;

            if (_isDashing)
            {
                // 대시 중에는 기본 이동을 무시하고 중력 및 외부 힘만 처리합니다.
                Vector3 dashMovement = Vector3.up * VerticalVelocity;
                if (ExternalVelocity.sqrMagnitude > 0.001f)
                {
                    dashMovement += ExternalVelocity;
                }
                _controller.Move(dashMovement * deltaTime);
                return;
            }

            float targetSpeed = _moveSpeed;
            if (isInWater) targetSpeed = _waterMoveSpeed;
            if (grabbedObject != null) targetSpeed = Mathf.Min(targetSpeed, _pushPullSpeed);

            if (grabbedObject != null && grabbedObject.movementType == MushOut.Interactables.PushPullMovementType.ForwardBackwardOnly)
            {
                if (Mathf.Abs(moveInput.y) < 0.01f) targetSpeed = 0f;
            }
            else
            {
                if (moveInput == Vector2.zero) targetSpeed = 0f;
            }

            _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, deltaTime * _speedChangeRate);

            Vector3 moveDir = Vector3.zero;

            if (grabbedObject != null && grabbedObject.movementType == MushOut.Interactables.PushPullMovementType.ForwardBackwardOnly)
            {
                if (Mathf.Abs(moveInput.y) > 0.01f)
                {
                    moveDir = transform.forward * Mathf.Sign(moveInput.y);
                }
            }
            else if (moveInput != Vector2.zero)
            {
                Vector3 inputDirection = new Vector3(moveInput.x, 0.0f, moveInput.y).normalized;
                float targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
                if (Camera.main != null)
                {
                    targetRotation += Camera.main.transform.eulerAngles.y;
                }

                if (grabbedObject == null)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0.0f, targetRotation, 0.0f), deltaTime * 10f);
                }

                moveDir = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;

            }

            if (grabbedObject != null && moveDir.sqrMagnitude > 0.001f)
            {
                Vector3 toObject = grabbedObject.transform.position - transform.position;
                toObject.y = 0;
                
                // 1. 밀고 당기기 가능 여부 확인
                if (!grabbedObject.CanMoveInDirection(moveDir, toObject))
                {
                    moveDir = Vector3.zero;
                }
                else
                {
                    // 2. 오브젝트가 막혔을 때 플레이어가 뚫고 들어가는 현상(클리핑) 방지
                    // 플레이어가 오브젝트를 향해 이동할 때 (밀기)
                    if (Vector3.Dot(moveDir, toObject) > 0)
                    {
                        Vector3 grabOffset = grabbedObject.GrabOffset;
                        grabOffset.y = 0;
                        
                        // 현재 수평 거리가 처음 잡았을 때의 수평 거리보다 눈에 띄게 짧아졌다면 (즉 뚫고 들어갔다면 이동 차단)
                        if (toObject.magnitude < grabOffset.magnitude - 0.1f)
                        {
                            moveDir = Vector3.zero;
                        }
                    }
                }
            }

            Vector3 movement = moveDir * _currentSpeed + Vector3.up * VerticalVelocity;

            if (ExternalVelocity.sqrMagnitude > 0.001f)
            {
                float drag = isInWater ? _waterDrag : (isGrounded ? _speedChangeRate : 2.0f);
                ExternalVelocity = Vector3.Lerp(ExternalVelocity, Vector3.zero, deltaTime * drag);
                movement += ExternalVelocity;
            }
            else
            {
                ExternalVelocity = Vector3.zero;
            }

            // 미끄러짐 보정 적용
            movement += CalculateSliding(isGrounded, hitNormal, groundLayers);

            _controller.Move(movement * deltaTime);
        }

        /// <summary>
        /// 점프를 즉시 실행합니다.
        /// 점프 높이와 중력을 기반으로 초기 수직 속도를 계산하여 적용합니다.
        /// </summary>
        public void ExecuteJump()
        {
            VerticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
        }

        /// <summary>
        /// 대시 동작을 코루틴으로 처리합니다.
        /// 대시 지속 시간 동안 플레이어 전방으로 고속 이동하며 중력을 무시합니다.
        /// </summary>
        private System.Collections.IEnumerator DashRoutine()
        {
            _isDashing = true;
            float elapsed = 0f;
            float speed = _dashDistance / _dashDuration;
            Vector3 dashDir = transform.forward; // 플레이어 전방

            while (elapsed < _dashDuration)
            {
                VerticalVelocity = 0f; // 대시 중에는 중력 무시 (체공 유지)
                Vector3 movement = dashDir * speed + Vector3.up * VerticalVelocity;
                _controller.Move(movement * Time.deltaTime);

                elapsed += Time.deltaTime;
                yield return null;
            }

            _isDashing = false;
        }

        /// <summary>
        /// 경사면 및 모서리 엣지 감지 시 미끄러짐 방향과 크기를 계산합니다.
        /// 지면이 아닌 경우 <see cref="Vector3.zero"/>를 반환합니다.
        /// </summary>
        /// <param name="isGrounded">지면 접촉 여부</param>
        /// <param name="hitNormal">충돌 지면의 법선 벡터</param>
        /// <param name="groundLayers">지면으로 인식할 레이어 마스크</param>
        /// <returns>적용할 미끄러짐 이동 벡터</returns>
        private Vector3 CalculateSliding(bool isGrounded, Vector3 hitNormal, LayerMask groundLayers)
        {
            if (!isGrounded) return Vector3.zero;

            float slopeAngle = Vector3.Angle(Vector3.up, hitNormal);
            bool isSteep = slopeAngle > (_controller != null ? _controller.slopeLimit : 45f);
            bool isOnEdge = !Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.25f, groundLayers, QueryTriggerInteraction.Ignore);

            if (isSteep || isOnEdge)
            {
                Vector3 slideDir = new Vector3(hitNormal.x, 0f, hitNormal.z);
                float slideSpeed = isSteep ? 5f : 2.5f;

                Vector3 slideMovement = slideDir * slideSpeed;
                slideMovement.y -= 2f;
                return slideMovement;
            }

            return Vector3.zero;
        }

        /// <summary>
        /// 플레이어에게 부력을 적용합니다.
        /// 물리 업데이트(FixedUpdate)에서 호출되며 수직 속도를 증가시킵니다.
        /// </summary>
        /// <param name="force">적용할 부력 크기</param>
        public void AddBuoyancy(float force)
        {
            VerticalVelocity += force * Time.fixedDeltaTime;
        }

        /// <summary>
        /// 외부에서 물리 힘을 추가합니다.
        /// 물리 업데이트(FixedUpdate)에서 호출되며 외부 속도 벡터에 누적됩니다.
        /// </summary>
        /// <param name="force">적용할 외부 힘 벡터</param>
        public void AddExternalForce(Vector3 force)
        {
            ExternalVelocity += force * Time.fixedDeltaTime;
        }

        /// <summary>
        /// Buoyancy (부력) 목표 지점을 설정합니다.
        /// Buoyancy.cs 등에서 호출되며, 플레이어가 물 속에서 수면으로 떠오르도록 유도합니다.
        /// </summary>
        public void SetBuoyancyTarget(float targetY, float power)
        {
            // 같은 물리 프레임(FixedUpdate) 내에서 여러 번 호출된 경우 (예: 물과 간헐천 겹침)
            // 더 높은 수면을 목표로 하거나, 같은 수면이면 더 강한 부력을 우선시합니다.
            if (Time.fixedTime == _buoyancyTime)
            {
                if (targetY > _buoyancyTargetY || (Mathf.Approximately(targetY, _buoyancyTargetY) && power > _buoyancyPower))
                {
                    _buoyancyTargetY = targetY;
                    _buoyancyPower = power;
                }
            }
            else
            {
                _buoyancyTargetY = targetY;
                _buoyancyPower = power;
                _buoyancyTime = Time.fixedTime;
            }
        }

        /// <summary>
        /// 현재 CharacterController의 수평 속도 벡터를 반환합니다.
        /// Y축 성분은 제거되어 반환됩니다.
        /// </summary>
        /// <returns>수평 속도 벡터 (Y=0)</returns>
        public Vector3 GetHorizontalVelocity()
        {
            return new Vector3(_controller.velocity.x, 0f, _controller.velocity.z);
        }

        /// <summary>
        /// 이 컴포넌트가 참조하는 CharacterController를 반환합니다.
        /// </summary>
        /// <returns>CharacterController 컴포넌트</returns>
        public CharacterController GetController()
        {
            if (_controller == null) {
                _controller = GetComponent<CharacterController>();
            }
            return _controller;
        }

        // --- 무빙 플랫폼(이동하는 오브젝트) 관련 ---
        private Transform _movingPlatform;
        private Vector3 _lastPlatformPosition;

        /// <summary>
        /// 플레이어가 이동하는 오브젝트 위에 있을 때, 그 오브젝트의 이동량(Delta)을 추적하여 플레이어에게 적용합니다.
        /// </summary>
        /// <param name="groundTransform">현재 밟고 있는 바닥의 Transform</param>
        public void HandleMovingPlatform(Transform groundTransform)
        {
            if (groundTransform != null)
            {
                if (_movingPlatform != groundTransform)
                {
                    // 새로 플랫폼에 올라탔을 때 위치 기억
                    _movingPlatform = groundTransform;
                    _lastPlatformPosition = _movingPlatform.position;
                }
                else
                {
                    // 기존 플랫폼에 계속 타고 있을 때 위치 변화량 계산
                    Vector3 deltaPos = _movingPlatform.position - _lastPlatformPosition;
                    
                    if (deltaPos.sqrMagnitude > 0.00001f)
                    {
                        // 플랫폼이 이동한 만큼 플레이어도 강제 이동시킴 (충돌 무시 방지를 위해 Move 사용)
                        _controller.Move(deltaPos);
                    }
                    
                    // 다음 프레임을 위해 현재 위치 갱신
                    _lastPlatformPosition = _movingPlatform.position;
                }
            }
            else
            {
                // 공중이거나 바닥이 없을 때
                _movingPlatform = null;
            }
        }
    }
}
