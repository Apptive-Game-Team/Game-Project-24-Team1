using UnityEngine;
using System;

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
            Paused,     // 일시 정지
            GameOver,   // 실패
            Success     // 클리어
        }

        /// <summary> 게임 상태가 변경될 때 호출되는 이벤트입니다. </summary>
        public event Action<GameState> OnGameStateChanged;
        #endregion

        #region Fields
        private GameState _currentState = GameState.None;

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
        #endregion

        private void Awake()
        {
            // 싱글톤 초기화 및 중복 파괴 처리
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
            // 초기 상태 설정
            ChangeState(GameState.Ready);

            // 전역 플레이어 찾아서 캐싱 (Enemy 등에서 무거운 Find를 매번 하지 않도록)
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) playerObj = GameObject.Find("Player");
            if (playerObj != null) PlayerTransform = playerObj.transform;
        }

        /// <summary>
        /// 게임의 상태를 변경합니다.
        /// </summary>
        /// <param name="newState">새로운 게임 상태</param>
        public void ChangeState(GameState newState)
        {
            CurrentState = newState;
        }

        /// <summary>
        /// 일시정지 상태를 토글합니다.
        /// </summary>
        public void TogglePause()
        {
            if (CurrentState == GameState.Playing)
            {
                ChangeState(GameState.Paused);
                Time.timeScale = 0f;
            }
            else if (CurrentState == GameState.Paused)
            {
                ChangeState(GameState.Playing);
                Time.timeScale = 1f;
            }
        }
    }
}
