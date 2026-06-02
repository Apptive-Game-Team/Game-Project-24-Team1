using UnityEngine;
using UnityEngine.Events;
using MushOut.Core; // GameManager 네임스페이스 추가

namespace MushOut.Interactables
{
    /// <summary>
    /// 특정 구역(Trigger)에 진입했을 때 설정된 이벤트를 발생시키는 범용 스크립트입니다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AreaTriggerEnter : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("이 구역을 작동시킬 수 있는 대상의 레이어 (예: Player)")]
        public LayerMask targetLayer;
        
        [Tooltip("한 번만 작동할지 여부")]
        public bool triggerOnlyOnce = true;

        // unity inspector의 UnityEvent는 enum을 매개변수로 받는 함수를 
        // 드롭다운 목록에 띄워주지 않아서 Game State 변경하는 기능 따로 만들 수 밖에
        [Header("Game State Change (Optional)")]
        [Tooltip("이 구역에 닿았을 때 변경할 게임 상태입니다. (None이면 변경하지 않음)")]
        public GameManager.GameState changeToState = GameManager.GameState.None;
        
        [Header("Extra Events")]
        [Tooltip("상태 변경 외에 추가로 실행할 유니티 이벤트")]
        public UnityEvent onTriggerEnterEvent;

        private bool _hasTriggered = false;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggerOnlyOnce && _hasTriggered) return;

            if (((1 << other.gameObject.layer) & targetLayer) != 0)
            {
                _hasTriggered = true;
                
                // 1. 게임 상태 변경 설정이 되어있다면 코드로 직접 변경
                if (changeToState != GameManager.GameState.None)
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.ChangeState(changeToState);
                    }
                }

                // 2. 추가적인 유니티 이벤트 실행 (사운드 재생, 파티클 등)
                onTriggerEnterEvent?.Invoke();
            }
        }
    }
}
