using UnityEngine;

namespace MushOut.Interaction
{
    [RequireComponent(typeof(Renderer))]
    public class ChangeMat : MonoBehaviour
    {
        [Header("Material Settings")]
        [Tooltip("변경할 대상 머티리얼을 할당하세요.")]
        [SerializeField] private Material targetMaterial;

        private Renderer _renderer;
        private Material _originalMaterial;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                // 시작할 때 오브젝트가 가지고 있던 원래 머티리얼을 저장합니다.
                _originalMaterial = _renderer.material;
            }
        }

        /// <summary>
        /// 인스펙터에 할당된 targetMaterial로 머티리얼을 변경합니다.
        /// Unity UnityEvent(예: 버튼 클릭, 트리거 등)에서 호출하기 좋습니다.
        /// </summary>
        public void ApplyMaterial()
        {
            if (_renderer != null && targetMaterial != null)
            {
                _renderer.material = targetMaterial;
            }
            else
            {
                Debug.LogWarning($"[{name}] 대상 머티리얼이 할당되지 않았거나 Renderer 컴포넌트가 없습니다.");
            }
        }

        /// <summary>
        /// 외부 스크립트에서 직접 특정 머티리얼을 넘겨받아 변경합니다.
        /// </summary>
        /// <param name="newMaterial">변경할 새 머티리얼</param>
        public void ApplySpecificMaterial(Material newMaterial)
        {
            if (_renderer != null && newMaterial != null)
            {
                _renderer.material = newMaterial;
            }
        }

        /// <summary>
        /// 머티리얼을 게임 시작 시점에 가지고 있던 원래 머티리얼로 되돌립니다.
        /// </summary>
        public void RestoreMaterial()
        {
            if (_renderer != null && _originalMaterial != null)
            {
                _renderer.material = _originalMaterial;
            }
        }
    }
}
