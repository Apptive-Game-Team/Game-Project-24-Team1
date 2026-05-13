using UnityEngine;
using Nexush.Player;

namespace Nexush.Environment
{
    /// <summary>
    /// 물이 흐르는 방향으로 들어온 오브젝트(플레이어, Rigidbody)를 밀어내는 스크립트입니다.
    /// Trigger 형태의 Collider가 물 오브젝트에 부착되어 있어야 합니다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WaterFlow : MonoBehaviour
    {
        [Header("Water Flow Settings")]
        [Tooltip("물이 흐르는 로컬 방향 벡터입니다. (오브젝트를 회전시키면 물이 흐르는 방향도 같이 회전합니다)")]
        [SerializeField] private Vector3 flowDirection = Vector3.forward;

        [Tooltip("물 흐름이 밀어내는 힘의 세기입니다.")]
        [SerializeField] private float flowForce = 5.0f;

        private Collider _waterCollider;

        private void Awake()
        {
            _waterCollider = GetComponent<Collider>();
            if (_waterCollider == null || !_waterCollider.isTrigger)
            {
                Debug.LogWarning("[WaterFlow] 물 흐름 오브젝트에 Trigger Collider가 필요하며, Is Trigger가 체크되어야 합니다!");
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (_waterCollider == null) return;

            // 로컬 방향 벡터를 월드 방향 벡터로 변환 (오브젝트 회전 반영)
            Vector3 worldDirection = transform.TransformDirection(flowDirection).normalized;
            Vector3 forceVector = worldDirection * flowForce;

            // 1. PlayerController를 가진 플레이어인 경우
            if (other.TryGetComponent<PlayerController>(out var player))
            {
                player.AddExternalForce(forceVector);
            }
            // 2. 일반 Rigidbody를 가진 오브젝트인 경우
            else if (other.TryGetComponent<Rigidbody>(out var rb))
            {
                // 질량(Mass) 스펙트럼 제한 (0 ~ 1.0): 0(에 가까울수록) 100% 힘, 1.0 이상이면 0% 힘
                float massMultiplier = Mathf.InverseLerp(1.0f, 0.0f, rb.mass);

                if (massMultiplier > 0f)
                {
                    // Rigidbody에는 FixedUpdate 주기에 맞는 ForceMode.Acceleration 사용
                    rb.AddForce(forceVector * massMultiplier, ForceMode.Acceleration);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 에디터에서 물 흐름 방향을 시각적으로 확인하기 위한 기즈모 (노란색 선)
            Gizmos.color = Color.yellow;
            Vector3 drawPos = transform.position;
            if (TryGetComponent<Collider>(out var col))
            {
                // 콜라이더의 가장 윗부분(수면) 중심을 기점으로 설정
                drawPos = col.bounds.center;
                drawPos.y = col.bounds.max.y;
            }

            // 기즈모에서도 오브젝트 회전을 반영하여 그리기
            Vector3 worldDirection = transform.TransformDirection(flowDirection).normalized;
            Vector3 endPos = drawPos + (worldDirection * 2.0f);
            Gizmos.DrawLine(drawPos, endPos);
            
            // 화살표 모양 표시
            Vector3 right = Quaternion.LookRotation(worldDirection) * Quaternion.Euler(0, 180 + 20, 0) * new Vector3(0, 0, 1);
            Vector3 left = Quaternion.LookRotation(worldDirection) * Quaternion.Euler(0, 180 - 20, 0) * new Vector3(0, 0, 1);
            Gizmos.DrawRay(endPos, right * 0.5f);
            Gizmos.DrawRay(endPos, left * 0.5f);
        }
    }
}
