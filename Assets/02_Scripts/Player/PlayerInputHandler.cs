using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Action Asset")]
    [SerializeField] private InputActionAsset inputActions;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool IsJumping { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsAiming { get; private set; }

    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _jumpAction;
    private InputAction _sprintAction;
    private InputAction _aimAction;

    private void Awake()
    {
        if (inputActions == null)
        {
            Debug.LogError("InputActionAsset is not assigned to PlayerInputHandler!");
            return;
        }

        var playerMap = inputActions.FindActionMap("Player");
        _moveAction = playerMap.FindAction("Move");
        _lookAction = playerMap.FindAction("Look");
        _jumpAction = playerMap.FindAction("Jump");
        _sprintAction = playerMap.FindAction("Sprint");
        _aimAction = playerMap.FindAction("Aim");
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
        
        // Use WasPressedThisFrame or similar for discrete actions if needed, 
        // but for state checking ReadValue/IsPressed is fine.
        IsJumping = _jumpAction.WasPressedThisFrame();
        IsSprinting = _sprintAction.IsPressed();
        IsAiming = _aimAction.IsPressed();
    }
}
