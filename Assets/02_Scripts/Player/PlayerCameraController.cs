using UnityEngine;
using Unity.Cinemachine;

public class PlayerCameraController : MonoBehaviour
{
    [Header("Camera Targets")]
    public Transform cameraTarget;
    public CinemachineCamera tpsCamera;
    public CinemachineCamera fpsCamera;

    [Header("Rotation Settings")]
    public float mouseSensitivity = 1.0f;
    public float topClamp = 70.0f;
    public float bottomClamp = -30.0f;

    private PlayerInputHandler _input;
    private float _yaw;
    private float _pitch;

    private void Start()
    {
        _input = GetComponent<PlayerInputHandler>();
        _yaw = transform.rotation.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        RotateCamera();
        UpdateAimMode();
    }

    private void RotateCamera()
    {
        if (_input.LookInput.sqrMagnitude >= 0.01f)
        {
            _yaw += _input.LookInput.x * mouseSensitivity;
            _pitch += _input.LookInput.y * mouseSensitivity;
        }

        _yaw = NormalizeAngle(_yaw);
        _pitch = Mathf.Clamp(_pitch, bottomClamp, topClamp);

        transform.rotation = Quaternion.Euler(0.0f, _yaw, 0.0f);
        cameraTarget.localRotation = Quaternion.Euler(-_pitch, 0.0f, 0.0f);
    }

    private void UpdateAimMode()
    {
        if (fpsCamera == null || tpsCamera == null) return;

        if (_input.IsAiming)
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
        while (angle < -180) angle += 360;
        while (angle > 180) angle -= 360;
        return angle;
    }
}
