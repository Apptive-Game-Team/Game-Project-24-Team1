using UnityEngine;
using UnityEngine.Events;
using MushOut.Interaction;

namespace MushOut.Interactables
{
    public class InteractableButton : MonoBehaviour, IInteractable
    {
        [Header("Events (Decoupled)")]
        [Tooltip("버튼 상호작용 시 실행될 이벤트. 기믹 오브젝트의 public 메서드를 여기에 연결합니다.")]
        public UnityEvent onInteract;

        private Behaviour outlineScript;

        private void Awake()
        {
            // 사용자가 추가한 Outline 컴포넌트를 찾아 캐싱합니다.
            // 네임스페이스에 종속되지 않도록 Behaviour로 캐싱하여 껐다 켭니다.
            outlineScript = GetComponent("Outline") as Behaviour;
            if (outlineScript != null)
            {
                outlineScript.enabled = false;
            }
            else
            {
                Debug.LogWarning($"[InteractableButton] {gameObject.name}에 Outline 스크립트가 없습니다!");
            }
        }

        public void Interact()
        {
            // 완벽한 디커플링: 트리거는 대상 기믹을 전혀 모르며 이벤트만 발생시킴
            onInteract?.Invoke();
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
