using UnityEngine;
using UnityEngine.InputSystem;

namespace MushOut.Player
{
    /// <summary>
    /// 플레이어의 입력을 처리하고 다른 컴포넌트가 참조할 수 있도록 상태를 보관하는 클래스입니다.
    /// New Input System의 Action Asset을 사용하여 입력을 캡처합니다.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("입력 액션 설정")]
        [Tooltip("사용할 Input Action Asset입니다.")]
        [SerializeField] private InputActionAsset inputActions;

        /// <summary>
        /// 이동 입력 벡터 (WASD)
        /// </summary>
        public Vector2 MoveInput { get; private set; }

        /// <summary>
        /// 시점 회전 입력 벡터 (Mouse Delta)
        /// </summary>
        public Vector2 LookInput { get; private set; }

        /// <summary>
        /// 점프 입력 여부
        /// </summary>
        public bool IsJumping { get; private set; }

        /// <summary>
        /// 전력 질주 입력 여부
        /// </summary>
        public bool IsSprinting { get; private set; }

        /// <summary>
        /// 조준(FPS 모드 전환) 입력 여부
        /// </summary>
        public bool IsAiming { get; private set; }

        /// <summary>
        /// 사격 입력 여부 (왼쪽 클릭 등)
        /// </summary>
        public bool IsFiring { get; private set; }

        /// <summary>
        /// 상호작용 입력 여부 (F 키 등)
        /// </summary>
        public bool IsInteracting { get; private set; }

        /// <summary>
        /// 상호작용 키 누르고 있는 상태 여부
        /// </summary>
        public bool IsInteractingHeld { get; private set; }

        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private InputAction _aimAction;
        private InputAction _fireAction;
        private InputAction _interactAction;

        /// <summary>
        /// 입력 차단 플래그.
        /// true이면 모든 프로퍼티가 기본값(zero/false)을 반환합니다.
        /// CharacterController나 Collider를 건드리지 않는 안전한 방식입니다.
        /// </summary>
        private bool _isBlocked = false;

        /// <summary>모든 입력을 차단합니다.</summary>
        public void DisableInput() => _isBlocked = true;

        /// <summary>차단된 입력을 다시 활성화합니다.</summary>
        public void EnableInput() => _isBlocked = false;

        private void Awake()
        {
            if (inputActions == null)
            {
                Debug.LogError("[PlayerInputHandler] InputActionAsset이 할당되지 않았습니다!");
                return;
            }

            var playerMap = inputActions.FindActionMap("Player");
            _moveAction = playerMap.FindAction("Move");
            _lookAction = playerMap.FindAction("Look");
            _jumpAction = playerMap.FindAction("Jump");
            _sprintAction = playerMap.FindAction("Sprint");
            _aimAction = playerMap.FindAction("Aim");
            _fireAction = playerMap.FindAction("Fire");
            _interactAction = playerMap.FindAction("Interact");
        }

        private void OnEnable()
        {
            inputActions.Enable();
        }

        private void OnDisable()
        {
            inputActions.Disable();
        }

        private void Update()
        {
            // 시점 회전은 차단 여부와 상관없이 항상 허용 (사용자 요청)
            LookInput = _lookAction.ReadValue<Vector2>();

            if (_isBlocked)
            {
                // 입력 차단 상태: 이동 및 액션 관련 값만 기본값으로 고정
                MoveInput = Vector2.zero;
                IsJumping = false;
                IsSprinting = false;
                IsAiming = false;
                IsFiring = false;
                IsInteracting = false;
                return;
            }

            MoveInput = _moveAction.ReadValue<Vector2>();
            // LookInput은 위에서 이미 처리함
            
            IsJumping = _jumpAction.WasPressedThisFrame();
            IsSprinting = _sprintAction.IsPressed();
            IsAiming = _aimAction != null && _aimAction.IsPressed();
            IsFiring = _fireAction != null && _fireAction.IsPressed();
            IsInteracting = _interactAction != null && _interactAction.WasPressedThisFrame();
            IsInteractingHeld = _interactAction != null && _interactAction.IsPressed();
        }
    }
}
