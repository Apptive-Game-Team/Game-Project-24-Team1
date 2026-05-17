using UnityEngine;

namespace MushOut.Combat
{
    /// <summary>
    /// [Rule B] Scriptable Object 기반의 데이터 주도 설계
    /// 무기의 수치를 코드와 분리하여 관리하며, 무기별 사격 대상 레이어를 정의합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "TranquilizerGunData", menuName = "Nexush/Combat/Weapon Data")]
    public class WeaponDataSO : ScriptableObject
    {
        [Header("Tranquilizer Settings")]
        [Tooltip("마취 침 한 발당 누적되는 마취 수치입니다.")]
        public float tranquilizerAmount = 25f;
        
        [Tooltip("최대 사거리입니다.")]
        public float range = 50f;
        
        [Tooltip("발사 간격 (초 단위)입니다.")]
        public float fireRate = 0.5f;

        [Header("Layer Settings")]
        [Tooltip("사격 레이캐스트가 물리적으로 충돌을 감지할 레이어입니다.")]
        public LayerMask targetLayer;
    }
}