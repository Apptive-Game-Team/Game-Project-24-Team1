using UnityEngine;

namespace MushOut.Interaction
{
    [CreateAssetMenu(fileName = "MovingGimmickSettings", menuName = "MushOut/Data/MovingGimmickSettings")]
    public class MovingGimmickSettingsSO : ScriptableObject
    {
        [Tooltip("작동 시 이동할 목표 오프셋 (로컬/월드 기준)")]
        public Vector3 targetOffset = new Vector3(0, 3f, 0);
        
        [Tooltip("목표 지점까지의 이동 속도")]
        public float moveSpeed = 2f;
    }
}
