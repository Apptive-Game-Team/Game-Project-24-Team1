using UnityEngine;
using MushOut.Core;

namespace MushOut.Core
{
    /// <summary>
    /// UI의 생성과 전역 UI 이벤트를 관리하는 싱글톤 매니저입니다.
    /// 씬 병합 충돌을 방지하기 위해 런타임에 UI를 생성하거나 추가 로드합니다.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        #region Singleton
        private static UIManager _instance;
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<UIManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("UIManager");
                        _instance = go.AddComponent<UIManager>();
                    }
                }
                return _instance;
            }
        }
        #endregion

        [Header("UI Prefabs")]
        [Tooltip("인스펙터에서 'Player UI' 프리팹을 할당해 주세요.")]
        [SerializeField] private GameObject _playerUIPrefab;

        private GameObject _activePlayerUI;

        private void Awake()
        {
            // 싱글톤 초기화
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // GameManager가 존재한다면 상태 변화 이벤트를 구독하여 UI 대응 로직을 구현할 수 있습니다.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }

            // 게임 시작 시 필요한 UI 인스턴스화
            InitializeUI();
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제 (메모리 누수 방지)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        /// <summary>
        /// 필요한 UI 요소를 런타임에 생성합니다.
        /// 이를 통해 씬 파일(.unity)에 UI 캔버스를 직접 배치하지 않아 충돌을 방지합니다.
        /// </summary>
        public void InitializeUI()
        {
            if (_playerUIPrefab == null)
            {
                Debug.LogWarning("[UIManager] Player UI 프리팹이 할당되지 않았습니다. 인스펙터 확인이 필요합니다.");
                return;
            }

            if (_activePlayerUI == null)
            {
                _activePlayerUI = Instantiate(_playerUIPrefab);
                _activePlayerUI.name = "[Runtime] PlayerUI";
                
                // 생성된 UI를 DontDestroyOnLoad 혹은 특정 UI 전용 루트로 관리할 수 있습니다.
                // 씬 전환 시에도 유지되어야 한다면 DontDestroyOnLoad를 호출하거나 UIManager의 자식으로 둡니다.
                // _activePlayerUI.transform.SetParent(this.transform);
            }
        }

        /// <summary>
        /// GameManager의 상태 변화에 따라 UI의 가시성이나 데이터를 업데이트합니다.
        /// UI 로직은 오직 데이터를 읽거나 이벤트를 구독하기만 해야 합니다.
        /// </summary>
        private void HandleGameStateChanged(GameManager.GameState state)
        {
            switch (state)
            {
                case GameManager.GameState.Paused:
                    // 일시정지 UI 표시 로직 등
                    break;
                case GameManager.GameState.Playing:
                    // 게임 플레이 UI 표시 로직 등
                    break;
            }
        }
    }
}
