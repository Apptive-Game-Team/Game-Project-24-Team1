using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace MushOut.Enemy
{
    /// <summary>
    /// 적이 Attacking 상태일 때 작동하는 원거리(발사체) 공격 로직입니다.
    /// EnemyAttacking(돌진)과 발동 조건은 동일하지만, 이 스크립트는 원거리 적 전용입니다.
    /// 동작:
    ///   1. Attacking 상태 진입 시 LockState()로 외부 상태 전환 차단 (Stunned/Dead 제외)
    ///   2. _preFireDelay 초 동안 플레이어를 바라보며 조준
    ///   3. _firePoints(머리 오브젝트)에서 각각 플레이어 방향으로 구체 발사체를 발사
    ///   4. 발사 후 _stateAfterAttack 상태로 복귀
    /// </summary>
    [RequireComponent(typeof(EnemyController), typeof(NavMeshAgent))]
    public class EnemyRangedAttacking : MonoBehaviour
    {
        [Header("Fire Points")]
        [Tooltip("발사 위치가 되는 머리 오브젝트들입니다. 머리가 두 개라면 두 개 모두 등록하세요.")]
        [SerializeField] private Transform[] _firePoints;

        [Header("Projectile Settings")]
        [Tooltip("사용할 발사체 프리팹입니다. 지정하면 이 프리팹을 Instantiate하여 사용합니다.\n비워두면 아래 Projectile Settings로 Sphere를 자동 생성합니다.")]
        [SerializeField] private GameObject _projectilePrefab;

        [Tooltip("발사체(구체)의 반지름 (0.2 → scale 0.4 구체) — 프리팹 미지정 시 사용")]
        [SerializeField] private float _projectileRadius = 0.2f;

        [Tooltip("발사체의 이동 속도 (m/s)")]
        [SerializeField] private float _projectileSpeed = 15f;

        [Tooltip("발사체가 자동으로 소멸하는 시간 (초)")]
        [SerializeField] private float _projectileLifetime = 5f;

        [Tooltip("발사체에 적용할 머티리얼 (없으면 _projectileColor로 생성)")]
        [SerializeField] private Material _projectileMaterial;

        [Tooltip("머티리얼이 없을 때 발사체에 사용할 색")]
        [SerializeField] private Color _projectileColor = Color.red;

        [Header("Attack Timing")]
        [Tooltip("Attacking 상태 진입 후 발사 전 대기 시간 (초). 조준 모션 등에 활용.")]
        [SerializeField] private float _preFireDelay = 1.5f;

        [Tooltip("대기 중 플레이어를 향해 회전하는 최대 각도 속도 (도/초). 360 = 즉시 회전.")]
        [SerializeField] private float _lookRotateSpeed = 180f;

        [Tooltip("공격 완료 후 복귀할 상태")]
        [SerializeField] private EnemyController.State _stateAfterAttack = EnemyController.State.Idle;

        // ── 내부 참조 ──────────────────────────────────────────────────
        private EnemyController _enemyController;
        private NavMeshAgent _agent;

        /// <summary>AttackRoutine 중복 실행 방지 플래그</summary>
        private bool _isRunning = false;

        /// <summary>플레이어 Transform 캐시 (EnemySight와 동일한 방식으로 획득)</summary>
        private Transform _playerTransform;

        // ── 생명주기 ───────────────────────────────────────────────────

        private void Awake()
        {
            _enemyController = GetComponent<EnemyController>();
            _agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            CachePlayerTransform();
        }

        private void Update()
        {
            if (_enemyController == null) return;

            // Attacking 상태 진입 시 한 번만 루틴 시작
            if (_enemyController.CurrentState == EnemyController.State.Attacking && !_isRunning)
            {
                StartCoroutine(AttackRoutine());
            }
        }

        // ── 공격 루틴 ──────────────────────────────────────────────────

        private IEnumerator AttackRoutine()
        {
            _isRunning = true;

            // 0. 이전 이동 명령 취소
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.velocity = Vector3.zero;
                _agent.ResetPath();
            }

            // 1. 상태 잠금
            _enemyController.LockState();

            // 2. 플레이어 Transform 재확인
            if (_playerTransform == null)
            {
                CachePlayerTransform();
            }

            // 3. 발사 전 대기: 플레이어를 바라보며 조준
            if (_preFireDelay > 0f)
            {
                float elapsed = 0f;
                while (elapsed < _preFireDelay)
                {
                    if (_enemyController.CurrentState != EnemyController.State.Attacking)
                        break;

                    RotateTowardPlayer();

                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            // 대기 중 Stunned/Dead 강제 전환 감지
            if (_enemyController.CurrentState != EnemyController.State.Attacking)
            {
                _enemyController.UnlockState();
                _isRunning = false;
                yield break;
            }

            // 4. 발사
            FireProjectiles();

            // 5. 잠금 해제 및 상태 복귀
            _enemyController.UnlockState();
            _isRunning = false;

            if (_enemyController.CurrentState == EnemyController.State.Attacking)
            {
                _enemyController.ChangeState(_stateAfterAttack);
            }
        }

        // ── 헬퍼 메서드 ────────────────────────────────────────────────

        /// <summary>플레이어 방향으로 수평만 회전합니다.</summary>
        private void RotateTowardPlayer()
        {
            if (_playerTransform == null) return;

            Vector3 toPlayer = _playerTransform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.001f) return;

            Quaternion targetRot = Quaternion.LookRotation(toPlayer);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                _lookRotateSpeed * Time.deltaTime
            );
        }

        /// <summary>
        /// 등록된 모든 firePoint에서 플레이어 현재 위치를 향해 구체 발사체를 생성합니다.
        /// </summary>
        private void FireProjectiles()
        {
            if (_playerTransform == null)
            {
                Debug.LogWarning("[EnemyRangedAttacking] 플레이어 Transform을 찾을 수 없어 발사를 중단합니다.");
                return;
            }

            if (_firePoints == null || _firePoints.Length == 0)
            {
                Debug.LogWarning("[EnemyRangedAttacking] Fire Points가 지정되지 않았습니다. Inspector에서 머리 오브젝트를 등록하세요.");
                return;
            }

            // 플레이어 허리 높이(1m 오프셋)를 조준점으로 사용
            Vector3 targetPos = _playerTransform.position + Vector3.up * 1f;

            foreach (Transform firePoint in _firePoints)
            {
                if (firePoint == null) continue;

                Vector3 origin = firePoint.position;
                Vector3 direction = (targetPos - origin).normalized;

                SpawnProjectile(origin, direction);
            }
        }

        /// <summary>
        /// 지정한 위치·방향으로 발사체를 생성합니다.
        /// _projectilePrefab이 지정된 경우 해당 프리팹을 Instantiate하고,
        /// 없는 경우 Projectile Settings 값으로 Sphere를 자동 생성합니다.
        /// </summary>
        private void SpawnProjectile(Vector3 position, Vector3 direction)
        {
            GameObject projectile;

            if (_projectilePrefab != null)
            {
                // ── 프리팹 경로 ──────────────────────────────────────────
                projectile = Instantiate(_projectilePrefab, position, Quaternion.LookRotation(direction));
            }
            else
            {
                // ── Sphere 폴백 경로 ─────────────────────────────────────
                projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                projectile.name = "RangedProjectile";
                projectile.transform.position = position;
                projectile.transform.rotation = Quaternion.LookRotation(direction);
                projectile.transform.localScale = Vector3.one * (_projectileRadius * 2f);

                // 머티리얼/색상 적용
                Renderer rend = projectile.GetComponent<Renderer>();
                if (rend != null)
                {
                    if (_projectileMaterial != null)
                        rend.material = _projectileMaterial;
                    else
                        rend.material.color = _projectileColor;
                }
            }

            // ── 공통: Rigidbody로 속도 주입 ────────────────────────────
            // 프리팹에 Rigidbody가 없으면 자동 추가
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb == null) rb = projectile.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = direction * _projectileSpeed;

            // ── 공통: 이 Enemy의 콜라이더와 충돌 무시 ──────────────────
            Collider projectileCol = projectile.GetComponent<Collider>();
            if (projectileCol != null)
            {
                foreach (Collider col in GetComponentsInChildren<Collider>())
                {
                    Physics.IgnoreCollision(projectileCol, col, true);
                }
            }

            // ── 공통: RangedProjectileHit 부착 (Player 피격 및 벽 충돌 처리) ──
            // 프리팹에 이미 붙어 있으면 중복 추가하지 않음
            if (projectile.GetComponent<RangedProjectileHit>() == null)
            {
                projectile.AddComponent<RangedProjectileHit>();
            }

            Destroy(projectile, _projectileLifetime);
        }

        /// <summary>
        /// GameManager → FindGameObjectWithTag 순서로 플레이어 Transform을 캐싱합니다.
        /// EnemySight와 동일한 우선순위를 사용합니다.
        /// </summary>
        private void CachePlayerTransform()
        {
            if (MushOut.Core.GameManager.Instance != null)
            {
                _playerTransform = MushOut.Core.GameManager.Instance.PlayerTransform;
            }

            if (_playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj == null) playerObj = GameObject.Find("Player");
                if (playerObj != null) _playerTransform = playerObj.transform;
            }
        }
    }
}
