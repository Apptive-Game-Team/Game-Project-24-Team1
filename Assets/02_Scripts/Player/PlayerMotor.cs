using UnityEngine;

namespace MushOut.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("이동 설정")]
        [Tooltip("기본 이동 속도입니다.")]
        public float moveSpeed = 5.0f;

        [Tooltip("전력 질주 시 속도입니다.")]
        public float sprintSpeed = 8.0f;

        [Tooltip("속도 변화의 가속도 계수입니다.")]
        public float speedChangeRate = 10.0f;

        [Header("밀고 당기기 설정")]
        [Tooltip("밀고 당길 때의 이동 속도입니다.")]
        public float pushPullSpeed = 2.0f;

        [Header("점프 및 중력")]
        [Tooltip("점프 높이입니다.")]
        public float jumpHeight = 1.2f;

        [Tooltip("적용될 중력 값입니다.")]
        public float gravity = -15.0f;

        [Header("물(Water) 설정")]
        [Tooltip("물 속에서의 이동 속도입니다.")]
        public float waterMoveSpeed = 2.0f;

        [Tooltip("물 속에서 받는 수직 저항력(Drag)입니다.")]
        public float waterDrag = 3.0f;

        public float VerticalVelocity { get; set; }
        public Vector3 ExternalVelocity { get; set; }

        private CharacterController _controller;
        private float _currentSpeed;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        public void ApplyGravity(float deltaTime, bool isInWater)
        {
            if (isInWater)
            {
                VerticalVelocity = Mathf.Lerp(VerticalVelocity, 0f, waterDrag * deltaTime);
            }

            if (VerticalVelocity > -53f)
            {
                VerticalVelocity += gravity * deltaTime;
            }
        }

        public void ApplyMovement(float deltaTime, Vector2 moveInput, bool isSprinting, bool isInWater, bool isGrounded, MushOut.Interactables.PushPullInteractable grabbedObject, Vector3 hitNormal, LayerMask groundLayers)
        {
            float targetSpeed = isSprinting ? sprintSpeed : moveSpeed;
            if (grabbedObject != null) targetSpeed = pushPullSpeed;
            if (isInWater) targetSpeed = waterMoveSpeed;

            if (grabbedObject != null && grabbedObject.movementType == MushOut.Interactables.PushPullMovementType.ForwardBackwardOnly)
            {
                if (Mathf.Abs(moveInput.y) < 0.01f) targetSpeed = 0f;
            }
            else
            {
                if (moveInput == Vector2.zero) targetSpeed = 0f;
            }

            _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, deltaTime * speedChangeRate);

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

                if (grabbedObject != null)
                {
                    Vector3 toObject = grabbedObject.transform.position - transform.position;
                    toObject.y = 0;
                    if (!grabbedObject.CanMoveInDirection(moveDir, toObject))
                    {
                        moveDir = Vector3.zero;
                    }
                }
            }

            Vector3 movement = moveDir * _currentSpeed + Vector3.up * VerticalVelocity;

            if (ExternalVelocity.sqrMagnitude > 0.001f)
            {
                float drag = isInWater ? waterDrag : (isGrounded ? speedChangeRate : 2.0f);
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

        public void ExecuteJump()
        {
            VerticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        public void AddBuoyancy(float force)
        {
            VerticalVelocity += force * Time.fixedDeltaTime;
        }

        public void AddExternalForce(Vector3 force)
        {
            ExternalVelocity += force * Time.fixedDeltaTime;
        }

        public Vector3 GetHorizontalVelocity()
        {
            return new Vector3(_controller.velocity.x, 0f, _controller.velocity.z);
        }

        public CharacterController GetController()
        {
            return _controller;
        }
    }
}
