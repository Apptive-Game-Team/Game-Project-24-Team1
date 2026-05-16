using UnityEngine;
using MushOut.Player;

namespace MushOut.Environment
{
    [RequireComponent(typeof(Collider))]
    public class Buoyancy : MonoBehaviour
    {
        [Header("Buoyancy Settings")]
        [Tooltip("기본 부력 계수 (값이 클수록 물 밖으로 띄워 올리는 힘이 강해집니다)")]
        [SerializeField] private float buoyancyPower = 11.0f;

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
            
            // 부력을 계산할 기준점 (오브젝트는 피벗, 플레이어는 중심)
            Vector3 targetPos = other.transform.position;

            // 플레이어인지 확인
            bool isPlayer = other.TryGetComponent<PlayerController>(out var player);
            if (isPlayer)
            {
                // 플레이어는 피벗(transform.position)이 발끝이므로, 중심점(Center)을 더해 가슴/배 높이에서 계산되도록 보정
                var cc = other.GetComponent<CharacterController>();
                if (cc != null)
                {
                    targetPos.y += cc.center.y;
                }
            }

            // 기준점이 수면보다 얼마나 깊이 들어갔는지 계산
            float depth = waterSurfaceY - targetPos.y;

            if (depth > 0f)
            {
                // 수면 근처(0 ~ 0.5m)에서는 힘을 부드럽게 줄여 수면 위로 튕겨오르는 현상 방지
                // 깊이가 0.5m 이상이면 1(최대 부력) 유지
                float depthFactor = Mathf.Clamp01(depth * 2.0f);
                float force = buoyancyPower * depthFactor;

                // 1. PlayerController를 가진 플레이어인 경우
                if (isPlayer)
                {
                    // 플레이어는 독자적인 중력값(보통 -15)을 사용하므로 기본 중력 상쇄값을 포함해 전달
                    player.AddBuoyancy((15.0f + force) * depthFactor);
                }
                // 2. 일반 Rigidbody를 가진 오브젝트인 경우
                else if (other.TryGetComponent<Rigidbody>(out var rb))
                {
                    // 질량과 무관하게 오브젝트의 수평 단면적(X * Z 스케일)이 클수록 부력을 더 받도록 설정
                    float areaScale = other.transform.localScale.x * other.transform.localScale.z;

                    // 중력을 상쇄하는 힘과 부력을 수면 근처에서 서서히 줄여서 중력과 균형을 이루게 함
                    float rbForce = Mathf.Abs(Physics.gravity.y) * depthFactor + (force * areaScale);

                    // Rigidbody에는 FixedUpdate 주기에 맞는 ForceMode.Acceleration 사용
                    rb.AddForce(Vector3.up * rbForce, ForceMode.Acceleration);
                    
                    // 수면에 가까워질수록 마찰력(Drag)을 강하게 주어 진자운동(튀는 현상)을 감쇠시킴
                    float damping = Mathf.Lerp(4.0f, 1.5f, 1f - depthFactor); // 수면 근처(depthFactor=0)일수록 강한 저항(4.0)
                    rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z), Time.fixedDeltaTime * damping);
                }
            }
        }
    }
}
