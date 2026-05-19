using UnityEngine;
using UnityEngine.InputSystem;

namespace MushOut.Player
{
    /// <summary>
    /// 플레이어가 사용할 수 있는 특수 행동(능력)의 상태를 관리하는 컨트롤러입니다.
    /// 숫자키 1, 2, 3, 4를 눌러 상태를 전환합니다.
    /// </summary>
    public enum AbilityState
    {
        Dash,       // 1번: 대시
        Paralyze,   // 2번: 마비 (수면 포자 등)
        Mad,        // 3번: 광분 (적을 화나게 만듦)
        Bomb        // 4번: 폭탄
    }

    public class AbilityController : MonoBehaviour
    {
        [Header("Ability State")]
        [Tooltip("현재 활성화된 특수 행동 상태입니다.")]
        [SerializeField] private AbilityState _currentState = AbilityState.Dash;

        /// <summary>
        /// 외부에서 현재 능력을 확인할 수 있는 프로퍼티입니다.
        /// </summary>
        public AbilityState CurrentState => _currentState;

        private void Update()
        {
            // Input System 키보드 확인
            if (Keyboard.current == null) return;

            // 숫자키 1~4 입력에 따른 상태 전환
            if (Keyboard.current[Key.Digit1].wasPressedThisFrame)
            {
                ChangeState(AbilityState.Dash);
            }
            else if (Keyboard.current[Key.Digit2].wasPressedThisFrame)
            {
                ChangeState(AbilityState.Paralyze);
            }
            else if (Keyboard.current[Key.Digit3].wasPressedThisFrame)
            {
                ChangeState(AbilityState.Mad);
            }
            else if (Keyboard.current[Key.Digit4].wasPressedThisFrame)
            {
                ChangeState(AbilityState.Bomb);
            }
        }

        /// <summary>
        /// 상태를 변경하고 로그를 출력합니다.
        /// </summary>
        public void ChangeState(AbilityState newState)
        {
            if (_currentState == newState) return;

            _currentState = newState;
            Debug.Log($"[AbilityController] 현재 능력이 변경되었습니다: {_currentState}");
        }
    }
}
