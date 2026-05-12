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
            if (other.CompareTag("Player"))
            {
                PlayerController player = other.GetComponent<PlayerController>();
                if (player != null && _waterCollider != null)
                {
                    // 수면의 Y 좌표 계산 (콜라이더의 가장 위쪽 끝 지점)
                    float waterSurfaceY = _waterCollider.bounds.max.y;
                    
                    // 플레이어 중심 위치가 수면보다 얼마나 깊이 들어갔는지 계산
                    float depth = waterSurfaceY - player.transform.position.y;

                    if (depth > 0f)
                    {
                        // 깊을수록 강한 위쪽 방향의 힘(부력) 계산
                        // OnTriggerStay는 FixedUpdate 주기이므로 Time.fixedDeltaTime 사용이 권장되지만,
                        // PlayerController의 Update 주기와 맞추기 위해 Force만 전달하고 
                        // 내부적으로 Time.deltaTime을 곱하도록 설계하는 것이 좋습니다.
                        float force = depth * buoyancyPower;
                        player.AddBuoyancy(force);
                    }
                }
            }
        }
    }
}
