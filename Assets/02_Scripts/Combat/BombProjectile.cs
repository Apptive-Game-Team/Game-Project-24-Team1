using UnityEngine;
using System.Collections;

namespace MushOut.Combat
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    /// <summary>
    /// 발사된 후 일정 시간이 지나면 폭발하는 폭탄 발사체입니다.
    /// 지정된 레이어에 충돌하면 표면에 박혀 고정된 후 카운트다운을 시작합니다.
    /// </summary>
    public class BombProjectile : MonoBehaviour
    {
        [Header("Explosion Settings")]
        [Tooltip("발사된 시점부터 폭발하기까지의 대기 시간(초)입니다.")]
        [SerializeField] private float explodeTime = 3.0f;
        
        [Tooltip("폭발 시 제자리에 생성될 폭발 이펙트/판정 프리팹입니다.")]
        [SerializeField] private GameObject ExplosionPrefab;

        [Tooltip("폭발이 미치는 물리적 반경(벽 파괴 등)입니다.")]
        [SerializeField] private float explosionRadius = 3.0f;

        [Header("Collision Settings")]
        [Tooltip("폭탄이 부딪혔을 때 박혀서 고정될 대상 레이어입니다.")]
        [SerializeField] private LayerMask stickLayer;

        private Rigidbody _rb;
        private bool _isStuck = false;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        /// <summary>
        /// PlayerBombAbility에서 발사할 때 호출됩니다.
        /// </summary>
        /// <param name="initialVelocity">초기 발사 속도</param>
        public void Initialize(Vector3 initialVelocity)
        {
            _rb.linearVelocity = initialVelocity;

            // 발사된 순간부터 타이머 시작
            StartCoroutine(ExplodeRoutine());
        }

        /// <summary>
        /// 폭탄이 다른 콜라이더와 충돌했을 때 호출됩니다.
        /// 충돌 대상이 stickLayer에 해당하면 해당 표면에 박힙니다.
        /// </summary>
        /// <param name="collision">충돌 정보</param>
        private void OnCollisionEnter(Collision collision)
        {
            if (_isStuck) return;

            // 충돌한 대상이 stickLayer에 포함되어 있는지 확인
            if (((1 << collision.gameObject.layer) & stickLayer.value) != 0)
            {
                Debug.Log($"[BombProjectile] 충돌 확인! 대상: {collision.gameObject.name}, 레이어: {LayerMask.LayerToName(collision.gameObject.layer)}");
                StickToSurface(collision.contacts[0].point, collision.collider.transform);
            }
        }

        /// <summary>
        /// 폭탄이 트리거 콜라이더에 진입했을 때 호출됩니다.
        /// 진입 대상이 stickLayer에 해당하면 해당 위치에 박힙니다.
        /// </summary>
        /// <param name="other">진입한 콜라이더</param>
        private void OnTriggerEnter(Collider other)
        {
            if (_isStuck) return;

            if (((1 << other.gameObject.layer) & stickLayer.value) != 0)
            {
                Debug.Log($"[BombProjectile] 트리거 진입! 대상: {other.gameObject.name}, 레이어: {LayerMask.LayerToName(other.gameObject.layer)}");
                StickToSurface(transform.position, other.transform);
            }
        }

        /// <summary>
        /// 폭탄을 지정된 위치에 고정시킵니다.
        /// 물리 연산을 정지하고 대상 오브젝트의 자식으로 등록하여 함께 움직이게 합니다.
        /// </summary>
        /// <param name="stickPoint">박힐 위치</param>
        /// <param name="targetTransform">박힐 대상 Transform</param>
        private void StickToSurface(Vector3 stickPoint, Transform targetTransform)
        {
            _isStuck = true;

            // 물리 연산 정지 → 해당 위치에 고정
            _rb.isKinematic = true;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        private IEnumerator ExplodeRoutine()
        {
            yield return new WaitForSeconds(explodeTime);

            Explode();
        }

        /// <summary>
        /// 폭탄을 폭발시킵니다.
        /// 폭발 이펙트를 생성하고, 반경 내의 파괴 가능한 오브젝트를 직접 파괴한 뒤 자신을 제거합니다.
        /// </summary>
        private void Explode()
        {
            if (ExplosionPrefab != null)
            {
                // 생성되는 Explosion의 Layer는 기획에 따라 에디터의 프리팹에서 'Explosion'로 설정해야 합니다.
                Instantiate(ExplosionPrefab, transform.position, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning("[BombProjectile] ExplosionPrefab이 할당되지 않았습니다!");
            }

            // 유니티 물리 엔진의 트리거 인식 불안정성을 보완하기 위해
            // 코드 레벨에서 반경 내의 벽(BreakableObject)을 직접 찾아 강제로 파괴합니다.
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (Collider col in colliders)
            {
                MushOut.Environment.BreakableObject breakable = col.GetComponent<MushOut.Environment.BreakableObject>();
                if (breakable == null)
                {
                    breakable = col.GetComponentInParent<MushOut.Environment.BreakableObject>();
                }

                if (breakable != null)
                {
                    // 수동으로 파괴 명령 전달
                    breakable.Break(transform.position);
                }
            }

            // 폭탄 파괴
            Destroy(gameObject);
        }
    }
}
