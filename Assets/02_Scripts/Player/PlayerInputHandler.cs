using UnityEngine;
using UnityEngine.InputSystem;

namespace Nexush.Player
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
        /// 상호작용 입력 여부 (E 키 등)
        /// </summary>
        public bool IsInteracting { get; private set; }

        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private InputAction _aimAction;
        private InputAction _fireAction;
        private InputAction _interactAction;

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
            MoveInput = _moveAction.ReadValue<Vector2>();
            LookInput = _lookAction.ReadValue<Vector2>();

            IsJumping = _jumpAction.WasPressedThisFrame();
            IsSprinting = _sprintAction.IsPressed();
            IsAiming = _aimAction != null && _aimAction.IsPressed();
            IsFiring = _fireAction != null && _fireAction.IsPressed();
            IsInteracting = _interactAction != null && _interactAction.WasPressedThisFrame();
        }
    }
}
