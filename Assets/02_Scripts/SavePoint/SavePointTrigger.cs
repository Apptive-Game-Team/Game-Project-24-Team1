using UnityEngine;
using MushOut.Player;

namespace MushOut.SavePoint
{
    public enum SavePointActivationMode
    {
        EnterOrStay,
        ExitAfterEnter
    }

    [RequireComponent(typeof(Collider))]
    public class SavePointTrigger : MonoBehaviour
    {
        [SerializeField] private string savePointId = "SavePoint_1";
        [SerializeField] private SavePointActivationMode activationMode = SavePointActivationMode.EnterOrStay;
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private bool usePlayerPositionOnSave = true;
        [SerializeField] private bool usePlayerRotationOnSave;
        [SerializeField] private bool requireGrounded = true;
        [SerializeField] private bool allowWater;

        private static bool _hasActiveSavePoint;
        private static string _activeSavePointId;
        private static Vector3 _activeRespawnPosition;
        private static Quaternion _activeRespawnRotation;

        private bool _playerEntered;

        public string SavePointId => savePointId;

        private void Awake()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;

            if (respawnPoint == null)
            {
                respawnPoint = transform;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;

            _playerEntered = true;
            if (activationMode != SavePointActivationMode.EnterOrStay) return;

            ActivateIfPlayer(other);
        }

        private void OnTriggerStay(Collider other)
        {
            if (activationMode != SavePointActivationMode.EnterOrStay) return;

            ActivateIfPlayer(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (activationMode != SavePointActivationMode.ExitAfterEnter) return;
            if (!_playerEntered) return;

            ActivateIfPlayer(other);
        }

        private void ActivateIfPlayer(Collider other)
        {
            if (!IsPlayer(other)) return;
            if (_hasActiveSavePoint && _activeSavePointId == savePointId) return;

            Transform playerTransform = other.transform.root != null ? other.transform.root : other.transform;
            if (requireGrounded && !CanSaveAtPlayer(playerTransform, allowWater)) return;

            Vector3 position = usePlayerPositionOnSave || respawnPoint == null
                ? playerTransform.position
                : respawnPoint.position;
            Quaternion rotation = usePlayerRotationOnSave || respawnPoint == null
                ? playerTransform.rotation
                : respawnPoint.rotation;

            SetActiveSavePoint(savePointId, position, rotation);
        }

        private static bool CanSaveAtPlayer(Transform playerTransform, bool allowWater)
        {
            if (playerTransform == null) return false;

            PlayerEnvironmentDetector detector = playerTransform.GetComponentInChildren<PlayerEnvironmentDetector>();
            if (detector == null)
            {
                detector = playerTransform.GetComponentInParent<PlayerEnvironmentDetector>();
            }

            if (detector == null) return true;

            detector.CheckEnvironment();
            return detector.IsGrounded && (allowWater || !detector.IsInWater);
        }

        public static bool TryGetActiveRespawn(out Vector3 position, out Quaternion rotation)
        {
            position = _activeRespawnPosition;
            rotation = _activeRespawnRotation;
            return _hasActiveSavePoint;
        }

        public static void SetActiveSavePoint(string id, Vector3 position, Quaternion rotation)
        {
            _hasActiveSavePoint = true;
            _activeSavePointId = id;
            _activeRespawnPosition = position;
            _activeRespawnRotation = rotation;

            Debug.Log($"[SavePoint] Activated: {_activeSavePointId}");
        }

        private static bool IsPlayer(Collider other)
        {
            if (other == null) return false;
            if (other.CompareTag("Player")) return true;

            Transform root = other.transform.root;
            if (root != null && root.CompareTag("Player")) return true;

            return other.GetComponentInParent<MushOut.Player.PlayerInputHandler>() != null;
        }
    }
}
