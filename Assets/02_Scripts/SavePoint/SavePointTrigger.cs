using UnityEngine;
using UnityEngine.SceneManagement;

namespace MushOut.SavePoint
{
    /// <summary>
    /// 플레이어가 닿았을 때 부활(체크포인트) 위치를 갱신하는 트리거 컴포넌트입니다.
    /// 구역 사이의 입구/출구 등에 배치하여 지정된 빈 오브젝트의 위치로 플레이어를 부활시킵니다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SavePointTrigger : MonoBehaviour
    {
        [Header("세이브 포인트 설정")]
        [Tooltip("부활할 위치를 나타내는 빈 오브젝트의 트랜스폼입니다. 미지정 시 이 콜라이더 위치를 사용합니다.")]
        [SerializeField] private Transform respawnPoint;

        [Tooltip("세이브 포인트 고유 식별자입니다. 미지정 시 게임오브젝트 이름을 식별자로 사용합니다.")]
        [SerializeField] private string savePointId;

        private static bool _hasActiveSavePoint;
        private static string _activeSavePointId;
        private static Vector3 _activeRespawnPosition;
        private static Quaternion _activeRespawnRotation;

        public string SavePointId => savePointId;

        private void Awake()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;

            // 부활 지점이 설정되지 않았다면 현재 오브젝트를 기본값으로 사용
            if (respawnPoint == null)
            {
                respawnPoint = transform;
            }

            // 고유 식별자가 설정되지 않았다면 오브젝트 이름을 기본값으로 사용
            if (string.IsNullOrEmpty(savePointId))
            {
                savePointId = gameObject.name;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;

            // 이미 활성화된 동일한 세이브 포인트인 경우 중복 처리 방지
            if (_hasActiveSavePoint && _activeSavePointId == savePointId) return;

            // 플레이어가 진입하면 설정된 빈 오브젝트(respawnPoint)의 위치와 회전값으로 세이브 포인트 갱신
            SetActiveSavePoint(savePointId, respawnPoint.position, respawnPoint.rotation);
        }

        /// <summary>
        /// 활성화되어 있는 세이브 포인트(부활 지점) 정보를 반환합니다.
        /// </summary>
        public static bool TryGetActiveRespawn(out Vector3 position, out Quaternion rotation)
        {
            position = _activeRespawnPosition;
            rotation = _activeRespawnRotation;
            return _hasActiveSavePoint;
        }

        /// <summary>
        /// 새로운 세이브 포인트를 활성화합니다.
        /// </summary>
        public static void SetActiveSavePoint(string id, Vector3 position, Quaternion rotation)
        {
            _hasActiveSavePoint = true;
            _activeSavePointId = id;
            _activeRespawnPosition = position;
            _activeRespawnRotation = rotation;

            Debug.Log($"[SavePoint] Activated: {_activeSavePointId} at {position}");
        }

        /// <summary>
        /// 활성화되어 있는 세이브 포인트 상태를 제거하고 초기화합니다.
        /// </summary>
        public static void ClearActiveSavePoint()
        {
            _hasActiveSavePoint = false;
            _activeSavePointId = null;
            _activeRespawnPosition = Vector3.zero;
            _activeRespawnRotation = Quaternion.identity;
            Debug.Log("[SavePoint] Active save point cleared.");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterSceneLoaded()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Single 모드로 씬이 새로 로드될 때(메인 화면 이동, 새로운 스테이지 시작 등) 정적 세이브 포인트를 비워줍니다.
            if (mode == LoadSceneMode.Single)
            {
                ClearActiveSavePoint();
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
    }
}
