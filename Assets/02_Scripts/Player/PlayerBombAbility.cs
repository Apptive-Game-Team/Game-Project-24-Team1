using UnityEngine;
using UnityEngine.InputSystem;

namespace MushOut.Player
{
    public enum AimMode
    {
        Hold,   // 누르고 있는 동안 조준
        Toggle  // 한 번 누르면 조준, 다시 누르면 취소
    }

    /// <summary>
    /// 플레이어의 폭탄 투척 능력을 담당하는 컴포넌트입니다.
    /// 조준 궤적 시각화, 투척 거리 조절, 폭탄 생성 및 발사를 처리합니다.
    /// </summary>
    [RequireComponent(typeof(AbilityController))]
    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(LineRenderer))]
    public class PlayerBombAbility : MonoBehaviour
    {
        [Header("Aim Settings")]
        [Tooltip("조준 방식을 선택합니다.")]
        [SerializeField] private AimMode _aimMode = AimMode.Hold;

        [Header("Bomb Settings")]
        [Tooltip("던질 폭탄의 프리팹입니다.")]
        [SerializeField] private GameObject bombPrefab;
        [Tooltip("폭탄이 발사될 위치 기준점입니다.")]
        [SerializeField] private Transform throwPoint;

        [Header("Throw Settings")]
        [Tooltip("기본 투척 거리입니다.")]
        [SerializeField] private float defaultThrowDistance = 5.0f;
        [Tooltip("마우스 휠 1틱당 조절되는 투척 거리입니다.")]
        [SerializeField] private float scrollSensitivity = 1.0f;
        [Tooltip("최소 투척 거리입니다.")]
        [SerializeField] private float minThrowDistance = 2.0f;
        [Tooltip("최대 투척 거리입니다.")]
        [SerializeField] private float maxThrowDistance = 15.0f;
        [Tooltip("투척 각도(도)입니다.")]
        [SerializeField] private float throwAngle = 30.0f;

        [Header("Trajectory Settings")]
        [Tooltip("궤적을 그릴 선의 점 개수(해상도)입니다.")]
        [SerializeField] private int trajectoryResolution = 30;
        [Tooltip("궤적을 그릴 선의 굵기입니다.")]
        [SerializeField] private float trajectoryWidth = 0.05f;

        private AbilityController _abilityController;
        private PlayerInputHandler _input;
        private LineRenderer _lineRenderer;

        private float _currentThrowDistance;
        private bool _wasFiring;
        private bool _wasAimingInput;
        private bool _isAimingToggled;

        private void Awake()
        {
            _abilityController = GetComponent<AbilityController>();
            _input = GetComponent<PlayerInputHandler>();
            _lineRenderer = GetComponent<LineRenderer>();
            
            _currentThrowDistance = defaultThrowDistance;
            
            if (_lineRenderer != null)
            {
                _lineRenderer.positionCount = trajectoryResolution;
                _lineRenderer.startWidth = trajectoryWidth;
                _lineRenderer.endWidth = trajectoryWidth;
                _lineRenderer.enabled = false;
            }
        }

        private void Update()
        {
            if (_abilityController == null || _input == null) return;

            bool isAimingInput = _input.IsAiming; // 우클릭
            bool isFiring = _input.IsFiring; // 좌클릭

            // Toggle 모드 처리
            if (_aimMode == AimMode.Toggle)
            {
                if (isAimingInput && !_wasAimingInput)
                {
                    _isAimingToggled = !_isAimingToggled;
                }
            }
            else
            {
                _isAimingToggled = false; // Hold 모드일 때는 강제 초기화
            }

            bool isAimingActive = _aimMode == AimMode.Hold ? isAimingInput : _isAimingToggled;

            // Bomb 상태가 아니면 토글을 풀고 궤적 끄기
            if (_abilityController.CurrentState != AbilityState.Bomb)
            {
                _isAimingToggled = false;
                isAimingActive = false;
            }

            // 조준 상태가 아니면 궤적 끄기
            if (!isAimingActive)
            {
                if (_lineRenderer != null) _lineRenderer.enabled = false;
                _wasFiring = isFiring;
                _wasAimingInput = isAimingInput;
                return;
            }

            // 폭탄 자원 확인
            if (_abilityController.BombFungus <= 0)
            {
                if (_lineRenderer != null) _lineRenderer.enabled = false;
                _isAimingToggled = false; // 자원이 없으면 토글 해제
                _wasFiring = isFiring;
                _wasAimingInput = isAimingInput;
                return;
            }

            // 마우스 휠 입력을 통한 투척 거리 조절
            if (Mouse.current != null)
            {
                float scrollY = Mouse.current.scroll.ReadValue().y;
                if (scrollY > 0)
                {
                    _currentThrowDistance += scrollSensitivity;
                }
                else if (scrollY < 0)
                {
                    _currentThrowDistance -= scrollSensitivity;
                }
                _currentThrowDistance = Mathf.Clamp(_currentThrowDistance, minThrowDistance, maxThrowDistance);
            }

            // 발사 초기 속도 계산
            Vector3 velocity = CalculateLaunchVelocity();

            // 궤적 그리기
            DrawTrajectory(velocity);

            // 좌클릭 발사 처리
            if (isFiring && !_wasFiring)
            {
                ThrowBomb(velocity);
                _isAimingToggled = false; // 발사 후에는 토글 해제 (자동 조준 취소)
            }

            _wasFiring = isFiring;
            _wasAimingInput = isAimingInput;
        }

        /// <summary>
        /// 현재 투척 거리와 각도를 기반으로 폭탄의 초기 발사 속도 벡터를 계산합니다.
        /// </summary>
        /// <returns>계산된 발사 속도 벡터(월드 공간)입니다.</returns>
        private Vector3 CalculateLaunchVelocity()
        {
            // 수평 방향: 카메라의 정면(수평)을 기준으로 함. (또는 플레이어의 정면)
            Vector3 forward = transform.forward;
            if (Camera.main != null)
            {
                forward = Camera.main.transform.forward;
                forward.y = 0;
                forward.Normalize();
            }

            // 공식: V = sqrt( d * g / sin(2*theta) )
            float g = Mathf.Abs(Physics.gravity.y);
            float theta = throwAngle * Mathf.Deg2Rad;
            float vSqr = (_currentThrowDistance * g) / Mathf.Sin(2 * theta);
            float v = Mathf.Sqrt(vSqr);

            Vector3 velocity = forward * (v * Mathf.Cos(theta)) + Vector3.up * (v * Mathf.Sin(theta));
            return velocity;
        }

        /// <summary>
        /// 주어진 초기 속도로 폭탄의 포물선 궤적을 LineRenderer로 시각화합니다.
        /// </summary>
        /// <param name="velocity">폭탄의 초기 발사 속도 벡터입니다.</param>
        private void DrawTrajectory(Vector3 velocity)
        {
            if (_lineRenderer == null || throwPoint == null) return;

            _lineRenderer.enabled = true;
            Vector3 startPos = throwPoint.position;
            Vector3 currentPos = startPos;
            Vector3 currentVel = velocity;
            float timeStep = 0.1f;

            for (int i = 0; i < trajectoryResolution; i++)
            {
                _lineRenderer.SetPosition(i, currentPos);
                
                // 등가속도 운동 공식 적용
                currentPos += currentVel * timeStep + 0.5f * Physics.gravity * timeStep * timeStep;
                currentVel += Physics.gravity * timeStep;

                // 지면에 닿았다면 그 뒤의 점들은 지면 위치로 맞추거나 그리지 않기
                if (Physics.Raycast(currentPos, Vector3.down, out RaycastHit hit, 0.5f))
                {
                    for (int j = i + 1; j < trajectoryResolution; j++)
                    {
                        _lineRenderer.SetPosition(j, hit.point);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// 폭탄을 생성하고 지정된 속도로 발사합니다.
        /// 폭탄 자원을 소모하고, 플레이어와의 물리 충돌을 무시하도록 설정합니다.
        /// </summary>
        /// <param name="velocity">폭탄에 적용할 초기 발사 속도 벡터입니다.</param>
        private void ThrowBomb(Vector3 velocity)
        {
            if (bombPrefab == null || throwPoint == null)
            {
                Debug.LogWarning("[PlayerBombAbility] 폭탄 프리팹이나 ThrowPoint가 설정되지 않았습니다!");
                return;
            }

            _abilityController.UseBomb(); // 폭탄 자원 1 소모

            GameObject bombObj = Instantiate(bombPrefab, throwPoint.position, Quaternion.identity);

            // 플레이어와 폭탄 간의 물리 충돌 무시 (밀림/점프 현상 방지)
            Collider[] playerColliders = GetComponentsInChildren<Collider>();
            Collider bombCollider = bombObj.GetComponent<Collider>();
            if (bombCollider != null)
            {
                foreach (Collider pc in playerColliders)
                {
                    Physics.IgnoreCollision(pc, bombCollider);
                }
            }
            
            // 폭탄에 초기 속도 전달 (BombProjectile 컴포넌트가 존재한다고 가정)
            MushOut.Combat.BombProjectile bombProj = bombObj.GetComponent<MushOut.Combat.BombProjectile>();
            if (bombProj != null)
            {
                bombProj.Initialize(velocity);
            }
            else
            {
                // Rigidbody가 있다면 직접 힘 가하기
                Rigidbody rb = bombObj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = velocity;
                }
            }
        }
    }
}
