using UnityEngine;
using MushOut.Player;

namespace MushOut.Interactables
{
    /// <summary>
    /// 사다리 영역을 정의하는 스크립트입니다.
    /// Trigger Collider를 통해 플레이어의 진입을 감지합니다.
    /// topPoint Transform을 통해 사다리 꼭대기 위치를 지정합니다.
    /// </summary>
    public class Ladder : MonoBehaviour
    {
        private BoxCollider _boxCollider;

        private void Awake()
        {
            _boxCollider = GetComponent<BoxCollider>();
        }

        public Vector3 GetTopPoint()
        {
            if (_boxCollider == null)
            {
                _boxCollider = GetComponent<BoxCollider>();
            }

            if (_boxCollider != null)
            {
                // BoxCollider의 로컬 상단 중앙 지점 계산 후 월드 좌표로 변환
                Vector3 localTop = _boxCollider.center + new Vector3(0f, _boxCollider.size.y * 0.5f, 0f);
                return transform.TransformPoint(localTop);
            }

            return transform.position;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<PlayerClimbHandler>(out var climbHandler))
            {
                // 사다리의 정면 방향과 꼭대기 위치를 함께 전달
                bool isGrounded = false;
                if (other.TryGetComponent<PlayerController>(out var player)) isGrounded = player.IsGrounded;
                
                climbHandler.SetNearLadder(true, transform.forward, GetTopPoint(), true, isGrounded);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<PlayerClimbHandler>(out var climbHandler))
            {
                bool isGrounded = false;
                if (other.TryGetComponent<PlayerController>(out var player)) isGrounded = player.IsGrounded;

                climbHandler.SetNearLadder(false, Vector3.zero, Vector3.zero, false, isGrounded);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 targetTop = GetTopPoint();
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(targetTop, 0.15f);
            Gizmos.DrawLine(transform.position, targetTop);
        }
    }
}
