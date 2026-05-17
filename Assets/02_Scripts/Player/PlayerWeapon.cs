using System;
using System.Collections;
using UnityEngine;
using MushOut.Interfaces;
using MushOut.Combat;

namespace MushOut.Player
{
    /// <summary>
    /// [Rule D] 싱글톤을 사용하지 않는 이중 레이캐스트 사격 시스템 컴포넌트입니다.
    /// TPS/FPS 시점 전환에 구애받지 않고 화면 중앙을 정확히 조준하여 마취 침을 발사합니다.
    /// </summary>
    public class PlayerWeapon : MonoBehaviour
    {
        [Header("Weapon Settings")]
        [Tooltip("[Rule B] 무기 수치 및 레이어 설정 데이터가 담긴 Scriptable Object입니다.")]
        [SerializeField] private WeaponDataSO weaponData;

        [Tooltip("마취 침이 실제로 발사되는 총구(Muzzle)의 위치입니다.")]
        [SerializeField] private Transform muzzleTransform;

        [Header("Fake Projectile Settings (지연 히트스캔)")]
        [Tooltip("다트 트레이서 (시각적 가짜 투사체) 프리팹 (옵션)")]
        [SerializeField] private GameObject dartTracerPrefab;

        [Tooltip("가짜 투사체의 날아가는 속도")]
        [SerializeField] private float projectileSpeed = 60f;

        [Header("Tag Settings")]
        [Tooltip("물리적으로 충돌한 객체 중, 실제로 피격 처리를 허용할 타겟의 태그입니다.")]
        [SerializeField] private string targetTag = "Enemy";

        /// <summary>
        /// [Rule A] 관찰자 패턴: 사격이 발생했을 때 사운드나 VFX 시스템에 알리기 위한 이벤트입니다.
        /// </summary>
        public event Action OnShoot;

        /// <summary>
        /// [Rule A] 관찰자 패턴: 타겟에 정확히 명중했을 때 명중 정보를 전달하기 위한 이벤트입니다.
        /// </summary>
        public event Action<HitInfo> OnHitTarget;

        private float _lastFireTime;
        private Camera _mainCam;
        private PlayerInputHandler _inputHandler;

        private void Awake()
        {
            // [Rule D] 싱글톤 대신 Camera.main을 통해 메인 카메라를 참조합니다.
            _mainCam = Camera.main;
            _inputHandler = GetComponent<PlayerInputHandler>();

            if (muzzleTransform == null)
            {
                Debug.LogError($"[{name}] Muzzle Transform이 설정되지 않았습니다! 총구 위치를 할당해주세요.");
            }
        }

        private void Update()
        {
            if (_inputHandler != null && _inputHandler.IsFiring)
            {
                FireWeapon();
            }
        }

        /// <summary>
        /// 외부(InputHandler 또는 Controller)에서 사격 입력을 받았을 때 호출하는 메서드입니다.
        /// </summary>
        public void FireWeapon()
        {
            if (weaponData == null || muzzleTransform == null || _mainCam == null)
            {
                return;
            }

            if (Time.time < _lastFireTime + weaponData.fireRate)
            {
                return;
            }

            ExecuteDoubleRaycast();

            _lastFireTime = Time.time;

            // [Rule A] 사격 발생 이벤트를 발생시켜 사운드/이펙트 등이 동작하게 합니다.
            OnShoot?.Invoke();
        }

        /// <summary>
        /// 화면 중앙을 조준점으로 하여 실제 총구에서 사격 판정을 수행하는 이중 레이캐스트 로직입니다.
        /// </summary>
        private void ExecuteDoubleRaycast()
        {
            // 1. 첫 번째 레이: 카메라 시점의 정중앙에서 정면으로 쏘아 실제 발사할 방향(Target Point)을 찾습니다.
            Ray cameraRay = _mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Vector3 targetPoint;

            if (Physics.Raycast(cameraRay, out RaycastHit camHit, weaponData.range, weaponData.targetLayer))
            {
                targetPoint = camHit.point;
            }
            else
            {
                // 타격된 물체가 없다면 사거리 내 최대 지점을 목표 방향으로 설정합니다.
                targetPoint = cameraRay.GetPoint(weaponData.range);
            }

            // 2. 두 번째 단계: 실제 총구 위치에서 목표 지점을 향한 '발사 방향'을 계산합니다.
            Vector3 fireDirection = (targetPoint - muzzleTransform.position).normalized;

            // [버그 수정] 3인칭 시점에서 벽에 완전히 밀착했을 때 카메라 레이캐스트의 충돌 지점(targetPoint)이
            // 총구 위치(muzzleTransform)보다 뒤에 있거나 너무 가까워져 발사 방향이 기형적으로 꺾이는 현상을 방지합니다.
            if (Vector3.Dot(fireDirection, cameraRay.direction) <= 0.1f || (targetPoint - muzzleTransform.position).sqrMagnitude < 2.0f)
            {
                fireDirection = cameraRay.direction;
            }

            // 디버그용: 씬 뷰 발사 방향 궤적 (에디터 전용)
            Debug.DrawRay(muzzleTransform.position, fireDirection * weaponData.range, Color.cyan, 1.0f);

            // 3. 계산된 방향으로 매 프레임 충돌을 검사하며 날아가는 '실제 투사체 코루틴'을 시작합니다.
            StartCoroutine(TrueProjectileRoutine(muzzleTransform.position, fireDirection, weaponData.range));
        }

        /// <summary>
        /// 실제 투사체 방식으로 매 프레임 위치를 이동하며 충돌을 검사하는 코루틴입니다.
        /// 중간에 궤적에 들어온 적도 정상적으로 피격 판정을 받습니다.
        /// </summary>
        private IEnumerator TrueProjectileRoutine(Vector3 startPos, Vector3 direction, float maxDistance)
        {
            float distanceTraveled = 0f;
            Vector3 currentPos = startPos;

            // 시각적 연출을 위한 트레이서 프리팹 생성 (할당된 경우)
            GameObject tracer = null;
            if (dartTracerPrefab != null)
            {
                tracer = Instantiate(dartTracerPrefab, startPos, Quaternion.LookRotation(direction));
            }

            // 최대 사거리(maxDistance)에 도달할 때까지 매 프레임 검사하며 전진
            while (distanceTraveled < maxDistance)
            {
                // 이번 프레임에 이동할 거리 (속도 * 시간)
                float moveStep = projectileSpeed * Time.deltaTime;

                // [핵심] 이동하기 전에, 내 현재 위치에서 이동할 위치 사이에 충돌체가 있는지 레이캐스트로 검사!
                if (Physics.Raycast(currentPos, direction, out RaycastHit hit, moveStep, weaponData.targetLayer))
                {
                    // 무언가에 부딪혔다면! (벽이든, 적이든)
                    if (hit.collider.CompareTag(targetTag))
                    {
                        if (hit.collider.TryGetComponent<IHittable>(out var hittable))
                        {
                            HitInfo hitInfo = new HitInfo
                            {
                                amount = weaponData.tranquilizerAmount,
                                hitPoint = hit.point,
                                normal = hit.normal
                            };

                            // 인터페이스 메서드 호출 (실제 마취 수치 적용)
                            hittable.OnHit(hitInfo);

                            // 명중 이벤트 알림 (UI나 사운드 등에 전달)
                            OnHitTarget?.Invoke(hitInfo);
                        }
                    }

                    // 대상의 종류와 상관없이(벽이든 적이든) 무언가에 부딪혔으므로 트레이서 위치를 충돌 지점으로 맞추고 파괴
                    if (tracer != null)
                    {
                        tracer.transform.position = hit.point;
                        Destroy(tracer);
                    }

                    yield break; // 부딪혔으므로 코루틴 즉시 종료 (투사체 소멸)
                }

                // 충돌이 없으면 실제 위치 이동
                currentPos += direction * moveStep;
                distanceTraveled += moveStep;

                if (tracer != null)
                {
                    tracer.transform.position = currentPos;
                }

                yield return null; // 다음 프레임까지 대기
            }

            // 루프가 끝남 == 사거리를 다 날아가도 부딪히지 않은 경우 파괴
            if (tracer != null)
            {
                Destroy(tracer);
            }
        }
    }
}
