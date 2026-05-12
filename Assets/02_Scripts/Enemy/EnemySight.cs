using UnityEngine;

namespace GameProject24.Enemy
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
        
        /// <summary> 상태를 제어하는 EnemyStatus 컴포넌트입니다. </summary>
        private EnemyStatus _enemyStatus;

        /// <summary> GL 렌더링에 사용할 반투명 머티리얼입니다. </summary>
        private Material _coneMaterial;

        // --- 공유 속성 (코드 중복 방지) ---
        private float CurrentDetectionDist => _enemyStatus.CurrentState == EnemyStatus.State.Chasing ? _enemyStatus.SightDistance + 7.0f : _enemyStatus.SightDistance;
        private float CurrentFov => _enemyStatus.CurrentState == EnemyStatus.State.Chasing ? 150.0f : _enemyStatus.FieldOfView;

        private void Awake()
        {
            _enemyStatus = GetComponentInParent<EnemyStatus>();
            CreateMaterial();
        }

        private void OnEnable()
        {
            if (_coneMaterial == null)
            {
                CreateMaterial();
            }
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                if (_enemyStatus == null) _enemyStatus = GetComponentInParent<EnemyStatus>();

                // 플레이어 캐싱 (태그가 없으면 이름으로 백업 검색)
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj == null)
                {
                    playerObj = GameObject.Find("Player");
                }

                if (playerObj != null)
                {
                    _playerTransform = playerObj.transform;
                }
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            if (_playerTransform == null || _enemyStatus == null)
            {
                return;
            }

            // 사망하거나 기절한 상태면 탐지 로직 생략
            if (_enemyStatus.CurrentState == EnemyStatus.State.Dead || 
                _enemyStatus.CurrentState == EnemyStatus.State.Stunned)
            {
                _enemyStatus.IsPlayerSpotted = false;
                return;
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
                _enemyStatus.IsPlayerSpotted = true;
                
                // 플레이어의 현재 위치로 LPP 지속 갱신
                _enemyStatus.LatestPlayerPosition = _playerTransform.position;

                // Chasing 상태가 아니라면 Chasing으로 전환
                if (_enemyStatus.CurrentState != EnemyStatus.State.Chasing)
                {
                    _enemyStatus.ChangeState(EnemyStatus.State.Chasing);
                }
            }
        }

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

        private void OnRenderObject()
        {
            if (_enemyStatus == null || _coneMaterial == null) return;

            // 사망하거나 기절한 상태면 탐지 범위 기즈모를 숨김
            if (_enemyStatus.CurrentState == EnemyStatus.State.Dead || 
                _enemyStatus.CurrentState == EnemyStatus.State.Stunned)
            {
                return;
            }

            bool isSpotted  = _enemyStatus.IsPlayerSpotted;

            // 탐지 여부에 따라 색상 결정
            Color fillColor = isSpotted
                ? new Color(1f, 0f, 0f, _alpha)
                : new Color(1f, 1f, 0f, _alpha);

            Color lineColor = new Color(fillColor.r, fillColor.g, fillColor.b, Mathf.Min(_alpha * 5f, 1f));

            // 원뿔 기하 계산
            float halfFovRad = CurrentFov * 0.5f * Mathf.Deg2Rad;
            float baseRadius = CurrentDetectionDist * Mathf.Tan(halfFovRad);

            Vector3 apex       = transform.position + transform.TransformDirection(_eyeOffset);
            Vector3 baseCenter = apex + transform.forward * CurrentDetectionDist;
            Vector3 axisRight  = transform.right;
            Vector3 axisUp     = transform.up;

            // 밑면 원 위의 점들 미리 계산
            Vector3[] circlePoints = new Vector3[_segments];
            for (int i = 0; i < _segments; i++)
            {
                float angle = 2f * Mathf.PI / _segments * i;
                circlePoints[i] = baseCenter
                    + axisRight * Mathf.Cos(angle) * baseRadius
                    + axisUp    * Mathf.Sin(angle) * baseRadius;
            }

            _coneMaterial.SetPass(0);
            GL.PushMatrix();

            // ── 채우기: 옆면 + 밑면 삼각형 팬 ───────────────────────────────
            GL.Begin(GL.TRIANGLES);
            GL.Color(fillColor);

            for (int i = 0; i < _segments; i++)
            {
                Vector3 p1 = circlePoints[i];
                Vector3 p2 = circlePoints[(i + 1) % _segments];

                // 옆면: 꼭짓점 → 밑면 두 점
                GL.Vertex(apex);
                GL.Vertex(p1);
                GL.Vertex(p2);

                // 밑면: 원 중심 → 밑면 두 점
                GL.Vertex(baseCenter);
                GL.Vertex(p2);
                GL.Vertex(p1);
            }

            GL.End();

            // ── 윤곽선: 밑면 원 테두리만 ─────────────────────────────────────
            GL.Begin(GL.LINES);
            GL.Color(lineColor);

            for (int i = 0; i < _segments; i++)
            {
                GL.Vertex(circlePoints[i]);
                GL.Vertex(circlePoints[(i + 1) % _segments]);
            }

            GL.End();
            GL.PopMatrix();
        }

        private void OnDrawGizmosSelected()
        {
            if (_enemyStatus == null)
            {
                _enemyStatus = GetComponentInParent<EnemyStatus>();
            }
            if (_enemyStatus == null) return;

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
    }
}
