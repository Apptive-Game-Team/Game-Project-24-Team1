using UnityEngine;
using System.Collections;

namespace MushOut.Interaction
{
    public class ObjectRotatingController : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("오브젝트가 회전하는 속도 (초당 각도)")]
        [SerializeField] private float rotateSpeed = 90f;

        [Tooltip("목표 각도 도달 후 대기할 시간 (초). 기본값은 2분(120초)입니다.")]
        [SerializeField] private float waitTime = 120f;

        // 내부적으로 회전해야 할 목표 각도
        private Quaternion targetRotation;

        private void Start()
        {
            // 시작 시 현재 회전을 목표 회전으로 설정 (로컬 회전 기준)
            targetRotation = transform.localRotation;
        }

        private void Update()
        {
            // 현재 회전과 목표 회전이 다르면 부드럽게 회전
            if (Quaternion.Angle(transform.localRotation, targetRotation) > 0.01f)
            {
                transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targetRotation, rotateSpeed * Time.deltaTime);
            }
        }

        #region 기본 회전 메서드 (돌아오지 않음)
        public void RotatePlusX(float angle) { targetRotation *= Quaternion.Euler(angle, 0, 0); }
        public void RotateMinusX(float angle) { targetRotation *= Quaternion.Euler(-angle, 0, 0); }

        public void RotatePlusY(float angle) { targetRotation *= Quaternion.Euler(0, angle, 0); }
        public void RotateMinusY(float angle) { targetRotation *= Quaternion.Euler(0, -angle, 0); }

        public void RotatePlusZ(float angle) { targetRotation *= Quaternion.Euler(0, 0, angle); }
        public void RotateMinusZ(float angle) { targetRotation *= Quaternion.Euler(0, 0, -angle); }
        #endregion

        #region T초 대기 후 원래 회전으로 돌아오는 메서드
        public void RotatePlusXWaitForT(float angle) { StartCoroutine(RoutineRotateWaitForT(Quaternion.Euler(angle, 0, 0))); }
        public void RotateMinusXWaitForT(float angle) { StartCoroutine(RoutineRotateWaitForT(Quaternion.Euler(-angle, 0, 0))); }

        public void RotatePlusYWaitForT(float angle) { StartCoroutine(RoutineRotateWaitForT(Quaternion.Euler(0, angle, 0))); }
        public void RotateMinusYWaitForT(float angle) { StartCoroutine(RoutineRotateWaitForT(Quaternion.Euler(0, -angle, 0))); }

        public void RotatePlusZWaitForT(float angle) { StartCoroutine(RoutineRotateWaitForT(Quaternion.Euler(0, 0, angle))); }
        public void RotateMinusZWaitForT(float angle) { StartCoroutine(RoutineRotateWaitForT(Quaternion.Euler(0, 0, -angle))); }
        #endregion

        // 공통 대기 코루틴
        private IEnumerator RoutineRotateWaitForT(Quaternion offset)
        {
            // 목표 회전으로 변경 (로컬 축 기준 회전 추가)
            targetRotation *= offset;
            
            // 목표 각도에 도달할 때까지 대기
            while (Quaternion.Angle(transform.localRotation, targetRotation) > 0.01f)
            {
                yield return null;
            }
            
            // 설정된 시간(waitTime) 동안 대기
            yield return new WaitForSeconds(waitTime);
            
            // 원래 회전으로 원상복구 (역회전을 곱해줌)
            targetRotation *= Quaternion.Inverse(offset);
        }
    }
}
