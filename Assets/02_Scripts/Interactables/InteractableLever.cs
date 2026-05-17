using UnityEngine;
using UnityEngine.Events;
using MushOut.Interaction;

namespace MushOut.Interactables
{
    /// <summary>
    /// 온/오프 상태를 가지는 상호작용 가능한 레버 오브젝트
    /// </summary>
    public class InteractableLever : MonoBehaviour, IInteractable
    {
        [Header("Lever State")]
        [Tooltip("현재 레버가 켜져 있는지 여부")]
        public bool isOn = false;

        [Header("Events (Decoupled)")]
        [Tooltip("레버가 켜질 때 (On) 실행될 이벤트")]
        public UnityEvent onTurnOn;
        
        [Tooltip("레버가 꺼질 때 (Off) 실행될 이벤트")]
        public UnityEvent onTurnOff;

        [Tooltip("레버의 상태가 변경될 때마다 실행될 이벤트 (상태값 전달)")]
        public UnityEvent<bool> onToggle;

        private Behaviour outlineScript;

        private void Awake()
        {
            // 사용자가 추가한 Outline 컴포넌트를 찾아 캐싱합니다.
            outlineScript = GetComponent("Outline") as Behaviour;
            if (outlineScript != null)
            {
                outlineScript.enabled = false;
            }
            else
            {
                Debug.LogWarning($"[InteractableLever] {gameObject.name}에 Outline 스크립트가 없습니다!");
            }
        }

        public void Interact()
        {
            // 상태를 토글
            isOn = !isOn;

            // 상태에 따른 이벤트 호출
            if (isOn)
            {
                onTurnOn?.Invoke();
            }
            else
            {
                onTurnOff?.Invoke();
            }

            // 통합 토글 이벤트 호출
            onToggle?.Invoke(isOn);
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
    }
}
