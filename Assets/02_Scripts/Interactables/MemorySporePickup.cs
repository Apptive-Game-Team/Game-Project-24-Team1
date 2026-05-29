using MushOut.Interaction;
using MushOut.UI;
using UnityEngine;

namespace MushOut.Interactables
{
    public class MemorySporePickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private int amount = 1;
        [SerializeField] private bool destroyOnPickup = true;
        [SerializeField] private bool pickupOnPlayerTouch = true;

        private Behaviour _outlineScript;
        private bool _collected;

        private void Awake()
        {
            _outlineScript = GetComponent("Outline") as Behaviour;
            if (_outlineScript != null)
            {
                _outlineScript.enabled = false;
            }
        }

        public void Interact()
        {
            Collect();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!pickupOnPlayerTouch || !IsPlayer(other)) return;
            Collect();
        }

        private void Collect()
        {
            if (_collected) return;

            MemorySporeUI ui = MemorySporeUI.Instance;
            if (ui == null)
            {
                ui = FindFirstObjectByType<MemorySporeUI>();
            }

            if (ui == null)
            {
                Debug.LogWarning("[MemorySporePickup] MemorySporeUI를 찾지 못해서 기억포자를 획득하지 못했습니다.");
                return;
            }

            _collected = true;
            ui.AddMemorySpores(amount);

            if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
        }

        private static bool IsPlayer(Collider other)
        {
            if (other == null) return false;
            if (other.CompareTag("Player")) return true;

            Transform root = other.transform.root;
            if (root != null && root.CompareTag("Player")) return true;

            return other.GetComponentInParent<MushOut.Player.PlayerInputHandler>() != null;
        }

        public void OnHighlight()
        {
            if (_outlineScript != null)
            {
                _outlineScript.enabled = true;
            }
        }

        public void OnUnhighlight()
        {
            if (_outlineScript != null)
            {
                _outlineScript.enabled = false;
            }
        }
    }
}
