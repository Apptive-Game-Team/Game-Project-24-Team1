using UnityEngine;
using System.Collections;

namespace MushOut.Interaction
{
    public class ObjectMovingController : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("오브젝트가 변화하는 속도")]
        [SerializeField] private float moveSpeed = 1f;

        [Tooltip("한 번 이동할 때 이동하는 거리")]
        [SerializeField] private float moveDistance = 6f;

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
        public void MovePlusX() { targetPosition.x += moveDistance; }
        public void MoveMinusX() { targetPosition.x -= moveDistance; }

        public void MovePlusY() { targetPosition.y += moveDistance; }
        public void MoveMinusY() { targetPosition.y -= moveDistance; }

        public void MovePlusZ() { targetPosition.z += moveDistance; }
        public void MoveMinusZ() { targetPosition.z -= moveDistance; }
        #endregion

        #region T초 대기 후 원래 위치로 돌아오는 메서드
        public void MovePlusXWaitForT() { StartCoroutine(RoutineMoveWaitForT(Vector3.right * moveDistance)); }
        public void MoveMinusXWaitForT() { StartCoroutine(RoutineMoveWaitForT(Vector3.left * moveDistance)); }

        public void MovePlusYWaitForT() { StartCoroutine(RoutineMoveWaitForT(Vector3.up * moveDistance)); }
        public void MoveMinusYWaitForT() { StartCoroutine(RoutineMoveWaitForT(Vector3.down * moveDistance)); }

        public void MovePlusZWaitForT() { StartCoroutine(RoutineMoveWaitForT(Vector3.forward * moveDistance)); }
        public void MoveMinusZWaitForT() { StartCoroutine(RoutineMoveWaitForT(Vector3.back * moveDistance)); }
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
