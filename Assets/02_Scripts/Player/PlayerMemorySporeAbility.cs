using MushOut.Enemy;
using MushOut.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MushOut.Player
{
    public class PlayerMemorySporeAbility : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private Key useKey = Key.V;

        [Header("Targeting")]
        [SerializeField] private float range = 8f;
        [SerializeField] private float aimAngle = 35f;
        [SerializeField] private float sphereCastRadius = 0.85f;
        [SerializeField] private LayerMask enemyLayerMask = ~0;
        [SerializeField] private Camera targetCamera;

        [Header("Effect")]
        [SerializeField] private GameObject memorySporeModelPrefab;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current[useKey].wasPressedThisFrame) return;
            TryUseMemorySpore();
        }

        public bool TryUseMemorySpore()
        {
            MemorySporeUI ui = MemorySporeUI.Instance;
            if (ui == null)
            {
                ui = FindFirstObjectByType<MemorySporeUI>();
            }

            if (ui == null || !ui.TryUseMemorySpore())
            {
                return false;
            }

            EnemyController target = FindTargetEnemy();
            if (target == null)
            {
                ui.AddMemorySpores(1);
                return false;
            }

            MemorySporeAbsorbEffect.Play(memorySporeModelPrefab, target.transform, transform);
            return true;
        }

        private EnemyController FindTargetEnemy()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            Ray ray = targetCamera != null
                ? targetCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                : new Ray(transform.position + Vector3.up, transform.forward);

            EnemyController castTarget = FindTargetFromCast(ray);
            if (castTarget != null)
            {
                return castTarget;
            }

            return FindTargetInAimCone(ray);
        }

        private EnemyController FindTargetFromCast(Ray ray)
        {
            RaycastHit[] hits = Physics.SphereCastAll(ray, sphereCastRadius, range, enemyLayerMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                EnemyController rayTarget = hit.collider.GetComponentInParent<EnemyController>();
                if (rayTarget != null)
                {
                    return rayTarget;
                }
            }

            return null;
        }

        private EnemyController FindTargetInAimCone(Ray ray)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, range, enemyLayerMask, QueryTriggerInteraction.Ignore);
            EnemyController bestTarget = null;
            float bestScore = float.MaxValue;

            foreach (Collider hit in hits)
            {
                EnemyController enemy = hit.GetComponentInParent<EnemyController>();
                if (enemy == null) continue;

                Vector3 targetPoint = enemy.transform.position + Vector3.up;
                Vector3 toTargetFromCamera = targetPoint - ray.origin;
                float angle = Vector3.Angle(ray.direction, toTargetFromCamera);
                if (angle > aimAngle) continue;

                float distanceFromPlayer = Vector3.Distance(transform.position, enemy.transform.position);
                if (distanceFromPlayer > range) continue;

                float score = angle + distanceFromPlayer * 0.1f;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = enemy;
                }
            }

            return bestTarget;
        }
    }
}
