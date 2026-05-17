using UnityEngine;
using UnityEngine.InputSystem;
using MushOut.Enemy;

namespace MushOut.Player
{
    /// <summary>
    /// 특정 키 입력 시 플레이어 주변 radius 내의 모든 Enemy를 Attacking 상태로 강제 전환합니다.
    /// 테스트/디버그 또는 특수 스킬 트리거 용도로 사용합니다.
    /// </summary>
    public class PlayerEnemyAggro : MonoBehaviour
    {
        [Header("Aggro Settings")]
        [Tooltip("Attacking 상태로 강제 전환할 최대 탐지 반경 (m)")]
        [SerializeField] private float _aggroRadius = 15f;

        [Tooltip("기능을 발동할 키 (New Input System Key 열거형)")]
        [SerializeField] private Key _aggroKey = Key.G;

        [Tooltip("Gizmo로 반경을 표시할지 여부 (에디터 전용)")]
        [SerializeField] private bool _showGizmo = true;

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current[_aggroKey].wasPressedThisFrame)
            {
                TriggerNearbyEnemies();
            }
        }

        /// <summary>
        /// 반경 내 모든 EnemyController를 Attacking 상태로 전환합니다.
        /// Dead 또는 Stunned 상태의 적은 제외합니다.
        /// </summary>
        private void TriggerNearbyEnemies()
        {
            // OverlapSphere로 반경 내 콜라이더를 가져온 후 EnemyController 탐색
            Collider[] hits = Physics.OverlapSphere(transform.position, _aggroRadius);
            int triggeredCount = 0;

            foreach (Collider hit in hits)
            {
                EnemyController enemy = hit.GetComponentInParent<EnemyController>();
                if (enemy == null) continue;

                // Dead, Stunned 상태는 건너뜀
                if (enemy.CurrentState == EnemyController.State.Dead ||
                    enemy.CurrentState == EnemyController.State.Stunned) continue;

                // 이미 Attacking 상태면 건너뜀
                if (enemy.CurrentState == EnemyController.State.Attacking) continue;

                enemy.ChangeState(EnemyController.State.Attacking);
                triggeredCount++;
            }

            Debug.Log($"[PlayerEnemyAggro] {triggeredCount}개의 적을 Attacking 상태로 전환했습니다. (반경: {_aggroRadius}m)");
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!_showGizmo) return;
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
            Gizmos.DrawSphere(transform.position, _aggroRadius);
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, _aggroRadius);
        }
#endif
    }
}
