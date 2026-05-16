using UnityEngine;

namespace MushOut.Interfaces
{
    /// <summary>
    /// [Rule C] 피격 정보를 담는 구조체
    /// </summary>
    public struct HitInfo
    {
        public float amount;      // 마취 수치
        public Vector3 hitPoint;  // 피격 위치
        public Vector3 normal;    // 피격 지점의 법선 데이터
    }

    /// <summary>
    /// [Rule C] 피격 가능한 모든 객체(적, 기믹 등)가 구현해야 하는 인터페이스
    /// </summary>
    public interface IHittable
    {
        void OnHit(HitInfo hitInfo);
    }
}