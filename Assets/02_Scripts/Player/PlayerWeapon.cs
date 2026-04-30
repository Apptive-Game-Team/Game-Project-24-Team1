using System;
using UnityEngine;
using Nexush.Interfaces;
using Nexush.Combat;

namespace Nexush.Player
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

        private void Awake()
        {
            // [Rule D] 싱글톤 대신 Camera.main을 통해 메인 카메라를 참조합니다.
            _mainCam = Camera.main;

            if (muzzleTransform == null)
            {
                Debug.LogError($"[{name}] Muzzle Transform이 설정되지 않았습니다! 총구 위치를 할당해주세요.");
            }
        }

        /// <summary>
        /// 외부(InputHandler 또는 Controller)에서 사격 입력을 받았을 때 호출하는 메서드입니다.
        /// </summary>
        public void FireWeapon()
        {
            if (weaponData == null) return;
            if (Time.time < _lastFireTime + weaponData.fireRate) return;

            Debug.Log($"[{name}] FireWeapon() 호출됨!"); // 호출 확인 로그
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
            // 1. 첫 번째 레이: 카메라 시점의 정중앙에서 정면으로 쏘아 실제 목표 지점(Target Point)을 찾습니다.
            // Viewport (0.5, 0.5)는 해상도에 상관없이 항상 화면 정중앙을 가리킵니다.
            Ray cameraRay = _mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Vector3 targetPoint;

            // [복구] WeaponDataSO의 targetLayer를 사용하여 물리적 충돌 대상을 필터링합니다.
            if (Physics.Raycast(cameraRay, out RaycastHit camHit, weaponData.range, weaponData.targetLayer))
            {
                targetPoint = camHit.point;
            }
            else
            {
                // 타격된 물체가 없다면 사거리 내 최대 지점을 목표로 설정합니다.
                targetPoint = cameraRay.GetPoint(weaponData.range);
            }

            // 2. 두 번째 레이: 실제 총구 위치에서 방금 찾은 targetPoint를 향해 발사하여 실제 피격 판정을 수행합니다.
            // 이를 통해 카메라와 총구 사이의 시차(Parallax) 문제를 해결합니다.
            Vector3 fireDirection = (targetPoint - muzzleTransform.position).normalized;
            float distanceToTarget = Vector3.Distance(muzzleTransform.position, targetPoint);

            // 총구에서 목표 지점 사이의 장애물을 체크합니다.
            // Physics.Raycast의 거리를 distanceToTarget으로 제한하여 목표 지점 너머의 물체를 체크하지 않도록 합니다.
            if (Physics.Raycast(muzzleTransform.position, fireDirection, out RaycastHit muzzleHit, distanceToTarget, weaponData.targetLayer))
            {
                // 장애물이나 타겟에 맞았다면 targetPoint를 실제 충돌 지점으로 업데이트합니다.
                // 이렇게 하면 시각적인 라인(DrawLine)이 장애물에서 멈추게 됩니다.
                targetPoint = muzzleHit.point;

                // 설정된 태그와 일치하는지 확인하여 유효한 타겟인지 검증합니다.
                if (muzzleHit.collider.CompareTag(targetTag))
                {
                    // [Rule C] 인터페이스 활용: TryGetComponent를 통해 대상과의 결합도를 제거합니다.
                    if (muzzleHit.collider.TryGetComponent<IHittable>(out var hittable))
                    {
                        Debug.Log($"[{name}] 명중! 대상: {muzzleHit.collider.name}"); // 명중 확인 로그
                        HitInfo hitInfo = new HitInfo
                        {
                            amount = weaponData.tranquilizerAmount,
                            hitPoint = muzzleHit.point,
                            normal = muzzleHit.normal
                        };

                        // 인터페이스 메서드 호출
                        hittable.OnHit(hitInfo);
                        
                        // [Rule A] 명중 이벤트 알림
                        OnHitTarget?.Invoke(hitInfo);
                    }
                }
            }

            // 디버그용: 씬 뷰에서 실제 발사 궤적을 확인하기 위한 라인 드로잉
            // 이제 targetPoint가 충돌 지점으로 업데이트되었으므로 장애물을 뚫지 않습니다.
            Debug.DrawLine(muzzleTransform.position, targetPoint, Color.cyan, 1.0f);
        }
    }
}
