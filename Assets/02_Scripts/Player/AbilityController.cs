using UnityEngine;
using UnityEngine.Serialization;

namespace MushOut.Player
{
    /// <summary>
    /// 플레이어가 사용할 수 있는 특수 행동의 상태를 관리합니다.
    /// 1, 2, 3, 4 입력으로 현재 능력을 전환합니다.
    /// </summary>
    public enum AbilityState
    {
        Dash,
        Paralyze,
        Mad,
        Bomb
    }

    public class AbilityController : MonoBehaviour
    {
        [Header("Ability State")]
        [Tooltip("현재 활성화된 특수 행동 상태입니다.")]
        [SerializeField] private AbilityState _currentState = AbilityState.Dash;

        [Header("Ability Resources")]
        [Tooltip("대시 사용 가능 횟수입니다.")]
        [FormerlySerializedAs("dashcount")]
        [SerializeField] private int _dashCount = 3;

        [Tooltip("수면 포자 보유 개수입니다.")]
        [FormerlySerializedAs("sleepfungus")]
        [SerializeField] private int _sleepFungus = 3;

        [Tooltip("광분 포자 보유 개수입니다.")]
        [FormerlySerializedAs("aggrofungus")]
        [SerializeField] private int _aggroFungus = 3;

        [Tooltip("폭탄 포자 보유 개수입니다.")]
        [FormerlySerializedAs("bombfungus")]
        [SerializeField] private int _bombFungus = 3;

        [Header("Ability Unlocked")]
        [Tooltip("대시 능력 해금 여부입니다.")]
        [SerializeField] private bool _dashUnlocked;

        [Tooltip("마비 능력 해금 여부입니다.")]
        [SerializeField] private bool _paralyzeUnlocked;

        [Tooltip("광분 능력 해금 여부입니다.")]
        [SerializeField] private bool _madUnlocked;

        [Tooltip("폭탄 능력 해금 여부입니다.")]
        [SerializeField] private bool _bombUnlocked;

        private PlayerInputHandler _input;

        public AbilityState CurrentState => _currentState;

        public int DashCount
        {
            get => _dashCount;
            set => _dashCount = Mathf.Max(0, value);
        }

        public int SleepFungus
        {
            get => _sleepFungus;
            set => _sleepFungus = Mathf.Max(0, value);
        }

        public int AggroFungus
        {
            get => _aggroFungus;
            set => _aggroFungus = Mathf.Max(0, value);
        }

        public int BombFungus
        {
            get => _bombFungus;
            set => _bombFungus = Mathf.Max(0, value);
        }

        public bool DashUnlocked => _dashUnlocked;
        public bool ParalyzeUnlocked => _paralyzeUnlocked;
        public bool MadUnlocked => _madUnlocked;
        public bool BombUnlocked => _bombUnlocked;

        // 이전 브랜치의 기존 스크립트 호환용 이름입니다.
        public int dashcount
        {
            get => DashCount;
            set => DashCount = value;
        }

        public int sleepfungus
        {
            get => SleepFungus;
            set => SleepFungus = value;
        }

        public int aggrofungus
        {
            get => AggroFungus;
            set => AggroFungus = value;
        }

        public int bombfungus
        {
            get => BombFungus;
            set => BombFungus = value;
        }

        private void Awake()
        {
            _input = GetComponent<PlayerInputHandler>();
            if (_input == null)
            {
                Debug.LogWarning("[AbilityController] PlayerInputHandler를 찾을 수 없습니다.");
            }
        }

        private void Update()
        {
            if (_input == null) return;

            if (_input.IsAbility1)
            {
                TryChangeState(AbilityState.Dash);
            }
            else if (_input.IsAbility2)
            {
                TryChangeState(AbilityState.Paralyze);
            }
            else if (_input.IsAbility3)
            {
                TryChangeState(AbilityState.Mad);
            }
            else if (_input.IsAbility4)
            {
                TryChangeState(AbilityState.Bomb);
            }
        }

        public bool TryChangeState(AbilityState newState)
        {
            if (!IsUnlocked(newState)) return false;

            ChangeState(newState);
            return true;
        }

        public void ChangeState(AbilityState newState)
        {
            if (_currentState == newState) return;

            _currentState = newState;
            Debug.Log($"[AbilityController] 현재 능력이 변경되었습니다: {_currentState}");
        }

        public bool IsUnlocked(AbilityState state)
        {
            switch (state)
            {
                case AbilityState.Dash:
                    return _dashUnlocked;
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

        public int GetResourceCount(AbilityState state)
        {
            switch (state)
            {
                case AbilityState.Dash:
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

        public bool HasResource(AbilityState state)
        {
            return GetResourceCount(state) > 0;
        }

        public void UnlockDash()
        {
            _dashUnlocked = true;
        }

        public void UnlockParalyze()
        {
            _paralyzeUnlocked = true;
        }

        public void UnlockMad()
        {
            _madUnlocked = true;
        }

        public void UnlockBomb()
        {
            _bombUnlocked = true;
        }

        public void UseDash()
        {
            DashCount--;
        }

        public void UseParalyze()
        {
            SleepFungus--;
        }

        public void UseMad()
        {
            AggroFungus--;
        }

        public void UseBomb()
        {
            BombFungus--;
        }

        private void UseAbility(AbilityState state)
        {
            switch (state)
            {
                case AbilityState.Dash:
                    DashCount--;
                    break;
                case AbilityState.Paralyze:
                    SleepFungus--;
                    break;
                case AbilityState.Mad:
                    AggroFungus--;
                    break;
                case AbilityState.Bomb:
                    BombFungus--;
                    break;
            }
        }
    }
}
