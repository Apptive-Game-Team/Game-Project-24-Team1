using MushOut.Core;
using MushOut.Interaction;
using MushOut.Player;
using MushOut.SavePoint;
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
        [SerializeField] private bool saveRespawnOnPickup = true;
        [SerializeField] private string pickupSavePointId = "SavePoint_8";
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
            Collect(FindPlayerTransform());
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!pickupOnPlayerTouch || !IsPlayer(other)) return;
            Collect(GetPlayerTransform(other));
        }

        private void Collect(Transform collector)
        {
            if (_collected) return;
            _collected = true;

            if (saveRespawnOnPickup)
            {
                Transform saveTransform = collector != null ? collector : transform;
                SavePointTrigger.SetActiveSavePoint(pickupSavePointId, saveTransform.position, saveTransform.rotation);
            }

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

        private static Transform GetPlayerTransform(Collider other)
        {
            if (other == null) return null;

            Transform root = other.transform.root;
            if (root != null && root.CompareTag("Player")) return root;
            if (other.CompareTag("Player")) return other.transform;

            PlayerInputHandler inputHandler = other.GetComponentInParent<PlayerInputHandler>();
            return inputHandler != null ? inputHandler.transform : root;
        }

        private static Transform FindPlayerTransform()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) return player.transform;

            PlayerInputHandler inputHandler = FindFirstObjectByType<PlayerInputHandler>();
            return inputHandler != null ? inputHandler.transform : null;
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
