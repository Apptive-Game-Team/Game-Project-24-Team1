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

            // 수면의 Y 좌표 계산 (콜라이더가 회전되었을 경우 bounds.max.y는 AABB의 최상단이므로 오차가 발생함)
            float waterSurfaceY = _waterCollider.bounds.max.y;
            
            if (_waterCollider is BoxCollider box)
            {
                // 플레이어의 현재 위치를 물 오브젝트의 로컬 좌표계로 변환
                Vector3 localPos = box.transform.InverseTransformPoint(other.transform.position);
                // 로컬 좌표계에서 Y값을 박스의 최상단(윗면)으로 고정
                localPos.y = box.center.y + box.size.y * 0.5f;
                // 다시 월드 좌표로 변환하여 정확한 수면의 Y값을 얻음
                waterSurfaceY = box.transform.TransformPoint(localPos).y;
            }
            else
            {
                // BoxCollider가 아닌 경우 (Sphere, Capsule, Mesh 등) Raycast를 사용하여 가장 위쪽 표면을 찾음
                Vector3 rayOrigin = other.transform.position;
                rayOrigin.y = _waterCollider.bounds.max.y + 1.0f;
                Ray ray = new Ray(rayOrigin, Vector3.down);
                
                if (_waterCollider.Raycast(ray, out RaycastHit hit, _waterCollider.bounds.size.y + 2.0f))
                {
                    waterSurfaceY = hit.point.y;
                }
            }
            
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
