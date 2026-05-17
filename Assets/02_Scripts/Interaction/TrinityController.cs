using UnityEngine;
using UnityEngine.Events;

namespace MushOut.Interaction
{
    /// <summary>
    /// 세 가지 조건(First, Second, Third)의 활성화/비활성화 상태를 관리하는 컨트롤러.
    /// 3개의 발판 등을 밟았을 때 등 복합적인 조건을 체크할 때 유용합니다.
    /// </summary>
    public class TrinityController : MonoBehaviour
    {
        [Header("Conditions (Debug / Read Only)")]
        [SerializeField] private bool conditionFirst;
        [SerializeField] private bool conditionSecond;
        [SerializeField] private bool conditionThird;

        [Header("Settings")]
        [Tooltip("이벤트가 실행된 직후 세 가지 조건을 모두 다시 false로 되돌릴지(재사용 가능하게 할지) 여부")]
        [SerializeField] private bool resetAfterResolve = false;

        [Header("Events")]
        [Tooltip("세 가지 조건이 모두 true가 된 순간 실행될 이벤트")]
        public UnityEvent onAllConditionsMet;

        // 세 가지가 모두 켜져서 이벤트가 실행되었는지 여부를 확인하는 플래그 (중복 실행 방지용)
        private bool _isResolved = false;

        // 외부에서 상태를 읽을 수 있도록 프로퍼티 제공
        public bool ConditionFirst => conditionFirst;
        public bool ConditionSecond => conditionSecond;
        public bool ConditionThird => conditionThird;

        #region First Condition 제어
        public void SetFirstOn()
        {
            conditionFirst = true;
            CheckConditions();
        }

        public void SetFirstOff()
        {
            conditionFirst = false;
        }

        public void ToggleFirst()
        {
            conditionFirst = !conditionFirst;
            CheckConditions();
        }
        #endregion

        #region Second Condition 제어
        public void SetSecondOn()
        {
            conditionSecond = true;
            CheckConditions();
        }

        public void SetSecondOff()
        {
            conditionSecond = false;
        }

        public void ToggleSecond()
        {
            conditionSecond = !conditionSecond;
            CheckConditions();
        }
        #endregion

        #region Third Condition 제어
        public void SetThirdOn()
        {
            conditionThird = true;
            CheckConditions();
        }

        public void SetThirdOff()
        {
            conditionThird = false;
        }

        public void ToggleThird()
        {
            conditionThird = !conditionThird;
            CheckConditions();
        }
        #endregion

        /// <summary>
        /// 세 조건이 모두 참인지 검사하고, 모두 참이면 이벤트를 발생시킵니다.
        /// </summary>
        private void CheckConditions()
        {
            // 이미 한 번 이벤트가 터졌다면 (그리고 다시 실행되는 걸 원치 않는다면) 방어
            if (_isResolved) return;

            if (conditionFirst && conditionSecond && conditionThird)
            {
                _isResolved = true;
                onAllConditionsMet?.Invoke();
                Debug.Log($"[{name}] 모든 트리니티 조건이 달성되었습니다!");

                // 인스펙터 설정에 따라 달성 직후 모든 조건을 초기화
                if (resetAfterResolve)
                {
                    ResetController();
                }
            }
        }

        /// <summary>
        /// 컨트롤러를 완전히 초기화(초기 상태로 되돌리기)할 때 사용
        /// </summary>
        public void ResetController()
        {
            conditionFirst = false;
            conditionSecond = false;
            conditionThird = false;
            _isResolved = false;
        }
    }
}
