using UnityEngine;

namespace MushOut.Environment
{
    /// <summary>
    /// Pre-fractured swap 방식의 파괴 가능한 오브젝트입니다.
    /// 지정된 레이어의 오브젝트와 충돌 시 원본 오브젝트를 비활성화하고,
    /// 미리 준비된 조각(fracture) 프리팹을 해당 위치에 생성합니다.
    /// </summary>
    public class BreakableObject : MonoBehaviour
    {
        [Header("Fracture Settings")]
        [Tooltip("충돌 시 생성할 조각난 버전의 프리팹입니다.")]
        [SerializeField] private GameObject _fracturedPrefab;

        [Tooltip("파괴를 트리거할 레이어 마스크입니다. 이 레이어의 오브젝트와 충돌 시 파괴됩니다.")]
        [SerializeField] private LayerMask _breakTriggerLayer;

        [Header("Fragment Physics")]
        [Tooltip("조각들에 가할 폭발 반경입니다.")]
        [SerializeField] private float _explosionRadius = 2.0f;

        [Tooltip("조각들에 가할 폭발 힘입니다.")]
        [SerializeField] private float _explosionForce = 200.0f;

        [Tooltip("조각들에 가할 위쪽 방향 힘의 비율입니다.")]
        [Range(0f, 1f)]
        [SerializeField] private float _upwardModifier = 0.3f;

        [Header("Cleanup")]
        [Tooltip("생성된 조각 오브젝트가 제거되기까지의 시간입니다. (0 이하면 제거하지 않음)")]
        [SerializeField] private float _fragmentLifetime = 5.0f;

        /// <summary> 이미 파괴 처리가 진행 중인지 여부 (중복 호출 방지) </summary>
        private bool _isBroken = false;

        /// <summary>
        /// 다른 Collider와 물리적으로 충돌했을 때 호출됩니다.
        /// Trigger가 아닌 일반 Collider를 사용하는 경우에 해당합니다.
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            if (_isBroken) return;
            if (!IsInBreakLayer(collision.gameObject.layer)) return;

            Break(collision.GetContact(0).point);
        }

        /// <summary>
        /// Trigger Collider와 접촉했을 때 호출됩니다.
        /// IsTrigger가 켜진 Collider에 반응하는 경우에 해당합니다.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (_isBroken) return;
            if (!IsInBreakLayer(other.gameObject.layer)) return;

            Break(transform.position);
        }

        /// <summary>
        /// 파괴 처리의 메인 로직입니다.
        /// 조각 프리팹을 현재 위치/회전에 맞게 생성하고,
        /// 각 조각에 폭발력을 적용한 뒤 원본 오브젝트를 비활성화합니다.
        /// </summary>
        /// <param name="contactPoint">충돌 지점 (폭발 진원지)</param>
        public void Break(Vector3 contactPoint)
        {
            _isBroken = true;

            if (_fracturedPrefab == null)
            {
                Debug.LogWarning($"[BreakableObject] '{gameObject.name}'에 _fracturedPrefab이 할당되지 않았습니다.", this);
                return;
            }

            // 원본과 동일한 위치/회전으로 조각 프리팹을 생성
            GameObject fractureInstance = Instantiate(
                _fracturedPrefab,
                transform.position,
                transform.rotation
            );

            // 조각들에 폭발력 적용
            ApplyExplosionForce(fractureInstance, contactPoint);

            // 일정 시간 후 조각 오브젝트 제거
            if (_fragmentLifetime > 0f)
            {
                Destroy(fractureInstance, _fragmentLifetime);
            }

            // 원본 오브젝트 비활성화
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 생성된 조각 인스턴스의 모든 Rigidbody에 폭발력을 적용합니다.
        /// </summary>
        /// <param name="fractureInstance">생성된 조각 프리팹 인스턴스</param>
        /// <param name="explosionCenter">폭발의 진원지</param>
        private void ApplyExplosionForce(GameObject fractureInstance, Vector3 explosionCenter)
        {
            Rigidbody[] fragments = fractureInstance.GetComponentsInChildren<Rigidbody>();

            foreach (Rigidbody rb in fragments)
            {
                // [추가] 프리팹에 설정된 isKinematic을 해제하여 물리 엔진의 영향을 받게 함
                rb.isKinematic = false;
                
                rb.AddExplosionForce(
                    _explosionForce,
                    explosionCenter,
                    _explosionRadius,
                    _upwardModifier,
                    ForceMode.Impulse
                );
            }
        }

        /// <summary>
        /// 주어진 레이어가 파괴 트리거 레이어에 포함되는지 확인합니다.
        /// </summary>
        /// <param name="layer">확인할 오브젝트의 레이어 인덱스</param>
        /// <returns>파괴 트리거 레이어에 포함되면 true</returns>
        private bool IsInBreakLayer(int layer)
        {
            return (_breakTriggerLayer.value & (1 << layer)) != 0;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 폭발 반경 시각화
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, _explosionRadius);
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, _explosionRadius);
        }
#endif
    }
}
