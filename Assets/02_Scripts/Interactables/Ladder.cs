using UnityEngine;
using Nexush.Player;

namespace Nexush.Interactables
{
    /// <summary>
    /// 사다리 영역을 정의하는 스크립트입니다.
    /// Trigger Collider를 통해 플레이어의 진입을 감지합니다.
    /// topPoint Transform을 통해 사다리 꼭대기 위치를 지정합니다.
    /// </summary>
    public class Ladder : MonoBehaviour
    {
        [Tooltip("사다리 꼭대기에 도달했을 때 플레이어가 이동할 목표 지점 Transform입니다.")]
        [SerializeField] private Transform topPoint;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<PlayerController>(out var player))
            {
                // 사다리의 정면 방향과 꼭대기 위치를 함께 전달
                player.SetNearLadder(true, transform.forward, topPoint != null ? topPoint.position : Vector3.zero, topPoint != null);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<PlayerController>(out var player))
            {
                player.SetNearLadder(false, Vector3.zero, Vector3.zero, false);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (topPoint == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(topPoint.position, 0.15f);
            Gizmos.DrawLine(transform.position, topPoint.position);
        }
    }
}
