using UnityEngine;
using System;
using UnityEngine.SceneManagement;

namespace MushOut.Core
{
    /// <summary>
    /// 게임의 전체적인 상태(시작, 일시정지, 종료 등)를 관리하는 전역 싱글톤 매니저입니다.
    /// Event-Driven 아키텍처를 위해 상태 변화를 Action으로 브로드캐스트합니다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GameManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Enums & Events
        public enum GameState
        {
            None,
            Loading,
            Ready,      // 게임 시작 전 대기 상태
            Playing,    // 플레이 중
            Escaping,   // Final objective acquired, escape phase is active
            Paused,     // 일시 정지
            GameOver,   // 실패
            Success     // 클리어
        }

        /// <summary> 게임 상태가 변경될 때 호출되는 이벤트입니다. </summary>
        public event Action<GameState> OnGameStateChanged;
        #endregion

        #region Fields
        private GameState _currentState = GameState.None;
        private GameState _stateBeforePause = GameState.Playing;

        /// <summary> 현재 게임의 상태입니다. 외부에서는 읽기만 가능합니다. </summary>
        public GameState CurrentState
        {
            get => _currentState;
            private set
            {
                if (_currentState == value) return;
                _currentState = value;
                OnGameStateChanged?.Invoke(_currentState);
                
                Debug.Log($"[GameManager] State Changed: {_currentState}");
            }
        }

        /// <summary> 씬 내의 전역 플레이어 Transform 캐싱 </summary>
        public Transform PlayerTransform { get; private set; }

        private bool _crashedByEnemy = false;
        
        /// <summary> 적과 충돌하여 게임 오버되는 상태를 나타냅니다. </summary>
        public bool CrashedByEnemy
        {
            get => _crashedByEnemy;
            set
            {
                if (_crashedByEnemy == value) return;
                _crashedByEnemy = value;

                if (_crashedByEnemy && CurrentState != GameState.GameOver)
                {
                    ChangeState(GameState.GameOver);
                }
            }
        }
        #endregion

        private void Awake()
        {
            // 싱글톤 초기화 및 중복 파괴 처리
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                EnsureGameOverManager();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Start()
        {
            if (CurrentState == GameState.None)
            {
                ChangeState(GameState.Ready);
            }

            CachePlayerTransform();
            ApplyStateForScene(SceneManager.GetActiveScene());
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CachePlayerTransform();
            ApplyStateForScene(scene);
        }

        private void CachePlayerTransform()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) playerObj = GameObject.Find("Player");
            PlayerTransform = playerObj != null ? playerObj.transform : null;
        }

        private void ApplyStateForScene(Scene scene)
        {
            if (scene.name == "GamePlayScene" && CurrentState != GameState.GameOver && CurrentState != GameState.Success)
            {
                ClearRuntimePause();
                ChangeState(GameState.Playing);
            }
            else if (scene.name == "GameStartScene")
            {
                ChangeState(GameState.Ready);
            }
        }

        private void ClearRuntimePause()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPaused = false;
#endif
        }


        /// <summary>
        /// 게임의 상태를 변경합니다.
        /// </summary>
        /// <param name="newState">새로운 게임 상태</param>
        public void ChangeState(GameState newState)
        {
            if (newState == GameState.GameOver)
            {
                EnsureGameOverManager();
            }

            CurrentState = newState;

            if (newState == GameState.GameOver)
            {
                GetComponent<GameOverManager>()?.BeginGameOver();
            }
        }

        private void EnsureGameOverManager()
        {
            if (GetComponent<GameOverManager>() == null)
            {
                gameObject.AddComponent<GameOverManager>();
            }
        }

        /// <summary>
        /// 일시정지 상태를 토글합니다.
        /// </summary>
        public void TogglePause()
        {
            if (CurrentState == GameState.Playing || CurrentState == GameState.Escaping)
            {
                _stateBeforePause = CurrentState;
                ChangeState(GameState.Paused);
                Time.timeScale = 0f;
            }
            else if (CurrentState == GameState.Paused)
            {
                ChangeState(_stateBeforePause);
                Time.timeScale = 1f;
            }
        }
    }
}
