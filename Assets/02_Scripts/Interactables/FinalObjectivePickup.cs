using MushOut.Core;
using MushOut.Interaction;
using MushOut.UI;
using UnityEngine;
using UnityEngine.Events;

namespace MushOut.Interactables
{
    public class FinalObjectivePickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private bool pickupOnPlayerTouch = true;
        [SerializeField] private bool destroyOnPickup = true;
        [SerializeField] private bool switchToEscapeState = true;
        [SerializeField] private UnityEvent onObjectiveTaken;

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
            _collected = true;

            EscapeScreenEffect.PlayEnterPulse();

            if (switchToEscapeState && GameManager.Instance != null)
            {
                GameManager.Instance.ChangeState(GameManager.GameState.Escaping);
            }

            onObjectiveTaken?.Invoke();

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
