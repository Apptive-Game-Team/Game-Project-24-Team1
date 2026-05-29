using UnityEngine;
using MushOut.Player;

namespace MushOut.Environment
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
            
            // 기존에는 flowForce를 '힘(Force)'으로 썼지만, 이제는 '목표 속도(Target Speed)'로 취급합니다.
            Vector3 targetVelocity = worldDirection * flowForce;

            // 1. PlayerController를 가진 플레이어인 경우
            if (other.TryGetComponent<PlayerController>(out var player))
            {
                // 플레이어의 물 속 저항(Drag) 기본값이 3.0이므로,
                // 목표 속도를 유지하기 위해 (목표 속도 * 3.0) 만큼의 힘을 지속적으로 가해줍니다.
                // 이렇게 하면 최종적으로 플레이어의 떠내려가는 속도가 targetVelocity와 정확히 일치하게 됩니다.
                player.AddExternalForce(targetVelocity * 3.0f);
            }
            // 2. 일반 Rigidbody를 가진 오브젝트인 경우 (폭탄, 나무상자 등)
            else if (other.TryGetComponent<Rigidbody>(out var rb))
            {
                // 중력이나 부력(Y축 오르내림)을 방해하지 않기 위해 X, Z축(수평) 속도만 고려
                Vector3 currentHorizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                Vector3 targetHorizontalVel = new Vector3(targetVelocity.x, 0, targetVelocity.z);

                // 목표 속도까지 얼마나 부족한지(차이) 계산
                Vector3 velocityDiff = targetHorizontalVel - currentHorizontalVel;

                // 질량(Mass)을 무시하고 직접 속도를 변화시키는 VelocityChange 모드 사용
                // 단번에 훅 바뀌지 않고 자연스럽게 가속되도록 Time.fixedDeltaTime과 보간 상수(5.0f)를 곱해줍니다.
                rb.AddForce(velocityDiff * 5.0f * Time.fixedDeltaTime, ForceMode.VelocityChange);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // flowDirection이 영벡터(0,0,0)이면 화살표를 표시하지 않음
            if (flowDirection == Vector3.zero) return;

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
            Vector3 left  = Quaternion.LookRotation(worldDirection) * Quaternion.Euler(0, 180 - 20, 0) * new Vector3(0, 0, 1);
            Gizmos.DrawRay(endPos, right * 0.5f);
            Gizmos.DrawRay(endPos, left * 0.5f);
        }
    }
}
