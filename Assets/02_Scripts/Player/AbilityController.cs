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
        Nothing,    // 1번: 아무 능력도 선택하지 않음
        Paralyze,   // 2번: 마비 (수면 포자 등)
        Mad,        // 3번: 광분 (적을 화나게 만듦)
        Bomb        // 4번: 폭탄
    }

    /// <summary>
    /// 플레이어의 능력 상태와 자원을 관리하는 클래스입니다.
    /// </summary>
    public class AbilityController : MonoBehaviour
    {
        [Header("능력 상태")]
        [Tooltip("현재 활성화된 특수 행동 상태입니다.")]
        [SerializeField] private AbilityState _currentState = AbilityState.Nothing;

        [Header("능력 자원")]
        [Tooltip("최대 대시 가능 횟수입니다.")]
        [SerializeField] private int _maxDashCount = 1;

        [Tooltip("현재 대시 가능 횟수입니다.")]
        [SerializeField] private int _dashCount = 1;

        [Tooltip("대시 충전 소요 시간(초)입니다.")]
        [SerializeField] private float _dashChargeTime = 3f;

        private float _dashChargeTimer = 0f;
        
        [Tooltip("수면 포자 보유 개수입니다.")]
        [SerializeField] private int _sleepFungus = 3;
        
        [Tooltip("광분 포자 보유 개수입니다.")]
        [SerializeField] private int _aggroFungus = 3;
        
        [Tooltip("폭탄 포자 보유 개수입니다.")]
        [SerializeField] private int _bombFungus = 3;

        [Header("능력 해금")]
        [Tooltip("마비 능력 해금 여부입니다.")]
        [SerializeField] private bool _paralyzeUnlocked = false;
        
        [Tooltip("광분 능력 해금 여부입니다.")]
        [SerializeField] private bool _madUnlocked = false;
        
        [Tooltip("폭탄 능력 해금 여부입니다.")]
        [SerializeField] private bool _bombUnlocked = false;

        /// <summary>
        /// 외부에서 현재 능력을 확인할 수 있는 프로퍼티입니다.
        /// </summary>
        public AbilityState CurrentState => _currentState;

        /// <summary>
        /// 대시 사용 가능 횟수에 접근하는 프로퍼티입니다.
        /// </summary>
        public int DashCount 
        {
            get => _dashCount;
            set => _dashCount = value;
        }

        /// <summary>
        /// 수면 포자 보유 개수에 접근하는 프로퍼티입니다.
        /// </summary>
        public int SleepFungus 
        {
            get => _sleepFungus;
            set => _sleepFungus = value;
        }

        /// <summary>
        /// 광분 포자 보유 개수에 접근하는 프로퍼티입니다.
        /// </summary>
        public int AggroFungus 
        {
            get => _aggroFungus;
            set => _aggroFungus = value;
        }

        /// <summary>
        /// 폭탄 포자 보유 개수에 접근하는 프로퍼티입니다.
        /// </summary>
        public int BombFungus 
        {
            get => _bombFungus;
            set => _bombFungus = value;
        }

        /// <summary>
        /// 마비 능력 해금 여부를 확인하는 프로퍼티입니다.
        /// </summary>
        public bool ParalyzeUnlocked => _paralyzeUnlocked;

        /// <summary>
        /// 광분 능력 해금 여부를 확인하는 프로퍼티입니다.
        /// </summary>
        public bool MadUnlocked => _madUnlocked;

        /// <summary>
        /// 폭탄 능력 해금 여부를 확인하는 프로퍼티입니다.
        /// </summary>
        public bool BombUnlocked => _bombUnlocked;

        private PlayerInputHandler _input;

        private void Awake()
        {
            _input = GetComponent<PlayerInputHandler>();
        }

        private void Update()
        {
            // 대시 충전 로직
            if (_dashCount < _maxDashCount)
            {
                _dashChargeTimer += Time.deltaTime;
                if (_dashChargeTimer >= _dashChargeTime)
                {
                    _dashCount++;
                    _dashChargeTimer = 0f;
                }
            }
            else
            {
                _dashChargeTimer = 0f;
            }

            if (_input == null) return;

            // PlayerInputHandler를 통한 상태 전환
            if (_input.IsAbility1)
            {
                ChangeState(AbilityState.Nothing);
            }
            else if (_input.IsAbility2 && _paralyzeUnlocked)
            {
                ChangeState(AbilityState.Paralyze);
            }
            else if (_input.IsAbility3 && _madUnlocked)
            {
                ChangeState(AbilityState.Mad);
            }
            else if (_input.IsAbility4 && _bombUnlocked)
            {
                ChangeState(AbilityState.Bomb);
            }
        }

        /// <summary>
        /// 상태를 변경하고 로그를 출력합니다.
        /// </summary>
        /// <param name="newState">새로 변경할 능력 상태</param>
        public void ChangeState(AbilityState newState)
        {
            if (_currentState == newState) return;

            _currentState = newState;
            Debug.Log($"[AbilityController] 현재 능력이 변경되었습니다: {_currentState}");
        }

        /// <summary>
        /// 대시 능력을 사용합니다. (자원 소모)
        /// </summary>
        public void UseDash() 
        {
            if (_dashCount > 0)
            {
                _dashCount -= 1;
            }
        }

        /// <summary>
        /// 마비 능력을 해금합니다.
        /// </summary>
        public void UnlockParalyze() 
        {
            _paralyzeUnlocked = true;
        }

        /// <summary>
        /// 마비 능력을 사용합니다. (자원 소모)
        /// </summary>
        public void UseParalyze() 
        {
            _sleepFungus -= 1;
        }

        /// <summary>
        /// 광분 능력을 해금합니다.
        /// </summary>
        public void UnlockMad() 
        {
            _madUnlocked = true;
        }

        /// <summary>
        /// 광분 능력을 사용합니다. (자원 소모)
        /// </summary>
        public void UseMad() 
        {
            _aggroFungus -= 1;
        }

        /// <summary>
        /// 폭탄 능력을 해금합니다.
        /// </summary>
        public void UnlockBomb() 
        {
            _bombUnlocked = true;
        }

        /// <summary>
        /// 폭탄 능력을 사용합니다. (자원 소모)
        /// </summary>
        public void UseBomb() 
        {
            _bombFungus -= 1;
        }

        /// <summary>
        /// 해당 능력 상태의 현재 남은 자원 개수를 반환합니다.
        /// </summary>
        public int GetResourceCount(AbilityState state)
        {
            switch (state)
            {
                case AbilityState.Nothing:
                    return _dashCount;
                case AbilityState.Paralyze:
                    return _sleepFungus;
                case AbilityState.Mad:
                    return _aggroFungus;
                case AbilityState.Bomb:
                    return _bombFungus;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// 해당 능력이 해금되었는지 여부를 반환합니다.
        /// </summary>
        public bool IsUnlocked(AbilityState state)
        {
            switch (state)
            {
                case AbilityState.Nothing:
                    return true;
                case AbilityState.Paralyze:
                    return _paralyzeUnlocked;
                case AbilityState.Mad:
                    return _madUnlocked;
                case AbilityState.Bomb:
                    return _bombUnlocked;
                default:
                    return false;
            }
        }
    }
}
