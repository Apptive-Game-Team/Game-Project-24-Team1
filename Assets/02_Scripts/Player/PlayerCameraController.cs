using UnityEngine;
using Unity.Cinemachine;

namespace Nexush.Player
{
    /// <summary>
    /// 플레이어의 카메라 제어 및 TPS/FPS 모드 전환을 담당하는 클래스입니다.
    /// 마우스 입력을 받아 플레이어 몸체와 카메라 타겟을 회전시킵니다.
    /// </summary>
    public class PlayerCameraController : MonoBehaviour
    {
        [Header("카메라 참조")]
        [Tooltip("상하 회전(Pitch)이 적용될 타겟 Transform입니다.")]
        [SerializeField] private Transform cameraTarget;
        
        [Tooltip("3인칭(TPS) 카메라 컴포넌트입니다.")]
        [SerializeField] private CinemachineCamera tpsCamera;
        
        [Tooltip("1인칭(FPS) 카메라 컴포넌트입니다.")]
        [SerializeField] private CinemachineCamera fpsCamera;

        [Header("캐릭터 모델 설정")]
        [Tooltip("1인칭 시 숨길 캐릭터 모델의 최상위 오브젝트입니다.")]
        [SerializeField] private GameObject playerMeshRoot;

        [Header("감도 및 부드러움 설정")]
        [Tooltip("마우스 감도입니다.")]
        [SerializeField] private float mouseSensitivity = 1.0f;
        
        [Tooltip("시점 회전의 부드러움 정도입니다. (0에 가까울수록 즉각적)")]
        [Range(0f, 0.2f)]
        [SerializeField] private float lookSmoothTime = 0.02f;

        [Header("회전 각도 제한")]
        [Tooltip("상단 회전 제한 각도입니다.")]
        [SerializeField] private float topClamp = 70.0f;
        
        [Tooltip("하단 회전 제한 각도입니다.")]
        [SerializeField] private float bottomClamp = -30.0f;

        [Header("카메라 충돌 설정")]
        [Tooltip("TPS 카메라가 충돌할 레이어 마스크입니다. (벽, 지형 등을 포함하세요)")]
        [SerializeField] private LayerMask cameraCollisionLayers = ~0;

        [Tooltip("카메라가 Follow 타겟(플레이어)에 최소한으로 유지할 거리입니다.")]
        [SerializeField] private float cameraMinDistanceFromTarget = 0.5f;

        [Tooltip("카메라가 통과할 수 있는 투명 레이어입니다. (유리, 트리거 등)")]
        [SerializeField] private LayerMask cameraTransparentLayers = 0;

        [Tooltip("카메라 충돌 감지용 구체(SphereCast) 반지름입니다. \n얇은 벽이 뚫릴 경우 이 값을 줄여보세요. (기본: 0.1)")]
        [SerializeField] private float cameraRadius = 0.1f;

        private PlayerInputHandler _input;
        
        // 회전 상태 변수
        private float _targetYaw;
        private float _targetPitch;
        private float _currentYaw;
        private float _currentPitch;
        private float _yawVelocity;
        private float _pitchVelocity;

        private bool _isFPS;

        private void Start()
        {
            _input = GetComponent<PlayerInputHandler>();

            _targetYaw = transform.eulerAngles.y;
            _currentYaw = _targetYaw;

            // 커서 잠금 및 숨김
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // 카메라 전환 시 즉시 반영되도록 설정
            var brain = Camera.main?.GetComponent<CinemachineBrain>();
            if (brain != null)
            {
                brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            }

            // TPS 카메라에 벽 관통 방지 Extension 설정
            SetupCameraDeoccluder();

            SetFPSMode(false);
        }

        /// <summary>
        /// 시점 회전은 시네머신 및 입력 시스템과의 동기화를 위해 LateUpdate에서 처리합니다.
        /// </summary>
        private void LateUpdate()
        {
            HandleMouseLook();
            UpdateCameraMode();
        }

        /// <summary>
        /// 마우스 입력을 기반으로 플레이어의 좌우 회전과 카메라 타겟의 상하 회전을 계산합니다.
        /// </summary>
        private void HandleMouseLook()
        {
            if (_input.LookInput.sqrMagnitude >= 0.01f)
            {
                _targetYaw += _input.LookInput.x * mouseSensitivity;
                _targetPitch -= _input.LookInput.y * mouseSensitivity;
            }

            // 각도 정규화 및 제한
            _targetYaw = NormalizeAngle(_targetYaw);
            _targetPitch = Mathf.Clamp(_targetPitch, bottomClamp, topClamp);

            // 스무딩 처리
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

            // 1. 회전 적용
            if (_isFPS)
            {
                // FPS 모드(또는 조준 중): 캐릭터 몸체(Yaw)와 카메라 타겟(Pitch)을 함께 제어
                transform.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
                if (cameraTarget != null)
                {
                    cameraTarget.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
                }
            }
            else
            {
                // TPS 모드: 캐릭터 몸체는 가만히 두고, 카메라 타겟(피벗)만 캐릭터 주위를 돌게 함
                if (cameraTarget != null)
                {
                    // 카메라 타겟에 상하(Pitch)와 좌우(Yaw) 회전을 모두 적용
                    cameraTarget.rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
                }
            }
        }

        /// <summary>
        /// TPS 카메라에 CinemachineDeoccluder Extension을 추가하고 설정합니다.
        /// 플레이어와 카메라 사이에 장애물이 있을 경우 카메라를 자동으로 앞으로 당깁니다.
        /// </summary>
        private void SetupCameraDeoccluder()
        {
            if (tpsCamera == null) return;

            // 기존 Deoccluder가 없으면 추가
            var deoccluder = tpsCamera.GetComponent<CinemachineDeoccluder>();
            if (deoccluder == null)
            {
                deoccluder = tpsCamera.gameObject.AddComponent<CinemachineDeoccluder>();
            }

            // 충돌 레이어: 카메라가 뚫으면 안 될 레이어 (기본값: 모든 레이어)
            deoccluder.CollideAgainst = cameraCollisionLayers;

            // 투명 레이어: 카메라가 통과할 수 있는 레이어 (유리, 트리거 등)
            deoccluder.TransparentLayers = cameraTransparentLayers;

            // 플레이어와 카메라 사이의 최소 거리 유지
            deoccluder.MinimumDistanceFromTarget = cameraMinDistanceFromTarget;

            // 충돌 감지 구체 반지름 설정
            // 이 값이 벽의 두께보다 크면 얇은 벽을 그냥 통과해버릴 수 있음
            // 예: localScale.z = 0.1인 벽을 감지하려면 CameraRadius도 0.1 이하여야 함
            var avoidance = deoccluder.AvoidObstacles;
            avoidance.Enabled = true;
            avoidance.CameraRadius = cameraRadius;
            deoccluder.AvoidObstacles = avoidance;

            Debug.Log($"[PlayerCameraController] TPS 카메라 충돌 방지(Deoccluder) 설정 완료. CameraRadius={cameraRadius}");
        }

        /// <summary>
        /// 입력 상태에 따라 카메라 모드(TPS/FPS)를 전환합니다.
        /// </summary>
        private void UpdateCameraMode()
        {
            if (fpsCamera == null || tpsCamera == null)
            {
                return;
            }

            bool shouldBeFPS = _input.IsAiming;
            if (shouldBeFPS == _isFPS)
            {
                return;
            }

            _isFPS = shouldBeFPS;
            SetFPSMode(_isFPS);
        }

        /// <summary>
        /// 특정 카메라의 우선순위를 높여 시점 모드를 변경합니다.
        /// </summary>
        /// <param name="isFPSMode">1인칭 모드 여부</param>
        private void SetFPSMode(bool isFPSMode)
        {
            if (isFPSMode)
            {
                fpsCamera.Priority = 20;
                tpsCamera.Priority = 10;
            }
            else
            {
                fpsCamera.Priority = 10;
                tpsCamera.Priority = 20;
            }

            // 1인칭 모드일 때 캐릭터 메쉬 숨기기
            if (playerMeshRoot != null)
            {
                playerMeshRoot.SetActive(!isFPSMode);
            }
        }

        /// <summary>
        /// 각도를 -180 ~ 180도 범위로 정규화합니다.
        /// </summary>
        private float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            if (angle < -180f) angle += 360f;
            return angle;
        }
    }
}
