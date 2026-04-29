using UnityEngine;
using Unity.Cinemachine;

public class PlayerCameraController : MonoBehaviour
{
    [Header("카메라 참조")]
    [Tooltip("수직 회전(Pitch)을 담당하는 카메라 타겟 Transform")]
    public Transform cameraTarget;
    public CinemachineCamera tpsCamera;
    public CinemachineCamera fpsCamera;

    [Header("마우스 감도 및 스무딩")]
    public float mouseSensitivity = 1.0f;
    [Tooltip("마우스 회전을 부드럽게 만듭니다 (0이면 즉각 반응, 0.05 정도면 부드러움)")]
    [Range(0f, 0.2f)]
    public float lookSmoothTime = 0.02f;

    [Header("수직 각도 제한")]
    public float topClamp = 70.0f;
    public float bottomClamp = -30.0f;

    private PlayerInputHandler _input;
    
    // 목표 회전값
    private float _targetYaw;
    private float _targetPitch;

    // 실제 적용되는 현재 회전값 (스무딩 용도)
    private float _currentYaw;
    private float _currentPitch;

    // 스무딩 속도 참조 변수
    private float _yawVelocity;
    private float _pitchVelocity;

    private bool _isFPS;

    private void Start()
    {
        _input = GetComponent<PlayerInputHandler>();

        _targetYaw = transform.eulerAngles.y;
        _currentYaw = _targetYaw;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 즉시 전환되도록 블렌드 시간 0으로 설정
        var brain = Camera.main?.GetComponent<CinemachineBrain>();
        if (brain != null)
        {
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
        }

        SetFPSMode(false);
    }

    // 💡 마우스 시점 처리는 반드시 LateUpdate에서 수행해야 시네머신 및 Input 타이밍과 완벽히 동기화됩니다.
    private void LateUpdate()
    {
        HandleMouseLook();
        UpdateCameraMode();
    }

    private void HandleMouseLook()
    {
        // 1. 마우스 입력 누적
        if (_input.LookInput.sqrMagnitude >= 0.01f)
        {
            // Input System의 마우스 Delta는 이미 프레임 독립적이므로 Time.deltaTime을 곱하지 않습니다.
            _targetYaw += _input.LookInput.x * mouseSensitivity;
            _targetPitch -= _input.LookInput.y * mouseSensitivity;
        }

        // 2. 각도 클램프
        _targetYaw = NormalizeAngle(_targetYaw);
        _targetPitch = Mathf.Clamp(_targetPitch, bottomClamp, topClamp);

        // 3. 부드러운 회전 보간 (스무딩 처리)
        if (lookSmoothTime > 0f)
        {
            _currentYaw = Mathf.SmoothDampAngle(_currentYaw, _targetYaw, ref _yawVelocity, lookSmoothTime);
            _currentPitch = Mathf.SmoothDampAngle(_currentPitch, _targetPitch, ref _pitchVelocity, lookSmoothTime);
        }
        else
        {
            _currentYaw = _targetYaw;
            _currentPitch = _targetPitch;
        }

        // 4. 회전 적용
        // 플레이어 몸체 전체가 좌우로 회전합니다.
        transform.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
        // 카메라 타겟만 상하로 고개를 숙이거나 듭니다.
        cameraTarget.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
    }

    private void UpdateCameraMode()
    {
        if (fpsCamera == null || tpsCamera == null) return;

        bool shouldBeFPS = _input.IsAiming;
        if (shouldBeFPS == _isFPS) return;

        _isFPS = shouldBeFPS;
        SetFPSMode(_isFPS);
    }

    private void SetFPSMode(bool fps)
    {
        if (fps)
        {
            fpsCamera.Priority = 20;
            tpsCamera.Priority = 10;
        }
        else
        {
            fpsCamera.Priority = 10;
            tpsCamera.Priority = 20;
        }
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        if (angle < -180f) angle += 360f;
        return angle;
    }
}
