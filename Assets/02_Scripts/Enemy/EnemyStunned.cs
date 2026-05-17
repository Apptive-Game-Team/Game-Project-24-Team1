using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace MushOut.Enemy
{
    /// <summary>
    /// 에너미가 Stunned(기절) 상태일 때 작동하는 로직입니다.
    /// 이동을 멈추고 빨간색으로 변한 뒤, 일정 시간 후 원래대로 복귀합니다.
    /// </summary>
    [RequireComponent(typeof(EnemyStatus), typeof(NavMeshAgent))]
    public class EnemyStunned : MonoBehaviour
    {
        private EnemyStatus _enemyStatus;
        private NavMeshAgent _agent;

        [Header("Stunned Settings")]
        [Tooltip("기절 지속 시간(초)입니다.")]
        [Range(1f, 30f)]
        [SerializeField] private float _stunnedDuration = 10f;

        private bool _isRunning = false;

        /// <summary> 기절 연출 중 색상을 변경할 렌더러들입니다. </summary>
        private Renderer[] _renderers;

        /// <summary> 기절 전 각 렌더러의 원래 색상들을 저장합니다. </summary>
        private Color[] _originalColors;

        private void Awake()
        {
            _enemyStatus = GetComponent<EnemyStatus>();
            _agent = GetComponent<NavMeshAgent>();

            // 자신과 자식 오브젝트의 모든 렌더러 캐싱
            _renderers = GetComponentsInChildren<Renderer>();
            _originalColors = new Color[_renderers.Length];
        }

        private void Update()
        {
            // Stunned 상태가 됐을 때 코루틴 1회 시작
            if (_enemyStatus.CurrentState == EnemyStatus.State.Stunned && !_isRunning)
            {
                StartCoroutine(StunnedRoutine());
            }
        }

        /// <summary>
        /// 기절 전체 시퀀스를 처리하는 코루틴입니다.
        /// (이동 정지 → 빨간색 변경 → 대기 → 색상 복구 → 이동 재개 → 이전 상태 복귀)
        /// </summary>
        private IEnumerator StunnedRoutine()
        {
            _isRunning = true;

            // 1. 이동 정지
            _agent.isStopped = true;

            // 2. 원래 색상 저장 후 빨간색으로 변경
            for (int i = 0; i < _renderers.Length; i++)
            {
                _originalColors[i] = _renderers[i].material.color;
                _renderers[i].material.color = Color.red;
            }

            // 3. 기절 지속 시간 대기
            yield return new WaitForSeconds(_stunnedDuration);

            // 4. 원래 색상 복구
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].material.color = _originalColors[i];
            }

            // 5. 이동 재개
            _agent.isStopped = false;

            // 6. 초기 상태로 복귀
            _isRunning = false;
            _enemyStatus.ChangeState(_enemyStatus.InitialState);
        }

        private void OnDisable()
        {
            // 오브젝트가 비활성화될 때 코루틴 안전하게 정리 및 색상 복구
            StopAllCoroutines();

            if (_isRunning && _renderers != null)
            {
                for (int i = 0; i < _renderers.Length; i++)
                {
                    _renderers[i].material.color = _originalColors[i];
                }
            }

            _isRunning = false;
        }
    }
}
