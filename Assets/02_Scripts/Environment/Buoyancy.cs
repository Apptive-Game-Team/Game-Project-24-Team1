using UnityEngine;
using Nexush.Player;

namespace Nexush.Environment
{
    [RequireComponent(typeof(Collider))]
    public class Buoyancy : MonoBehaviour
    {
        [Header("Buoyancy Settings")]
        [Tooltip("기본 부력 계수 (값이 클수록 물 밖으로 띄워 올리는 힘이 강해집니다)")]
        [SerializeField] private float buoyancyPower = 7.0f;

        private Collider _waterCollider;

        private void Awake()
        {
            _waterCollider = GetComponent<Collider>();
            if (_waterCollider == null || !_waterCollider.isTrigger)
            {
                Debug.LogWarning("[Buoyancy] 물 오브젝트에 Trigger Collider가 필요하며, Is Trigger가 체크되어야 합니다!");
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (_waterCollider == null) return;

            // 수면의 Y 좌표 계산 (콜라이더의 가장 위쪽 끝 지점)
            float waterSurfaceY = _waterCollider.bounds.max.y;
            
            // 오브젝트 중심 위치가 수면보다 얼마나 깊이 들어갔는지 계산
            float depth = waterSurfaceY - other.transform.position.y;

            if (depth > 0f)
            {
                // 깊을수록 강한 위쪽 방향의 힘(부력) 계산
                float force = depth * buoyancyPower;

                // 1. PlayerController를 가진 플레이어인 경우
                if (other.TryGetComponent<PlayerController>(out var player))
                {
                    player.AddBuoyancy(force);
                }
                // 2. 일반 Rigidbody를 가진 오브젝트인 경우
                else if (other.TryGetComponent<Rigidbody>(out var rb))
                {
                    // 질량과 무관하게 오브젝트의 수평 단면적(X * Z 스케일)이 클수록 부력을 더 받도록 설정
                    float areaScale = other.transform.localScale.x * other.transform.localScale.z;

                    // 중력을 상쇄하는 기본 힘은 유지하고, 물에 잠겼을 때 받는 추가 부력(force)만 넓이에 비례해 증폭
                    float rbForce = Mathf.Abs(Physics.gravity.y) + (force * areaScale);

                    // Rigidbody에는 FixedUpdate 주기에 맞는 ForceMode.Acceleration 사용
                    rb.AddForce(Vector3.up * rbForce, ForceMode.Acceleration);
                    
                    // 마찰력(Drag)도 유지
                    rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z), Time.fixedDeltaTime * 1.5f);
                }
            }
        }
    }
}
