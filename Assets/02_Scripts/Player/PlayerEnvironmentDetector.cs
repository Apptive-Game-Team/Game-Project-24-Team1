using UnityEngine;

namespace MushOut.Player
{
    public class PlayerEnvironmentDetector : MonoBehaviour
    {
        [Header("지면 체크 설정")]
        [Tooltip("지면으로 인식할 레이어 설정입니다.")]
        public LayerMask groundLayers;

        [Header("물(Water) 설정")]
        [Tooltip("물 판정을 위한 레이어 설정입니다.")]
        public LayerMask waterLayer;

        public bool IsGrounded { get; private set; } = true;
        public bool IsInWater { get; private set; }
        public Vector3 HitNormal { get; private set; } = Vector3.up;
        public Transform GroundTransform { get; private set; } // 플레이어가 밟고 있는 바닥 오브젝트

        private CharacterController _controller;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (groundLayers.value == 0)
            {
                groundLayers = ~0; // 기본적으로 모든 레이어 체크
            }
        }

        public void CheckEnvironment()
        {
            CheckGrounded();
            CheckWater();
        }

        private void CheckGrounded()
        {
            float rayLength = _controller != null ? _controller.skinWidth + 0.1f : 0.2f;
            Vector3 rayOrigin = transform.position + Vector3.up * 0.05f;

            RaycastHit hit;
            bool hitGround = Physics.Raycast(rayOrigin, Vector3.down, out hit, rayLength, groundLayers, QueryTriggerInteraction.Ignore);
            
            IsGrounded = (_controller != null && _controller.isGrounded) || hitGround;
            GroundTransform = hitGround ? hit.transform : null;
        }

        private void CheckWater()
        {
            if (_controller == null) return;
            Vector3 center = transform.position + _controller.center;
            IsInWater = Physics.CheckSphere(center, _controller.radius + 0.1f, waterLayer, QueryTriggerInteraction.Collide);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            HitNormal = hit.normal;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Vector3 rayOrigin = transform.position + Vector3.up * 0.05f;
            float rayLength = (_controller != null) ? _controller.skinWidth + 0.1f : 0.2f;

            Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * rayLength);
            Gizmos.DrawWireSphere(rayOrigin + Vector3.down * rayLength, 0.05f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, Vector3.down * 0.2f);
        }
    }
}
