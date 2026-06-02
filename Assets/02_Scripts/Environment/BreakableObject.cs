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

        [Tooltip("조각난 프리팹이 생성될 위치(Transform)입니다. 지정하지 않으면 현재 오브젝트의 위치에 생성됩니다.")]
        [SerializeField] private Transform _fracturedSpawnPosition;

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

        [Header("Restore Settings")]
        [Tooltip("파괴된 원본 오브젝트를 일정 시간 후 복구할지 여부입니다.")]
        [SerializeField] private bool _shouldRestore = false;

        [Tooltip("복구하기까지의 대기 시간(초)입니다. (_shouldRestore가 true일 때만 적용)")]
        [SerializeField] private float _restoreDelay = 3.0f;

        [Header("Events")]
        [Tooltip("오브젝트가 부서질 때 실행될 이벤트입니다.")]
        public UnityEngine.Events.UnityEvent OnBreakEvent;

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

            // 만약 충돌한 대상이 적(Enemy)이라면, 현재 상태가 Attacking(돌진 중)인지 확인
            var enemy = collision.gameObject.GetComponentInParent<MushOut.Enemy.EnemyController>();
            if (enemy != null && enemy.CurrentState != MushOut.Enemy.EnemyController.State.Attacking)
            {
                return; // 적이지만 돌진 중이 아니라면 부서지지 않음
            }

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

            // 만약 충돌한 대상이 적(Enemy)이라면, 현재 상태가 Attacking(돌진 중)인지 확인
            var enemy = other.gameObject.GetComponentInParent<MushOut.Enemy.EnemyController>();
            if (enemy != null && enemy.CurrentState != MushOut.Enemy.EnemyController.State.Attacking)
            {
                return; // 적이지만 돌진 중이 아니라면 부서지지 않음
            }

            Break(transform.position);
        }

        /// <summary>
        /// 외부 스크립트(또는 UnityEvent)에서 매개변수 없이 호출하기 위한 편의성 메서드입니다.
        /// 오브젝트의 현재 중심 위치를 폭발 진원지로 사용합니다.
        /// </summary>
        public void Break()
        {
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
            
            // 인스펙터에 연결된 이벤트(특정 오브젝트의 메서드 등) 실행
            OnBreakEvent?.Invoke();

            if (_fracturedPrefab == null)
            {
                if (_shouldRestore)
                {
                    gameObject.SetActive(false);
                    RestoreAfterDelay();
                }
                else
                {
                    Destroy(gameObject);
                }
                return;
            }

            // 원본과 동일한 위치/회전으로 조각 프리팹을 생성 (또는 지정된 위치)
            Vector3 spawnPosition = _fracturedSpawnPosition != null ? _fracturedSpawnPosition.position : transform.position;
            Quaternion spawnRotation = _fracturedSpawnPosition != null ? _fracturedSpawnPosition.rotation : transform.rotation;

            GameObject fractureInstance = Instantiate(
                _fracturedPrefab,
                spawnPosition,
                spawnRotation
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

            if (_shouldRestore)
            {
                RestoreAfterDelay();
            }
        }

        /// <summary>
        /// 비활성화된 원본 오브젝트를 일정 시간 후 다시 활성화하고 파괴 가능한 상태로 되돌립니다.
        /// </summary>
        private async void RestoreAfterDelay()
        {
            await System.Threading.Tasks.Task.Delay(Mathf.RoundToInt(_restoreDelay * 1000));
            
            // 에디터 종료나 씬 변경으로 오브젝트가 파괴되지 않았는지 확인
            if (this != null && gameObject != null)
            {
                _isBroken = false;
                gameObject.SetActive(true);
            }
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
