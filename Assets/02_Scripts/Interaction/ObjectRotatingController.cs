using UnityEngine;
using System.Collections;

namespace MushOut.Interaction
{
    public class ObjectRotatingController : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("오브젝트가 회전하는 속도 (초당 각도)")]
        [SerializeField] private float rotateSpeed = 90f;

        [Tooltip("한 번 회전할 때 돌아가는 각도")]
        [SerializeField] private float rotateAngle = 90f;

        [Tooltip("목표 각도 도달 후 대기할 시간 (초). 기본값은 2분(120초)입니다.")]
        [SerializeField] private float waitTime = 120f;

        [Tooltip("회전을 시작할 때 서서히 가속할지 여부")]
        [SerializeField] private bool useAcceleration = false;

        [Tooltip("최대 속도에 도달하기까지 걸리는 시간 (초)")]
        [SerializeField] private float accelerationTime = 2f;

        // 내부적으로 회전해야 할 목표 각도
        private Quaternion targetRotation;
        private float currentSpeed = 0f;

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
                if (useAcceleration && accelerationTime > 0f)
                {
                    currentSpeed = Mathf.MoveTowards(currentSpeed, rotateSpeed, (rotateSpeed / accelerationTime) * Time.deltaTime);
                }
                else
                {
                    currentSpeed = rotateSpeed;
                }

                transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targetRotation, currentSpeed * Time.deltaTime);
            }
            else
            {
                currentSpeed = 0f;
            }
        }

        #region 기본 회전 메서드 (돌아오지 않음)
        public void RotatePlusX() { targetRotation *= Quaternion.Euler(rotateAngle, 0, 0); }
        public void RotateMinusX() { targetRotation *= Quaternion.Euler(-rotateAngle, 0, 0); }

        public void RotatePlusY() { targetRotation *= Quaternion.Euler(0, rotateAngle, 0); }
        public void RotateMinusY() { targetRotation *= Quaternion.Euler(0, -rotateAngle, 0); }

        public void RotatePlusZ() { targetRotation *= Quaternion.Euler(0, 0, rotateAngle); }
        public void RotateMinusZ() { targetRotation *= Quaternion.Euler(0, 0, -rotateAngle); }
        #endregion

        #region T초 대기 후 원래 회전으로 돌아오는 메서드
        public void RotatePlusXWaitForT() { StartCoroutine(RoutineRotateWaitForT(Quaternion.Euler(rotateAngle, 0, 0))); }
        public void RotateMinusXWaitForT() { StartCoroutine(RoutineRotateWaitForT(Quaternion.Euler(-rotateAngle, 0, 0))); }

        public void RotatePlusYWaitForT() { StartCoroutine(RoutineRotateWaitForT(Quaternion.Euler(0, rotateAngle, 0))); }
        public void RotateMinusYWaitForT() { StartCoroutine(RoutineRotateWaitForT(Quaternion.Euler(0, -rotateAngle, 0))); }

        public void RotatePlusZWaitForT() { StartCoroutine(RoutineRotateWaitForT(Quaternion.Euler(0, 0, rotateAngle))); }
        public void RotateMinusZWaitForT() { StartCoroutine(RoutineRotateWaitForT(Quaternion.Euler(0, 0, -rotateAngle))); }
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
