using UnityEngine;

namespace MushOut.Enemy
{
    /// <summary>
    /// 에너미의 시야를 담당하는 클래스입니다.
    /// 플레이어를 Raycast로 탐지하고 EnemyStatus의 상태를 Chasing으로 변경합니다.
    /// 또한 GL을 사용하여 씬 뷰와 게임 뷰에서 탐지 범위 원뿔을 시각화합니다.
    /// </summary>
    [ExecuteAlways]
    public class EnemySight : MonoBehaviour
    {
        [Header("Gizmo Settings")]
        [Tooltip("원뿔 채우기 투명도입니다. (0: 완전 투명 ~ 1: 완전 불투명)")]
        [Range(0f, 1f)]
        [SerializeField] private float _alpha = 0.15f;

        [Tooltip("원뿔 밑면 원을 분할하는 세그먼트 수입니다. 높을수록 부드럽지만 성능을 소모합니다.")]
        [Range(8, 64)]
        [SerializeField] private int _segments = 32;

        [Tooltip("시야(Raycast 및 기즈모)가 시작되는 위치의 로컬 오프셋입니다.")]
        [SerializeField] private Vector3 _eyeOffset = new Vector3(0f, 1.0f, 0f);

        /// <summary> 플레이어의 Transform을 캐싱합니다. </summary>
        private Transform _playerTransform;
        
        /// <summary> 상태를 제어하는 EnemyController 컴포넌트입니다. </summary>
        private EnemyController _enemyController;

#if UNITY_EDITOR
        /// <summary> GL 렌더링에 사용할 반투명 머티리얼입니다. </summary>
        private Material _coneMaterial;

        // 원뿔 기하 정점 캐싱용 변수들
        private Vector3[] _localCirclePoints;
        private float _cachedDetectionDist = -1f;
        private float _cachedFov = -1f;
        private int _cachedSegments = -1;
#endif

        // --- 공유 속성 (코드 중복 방지) ---
        private float CurrentDetectionDist => _enemyController.CurrentState == EnemyController.State.Chasing ? _enemyController.SightDistance + 7.0f : _enemyController.SightDistance;
        private float CurrentFov => _enemyController.CurrentState == EnemyController.State.Chasing ? 150.0f : _enemyController.FieldOfView;

        private void Awake()
        {
            _enemyController = GetComponentInParent<EnemyController>();
#if UNITY_EDITOR
            CreateMaterial();
#endif
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (_coneMaterial == null)
            {
                CreateMaterial();
            }
#endif
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                if (_enemyController == null) _enemyController = GetComponentInParent<EnemyController>();

                // 1. 최적화: 매번 씬 전체를 뒤지는 Find() 대신 GameManager의 전역 캐싱 활용
                if (MushOut.Core.GameManager.Instance != null)
                {
                    _playerTransform = MushOut.Core.GameManager.Instance.PlayerTransform;
                }

                // 싱글톤에 등록되지 않은 특수 상황 대비 (Editor 테스트 등)
                if (_playerTransform == null)
                {
                    GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                    if (playerObj == null) playerObj = GameObject.Find("Player");
                    if (playerObj != null) _playerTransform = playerObj.transform;
                }

                // 2. 최적화: 매 프레임 검사 대신 코루틴을 통해 주기적(0.15초)으로 검사
                StartCoroutine(SightRoutine());
            }
        }

        private System.Collections.IEnumerator SightRoutine()
        {
            // 0.15초 간격으로 시야 판정 (성능 부하 감소)
            WaitForSeconds waitTime = new WaitForSeconds(0.1f);

            while (true)
            {
                yield return waitTime;

                if (_playerTransform == null || _enemyController == null)
                {
                    continue;
                }

                // 사망하거나 기절한 상태면 탐지 로직 생략
                if (_enemyController.CurrentState == EnemyController.State.Dead || 
                    _enemyController.CurrentState == EnemyController.State.Stunned)
                {
                    _enemyController.IsPlayerSpotted = false;
                    continue;
                }

                bool canSee = false;

                // 시야 기준점 설정
                Vector3 origin = transform.position + transform.TransformDirection(_eyeOffset);
                
                // 플레이어의 발끝(0.2m)부터 머리(1.8m)까지 10등분하여 촘촘하게 전부 검사
                int checkResolution = 10;
                
                for (int i = 0; i <= checkResolution; i++)
                {
                    float height = Mathf.Lerp(1.8f, 0.2f, (float)i / checkResolution);
                    Vector3 offset = Vector3.up * height;
                    Vector3 targetPos = _playerTransform.position + offset;
                    Vector3 dirToTarget = targetPos - origin;
                    float distToTarget = dirToTarget.magnitude;

                    // 1. 판정 로직
                    if (distToTarget <= CurrentDetectionDist && Vector3.Angle(transform.forward, dirToTarget) <= CurrentFov * 0.5f)
                    {
                        // 시야각 내에 있으면 Raycast로 장애물 확인
                        Vector3 rayDir = dirToTarget.normalized;

                        if (Physics.Raycast(origin, rayDir, out RaycastHit hit, distToTarget, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                        {
                            if (hit.transform.CompareTag("Player"))
                            {
                                canSee = true;
                                break; // 신체 중 하나라도 보이면 즉시 탐지 완료
                            }
                        }
                    }
                }

                // 3. 발각 시 공통 처리 로직
                if (canSee)
                {
                    // 시야에 보일 때만 갱신 (여러 개의 Sight 컴포넌트 간의 덮어씌우기 방지)
                    _enemyController.IsPlayerSpotted = true;
                    
                    // 플레이어의 현재 위치로 LPP 지속 갱신
                    _enemyController.LatestPlayerPosition = _playerTransform.position;

                    // Chasing 상태가 아니라면 Chasing으로 전환
                    if (_enemyController.CurrentState != EnemyController.State.Chasing)
                    {
                        _enemyController.ChangeState(EnemyController.State.Chasing);
                    }
                }
            }
        }

#if UNITY_EDITOR
        private void CreateMaterial()
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null) return;

            _coneMaterial = new Material(shader);
            _coneMaterial.hideFlags = HideFlags.HideAndDontSave;
            _coneMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _coneMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _coneMaterial.SetInt("_Cull",     (int)UnityEngine.Rendering.CullMode.Off);
            _coneMaterial.SetInt("_ZWrite",   0);
        }

        private void UpdateCachedPoints()
        {
            if (_localCirclePoints == null || _cachedSegments != _segments || 
                _cachedDetectionDist != CurrentDetectionDist || _cachedFov != CurrentFov)
            {
                _cachedSegments = _segments;
                _cachedDetectionDist = CurrentDetectionDist;
                _cachedFov = CurrentFov;

                _localCirclePoints = new Vector3[_segments];

                float halfFovRad = _cachedFov * 0.5f * Mathf.Deg2Rad;
                float baseRadius = _cachedDetectionDist * Mathf.Tan(halfFovRad);

                // 로컬 좌표계 기준의 꼭짓점과 밑면 중심
                Vector3 localBaseCenter = _eyeOffset + Vector3.forward * _cachedDetectionDist;

                for (int i = 0; i < _segments; i++)
                {
                    float angle = 2f * Mathf.PI / _segments * i;
                    _localCirclePoints[i] = localBaseCenter
                        + Vector3.right * Mathf.Cos(angle) * baseRadius
                        + Vector3.up    * Mathf.Sin(angle) * baseRadius;
                }
            }
        }

        private void OnRenderObject()
        {
            if (_enemyController == null || _coneMaterial == null) return;

            // 사망하거나 기절한 상태면 탐지 범위 기즈모를 숨김
            if (_enemyController.CurrentState == EnemyController.State.Dead || 
                _enemyController.CurrentState == EnemyController.State.Stunned)
            {
                return;
            }

            bool isSpotted  = _enemyController.IsPlayerSpotted;

            // 탐지 여부에 따라 색상 결정
            Color fillColor = isSpotted
                ? new Color(1f, 0f, 0f, _alpha)
                : new Color(1f, 1f, 0f, _alpha);

            Color lineColor = new Color(fillColor.r, fillColor.g, fillColor.b, Mathf.Min(_alpha * 5f, 1f));

            // 정점 정보가 변경되었을 때만 캐시 업데이트 (메모리 재할당 방지 및 CPU 최적화)
            UpdateCachedPoints();

            _coneMaterial.SetPass(0);
            GL.PushMatrix();
            // 로컬 좌표계 매트릭스 적용
            GL.MultMatrix(transform.localToWorldMatrix);

            Vector3 localApex = _eyeOffset;
            Vector3 localBaseCenter = _eyeOffset + Vector3.forward * CurrentDetectionDist;

            // ── 채우기: 옆면 + 밑면 삼각형 팬 ───────────────────────────────
            GL.Begin(GL.TRIANGLES);
            GL.Color(fillColor);

            for (int i = 0; i < _segments; i++)
            {
                Vector3 p1 = _localCirclePoints[i];
                Vector3 p2 = _localCirclePoints[(i + 1) % _segments];

                // 옆면: 꼭짓점 → 밑면 두 점
                GL.Vertex(localApex);
                GL.Vertex(p1);
                GL.Vertex(p2);

                // 밑면: 원 중심 → 밑면 두 점
                GL.Vertex(localBaseCenter);
                GL.Vertex(p2);
                GL.Vertex(p1);
            }

            GL.End();

            // ── 윤곽선: 밑면 원 테두리만 ─────────────────────────────────────
            GL.Begin(GL.LINES);
            GL.Color(lineColor);

            for (int i = 0; i < _segments; i++)
            {
                GL.Vertex(_localCirclePoints[i]);
                GL.Vertex(_localCirclePoints[(i + 1) % _segments]);
            }

            GL.End();
            GL.PopMatrix();
        }

        private void OnDrawGizmosSelected()
        {
            if (_enemyController == null)
            {
                _enemyController = GetComponentInParent<EnemyController>();
            }
            if (_enemyController == null) return;

            Vector3 origin = transform.position + transform.TransformDirection(_eyeOffset);

            // 시야각 경계선 (수평 좌/우)
            Vector3 leftBoundary = Quaternion.AngleAxis(-CurrentFov * 0.5f, transform.up) * transform.forward * CurrentDetectionDist;
            Vector3 rightBoundary = Quaternion.AngleAxis(CurrentFov * 0.5f, transform.up) * transform.forward * CurrentDetectionDist;
            
            // 시야각 경계선 (수직 상/하)
            Vector3 topBoundary = Quaternion.AngleAxis(-CurrentFov * 0.5f, transform.right) * transform.forward * CurrentDetectionDist;
            Vector3 bottomBoundary = Quaternion.AngleAxis(CurrentFov * 0.5f, transform.right) * transform.forward * CurrentDetectionDist;
            
            Gizmos.color = Color.cyan;
            
            // 중심에서 각 모서리로 뻗어나가는 선
            Gizmos.DrawLine(origin, origin + leftBoundary);
            Gizmos.DrawLine(origin, origin + rightBoundary);
            Gizmos.DrawLine(origin, origin + topBoundary);
            Gizmos.DrawLine(origin, origin + bottomBoundary);
            
            // 시야 끝부분을 이어주는 다이아몬드 형태 테두리
            Gizmos.DrawLine(origin + leftBoundary, origin + topBoundary);
            Gizmos.DrawLine(origin + topBoundary, origin + rightBoundary);
            Gizmos.DrawLine(origin + rightBoundary, origin + bottomBoundary);
            Gizmos.DrawLine(origin + bottomBoundary, origin + leftBoundary);

            // 플레이어 탐지 시각화
            if (Application.isPlaying && _playerTransform != null)
            {
                int checkResolution = 30;
                Gizmos.color = Color.yellow;
                
                for (int i = 0; i <= checkResolution; i++)
                {
                    float height = Mathf.Lerp(1.8f, 0.2f, (float)i / checkResolution);
                    Vector3 offset = Vector3.up * height;
                    
                    Vector3 targetPos = _playerTransform.position + offset;
                    Vector3 dirToTarget = targetPos - origin;

                    if (dirToTarget.magnitude <= CurrentDetectionDist && Vector3.Angle(transform.forward, dirToTarget) <= CurrentFov * 0.5f)
                    {
                        Gizmos.DrawLine(origin, targetPos);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (_coneMaterial != null)
            {
                DestroyImmediate(_coneMaterial);
            }
        }
#endif
    }
}
