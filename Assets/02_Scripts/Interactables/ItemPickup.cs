using UnityEngine;
using MushOut.Player;
using MushOut.Interaction;

namespace MushOut.Interactables
{
    /// <summary>
    /// 플레이어의 자원을 증가시키거나 능력을 해금하는 아이템 픽업 스크립트입니다.
    /// IInteractable을 상속받아 직접 상호작용 및 아웃라인 처리를 수행합니다.
    /// </summary>
    public class ItemPickup : MonoBehaviour, IInteractable
    {
        [Header("Unlock Abilities (능력 해금)")]
        [Tooltip("체크 시 수면 포자(마비) 능력을 해금합니다.")]
        [SerializeField] private bool unlockParalyze;
        
        [Tooltip("체크 시 광분 포자 능력을 해금합니다.")]
        [SerializeField] private bool unlockMad;
        
        [Tooltip("체크 시 폭탄 포자 능력을 해금합니다.")]
        [SerializeField] private bool unlockBomb;

        [Header("Add Resources (자원 +1)")]
        [Tooltip("체크 시 대시 횟수를 1 증가시킵니다.")]
        [SerializeField] private bool addDash;
        
        [Tooltip("체크 시 수면 포자 개수를 1 증가시킵니다.")]
        [SerializeField] private bool addSleepFungus;
        
        [Tooltip("체크 시 광분 포자 개수를 1 증가시킵니다.")]
        [SerializeField] private bool addAggroFungus;
        
        [Tooltip("체크 시 폭탄 포자 개수를 1 증가시킵니다.")]
        [SerializeField] private bool addBombFungus;

        [Header("Misc Settings")]
        [Tooltip("체크 시 획득(상호작용)이 끝난 후 이 오브젝트를 삭제합니다.")]
        [SerializeField] private bool destroyOnPickup = true;

        private Behaviour outlineScript;

        private void Awake()
        {
            // 사용자가 추가한 Outline 컴포넌트를 찾아 캐싱합니다.
            outlineScript = GetComponent("Outline") as Behaviour;
            if (outlineScript != null)
            {
                outlineScript.enabled = false;
            }
        }

        public void Interact()
        {
            PickupItem();
        }

        public void OnHighlight()
        {
            if (outlineScript != null)
            {
                outlineScript.enabled = true;
            }
        }

        public void OnUnhighlight()
        {
            if (outlineScript != null)
            {
                outlineScript.enabled = false;
            }
        }

        /// <summary>
        /// 아이템 획득 로직을 실행합니다.
        /// </summary>
        public void PickupItem()
        {
            // 'Player' 태그를 가진 오브젝트를 찾습니다.
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            
            if (player != null)
            {
                // 플레이어에서 AbilityController를 가져옵니다. (자식 오브젝트에 있을 수도 있으므로 GetComponentInChildren 사용)
                AbilityController abilityController = player.GetComponentInChildren<AbilityController>();
                
                if (abilityController != null)
                {
                    ApplyEffects(abilityController);
                }
                else
                {
                    Debug.LogWarning("[ItemPickup] 플레이어 오브젝트에서 AbilityController를 찾을 수 없습니다.");
                }
            }
            else
            {
                Debug.LogWarning("[ItemPickup] 씬에서 'Player' 태그를 가진 오브젝트를 찾을 수 없습니다. 플레이어의 태그가 'Player'인지 확인해주세요.");
            }
        }

        private void ApplyEffects(AbilityController controller)
        {
            // 능력 해금 적용
            if (unlockParalyze) controller.UnlockParalyze();
            if (unlockMad) controller.UnlockMad();
            if (unlockBomb) controller.UnlockBomb();

            // 자원 획득(증가) 적용
            if (addDash) controller.AddDash();
            if (addSleepFungus) controller.AddSleepFungus();
            if (addAggroFungus) controller.AddAggroFungus();
            if (addBombFungus) controller.AddBombFungus();
            
            Debug.Log($"[ItemPickup] 아이템 획득 효과가 적용되었습니다! ({gameObject.name})");

            if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
        }
    }
}
