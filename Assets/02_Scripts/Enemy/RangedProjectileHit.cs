using UnityEngine;
using MushOut.Interfaces;

namespace MushOut.Enemy
{
    /// <summary>
    /// EnemyRangedAttacking이 발사하는 발사체에 부착되는 충돌 처리 컴포넌트입니다.
    /// - _playerLayer 레이어 오브젝트에 닿으면 IHittable.OnHit을 호출한 뒤 즉시 파괴됩니다.
    /// - _wallLayers 레이어 오브젝트에 닿으면 즉시 파괴됩니다.
    ///   (_wallLayers 기본값: "Box", "Wall" 레이어)
    /// </summary>
    public class RangedProjectileHit : MonoBehaviour
    {
        [Tooltip("플레이어로 판정할 레이어 마스크입니다.")]
        [SerializeField] private LayerMask _playerLayer;

        [Tooltip("벽으로 판정할 레이어 마스크입니다. (기본값: Box, Wall)")]
        [SerializeField] private LayerMask _wallLayers;

        private void Awake()
        {
            // _playerLayer 미설정 시 "Player" 레이어를 기본값으로 설정
            if (_playerLayer.value == 0)
            {
                int playerMask = LayerMask.GetMask("Player");
                if (playerMask != 0)
                    _playerLayer = playerMask;
            }

            // _wallLayers 미설정 시 "Box", "Wall" 레이어를 기본값으로 설정
            if (_wallLayers.value == 0)
            {
                int wallMask = LayerMask.GetMask("Box", "Wall");
                if (wallMask != 0)
                    _wallLayers = wallMask;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleHit(collision.collider);
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleHit(other);
        }

        /// <summary>
        /// 충돌 대상 레이어에 따라 피격 처리 후 발사체를 파괴합니다.
        /// </summary>
        private void HandleHit(Collider other)
        {
            int hitLayer = 1 << other.gameObject.layer;

            // ── Player 레이어: IHittable.OnHit 호출 후 파괴 ──────────────
            if (_playerLayer.value != 0 && (_playerLayer.value & hitLayer) != 0)
            {
                IHittable hittable = other.GetComponentInParent<IHittable>();
                if (hittable != null)
                {
                    HitInfo info = new HitInfo
                    {
                        hitPoint = transform.position,
                        normal   = (transform.position - other.ClosestPoint(transform.position)).normalized,
                        amount   = 0f   // 원거리 공격은 amount를 사용하지 않음
                    };
                    hittable.OnHit(info);
                }

                Destroy(gameObject);
                return;
            }

            // ── Wall 레이어: 즉시 파괴 ──────────────────────────────────
            if (_wallLayers.value != 0 && (_wallLayers.value & hitLayer) != 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
