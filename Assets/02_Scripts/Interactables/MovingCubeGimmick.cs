using UnityEngine;
using MushOut.Interaction;

namespace MushOut.Interactables
{
    [RequireComponent(typeof(Rigidbody))]
    public class MovingCubeGimmick : MonoBehaviour
    {
        [Header("Data & Settings")]
        [SerializeField] private MovingGimmickSettingsSO gimmickSettings;

        private Rigidbody rb;
        private Vector3 startPosition;
        private Vector3 targetPosition;
        private bool isMovingToTarget = false;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true; // 외부의 물리적 충돌로 밀리지 않도록 설정
            
            startPosition = transform.position;
            if (gimmickSettings != null)
            {
                targetPosition = startPosition + gimmickSettings.targetOffset;
            }
        }

        // 완벽한 디커플링: 버튼의 UnityEvent 인스펙터에서 이 메서드를 호출하도록 연결
        public void ToggleMove()
        {
            isMovingToTarget = !isMovingToTarget;
        }

        private void FixedUpdate()
        {
            if (gimmickSettings == null) return;

            // 물리 연산 분리: 리지드바디를 통한 이동 처리는 항상 FixedUpdate에서 수행
            Vector3 destination = isMovingToTarget ? targetPosition : startPosition;
            Vector3 newPosition = Vector3.MoveTowards(rb.position, destination, gimmickSettings.moveSpeed * Time.fixedDeltaTime);
            
            rb.MovePosition(newPosition);
        }
    }
}
