using MushOut.Core;
using UnityEngine;

namespace MushOut.Player
{
    /// <summary>
    /// 플레이어가 적에게 닿았는지 확인해서 GameManager에 게임오버를 알려주는 컴포넌트입니다.
    /// 다시시도할 때 처음 시작 위치로 돌아갈 수 있도록 플레이어의 시작 위치도 기억합니다.
    /// </summary>
    public class PlayerEnemyCollisionHandler : MonoBehaviour
    {
        [SerializeField] private float enemyCheckRadius = 0.75f;

        private const int MaxOverlapResults = 16;
        private static readonly Collider[] OverlapResults = new Collider[MaxOverlapResults];

        private static bool _hasInitialSpawn;
        private static Vector3 _initialSpawnPosition;
        private static Quaternion _initialSpawnRotation;

        private void Awake()
        {
            _initialSpawnPosition = transform.position;
            _initialSpawnRotation = transform.rotation;
            _hasInitialSpawn = true;
        }

        private void Update()
        {
            CheckEnemyOverlap();
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            CheckEnemyCollision(hit.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            CheckEnemyCollision(collision.gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            CheckEnemyCollision(other.gameObject);
        }

        public static bool TryGetInitialSpawn(out Vector3 position, out Quaternion rotation)
        {
            position = _initialSpawnPosition;
            rotation = _initialSpawnRotation;
            return _hasInitialSpawn;
        }

        private void CheckEnemyOverlap()
        {
            if (GameManager.Instance == null || GameManager.Instance.CrashedByEnemy)
                return;

            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                enemyCheckRadius,
                OverlapResults,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                Collider hit = OverlapResults[i];
                if (hit != null && hit.gameObject != gameObject)
                {
                    CheckEnemyCollision(hit.gameObject);
                }
            }
        }

        private void CheckEnemyCollision(GameObject otherObject)
        {
            if (otherObject == null || !IsPlayerObject() || !IsEnemyObject(otherObject))
                return;

            // 적이 기절(Stunned) 상태라면 게임오버 처리를 하지 않음
            var enemyController = otherObject.GetComponentInParent<MushOut.Enemy.EnemyController>();
            if (enemyController != null && enemyController.CurrentState == MushOut.Enemy.EnemyController.State.Stunned)
            {
                return;
            }

            Debug.Log($"[PlayerEnemyCollisionHandler] Enemy collision detected: {otherObject.name} (tag={otherObject.tag}, layer={LayerMask.LayerToName(otherObject.layer)})");
            GameManager.Instance.CrashedByEnemy = true;
        }

        private bool IsPlayerObject()
        {
            return CompareTag("Player") && gameObject.layer == LayerMask.NameToLayer("Player");
        }

        private static bool IsEnemyObject(GameObject otherObject)
        {
            int layer = otherObject.layer;
            return otherObject.CompareTag("Enemy")
                && (layer == LayerMask.NameToLayer("Enemy") || layer == LayerMask.NameToLayer("Enemy(Heavy)"));
        }
    }
}
