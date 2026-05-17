using UnityEngine;
using System.Collections;

namespace MushOut.Interaction
{
    public class ObjectMovingController : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("오브젝트가 변화하는 속도")]
        [SerializeField] private float moveSpeed = 1f;

        [Tooltip("목표 위치 도달 후 대기할 시간 (초). 기본값은 2분(120초)입니다.")]
        [SerializeField] private float waitTime = 120f;

        // 내부적으로 이동해야 할 목표 좌표
        private Vector3 targetPosition;

        private void Start()
        {
            // 시작 시 현재 좌표를 목표 좌표로 설정
            targetPosition = transform.position;
        }

        private void Update()
        {
            // 현재 좌표와 목표 좌표가 다르면 부드럽게 이동
            if (Vector3.Distance(transform.position, targetPosition) > 0.001f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            }
        }

        #region 기본 이동 메서드 (돌아오지 않음)
        public void MovePlusX(float distance) { targetPosition.x += distance; }
        public void MoveMinusX(float distance) { targetPosition.x -= distance; }

        public void MovePlusY(float distance) { targetPosition.y += distance; }
        public void MoveMinusY(float distance) { targetPosition.y -= distance; }

        public void MovePlusZ(float distance) { targetPosition.z += distance; }
        public void MoveMinusZ(float distance) { targetPosition.z -= distance; }
        #endregion

        #region T초 대기 후 원래 위치로 돌아오는 메서드
        public void MovePlusXWaitForT(float distance) { StartCoroutine(RoutineMoveWaitForT(Vector3.right * distance)); }
        public void MoveMinusXWaitForT(float distance) { StartCoroutine(RoutineMoveWaitForT(Vector3.left * distance)); }

        public void MovePlusYWaitForT(float distance) { StartCoroutine(RoutineMoveWaitForT(Vector3.up * distance)); }
        public void MoveMinusYWaitForT(float distance) { StartCoroutine(RoutineMoveWaitForT(Vector3.down * distance)); }

        public void MovePlusZWaitForT(float distance) { StartCoroutine(RoutineMoveWaitForT(Vector3.forward * distance)); }
        public void MoveMinusZWaitForT(float distance) { StartCoroutine(RoutineMoveWaitForT(Vector3.back * distance)); }
        #endregion

        // 공통 대기 코루틴
        private IEnumerator RoutineMoveWaitForT(Vector3 offset)
        {
            // 목표 위치로 이동
            targetPosition += offset;
            
            // 목표 위치에 도달할 때까지 대기
            while (Vector3.Distance(transform.position, targetPosition) > 0.001f)
            {
                yield return null;
            }
            
            // 설정된 시간(waitTime) 동안 대기
            yield return new WaitForSeconds(waitTime);
            
            // 원래 위치로 원상복구 (더했던 offset을 다시 뺌)
            targetPosition -= offset;
        }
    }
}
