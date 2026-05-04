using UnityEngine;

namespace GameProject24.Enemy
{
    /// <summary>
    /// EnemySight의 탐지 범위를 반투명 3D 원뿔로 씬 뷰 및 게임 화면 모두에 시각화합니다.
    /// 원뿔의 높이는 SightDistance, 반각은 FieldOfView / 2 입니다.
    /// IsPlayerSpotted 여부에 따라 노란색(미탐지) / 빨간색(탐지)으로 전환됩니다.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(EnemyStatus))]
    public class EnemySightGizmo : MonoBehaviour
    {
        [Header("Gizmo Settings")]
        [Tooltip("원뿔 채우기 투명도입니다. (0: 완전 투명 ~ 1: 완전 불투명)")]
        [Range(0f, 1f)]
        [SerializeField] private float _alpha = 0.15f;

        [Tooltip("원뿔 밑면 원을 분할하는 세그먼트 수입니다. 높을수록 부드럽지만 성능을 소모합니다.")]
        [Range(8, 64)]
        private int _segments = 32;

        private EnemyStatus _enemyStatus;

        /// <summary> GL 렌더링에 사용할 반투명 머티리얼입니다. </summary>
        private Material _coneMaterial;

        private void Awake()
        {
            _enemyStatus = GetComponent<EnemyStatus>();
            CreateMaterial();
        }

        private void OnEnable()
        {
            if (_coneMaterial == null)
            {
                CreateMaterial();
            }
        }

        /// <summary>
        /// 알파 블렌딩을 지원하는 GL용 머티리얼을 생성합니다.
        /// </summary>
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

        /// <summary>
        /// 카메라가 씬을 렌더링할 때마다 호출됩니다.
        /// [ExecuteAlways] 덕분에 에디터 씬 뷰와 게임 뷰 모두에서 동작합니다.
        /// </summary>
        private void OnRenderObject()
        {
            if (_enemyStatus == null || _coneMaterial == null) return;

            float fov       = _enemyStatus.FieldOfView;
            float sightDist = _enemyStatus.SightDistance;
            bool isSpotted  = _enemyStatus.IsPlayerSpotted;

            // 탐지 여부에 따라 색상 결정
            Color fillColor = isSpotted
                ? new Color(1f, 0f, 0f, _alpha)
                : new Color(1f, 1f, 0f, _alpha);

            Color lineColor = new Color(fillColor.r, fillColor.g, fillColor.b, Mathf.Min(_alpha * 5f, 1f));

            // 원뿔 기하 계산
            float halfFovRad = fov * 0.5f * Mathf.Deg2Rad;
            float baseRadius = sightDist * Mathf.Tan(halfFovRad);

            Vector3 apex       = transform.position + Vector3.up * 1.0f;
            Vector3 baseCenter = apex + transform.forward * sightDist;
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

        private void OnDestroy()
        {
            if (_coneMaterial != null)
            {
                DestroyImmediate(_coneMaterial);
            }
        }
    }
}
