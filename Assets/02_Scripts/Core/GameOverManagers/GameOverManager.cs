// GameOverManager의 부모/본체 파일.
// Inspector에 보이는 설정값을 들고 있고, 게임 상태가 GameOver로 바뀌면 게임오버 흐름을 시작한다.

using UnityEngine;
using UnityEngine.UI;

namespace MushOut.Core
{
    [DisallowMultipleComponent]
    public partial class GameOverManager : MonoBehaviour
    {
        private const int FadeCanvasSortingOrder = 9998;
        private const int GameOverCanvasSortingOrder = 9999;
        private const string DefaultGameOverBackgroundPath = "Assets/04_Art/UI/StartScene/PreGameOver.png";

        [Header("Scene Flow")]
        [SerializeField] private string homeSceneName = "GameStartScene";

        [Header("Visuals")]
        [SerializeField] private Image fadeImage;
        [SerializeField] private Texture2D gameOverBackground;
        [SerializeField] private float fadeDuration = 0.8f;
        [SerializeField] private float blackScreenHoldDuration = 0.25f;

        [Header("Audio")]
        [SerializeField] private AudioClip gameOverSound;
        [SerializeField] private bool playSoundOnGameOver = true;

        private AudioSource _audioSource;
        private bool _isInitialized;
        private bool _isProcessingGameOver;
        private GameObject _fadeCanvasObject;
        private GameObject _gameOverCanvasObject;
        private GameObject _playerObject;
        private Vector3 _playerSpawnPosition;
        private Quaternion _playerSpawnRotation;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }
        }

        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        public void BeginGameOver()
        {
            Initialize();

            if (!_isProcessingGameOver)
            {
                StartCoroutine(ProcessGameOverSequence());
            }
        }

        private void Initialize()
        {
            if (_isInitialized)
                return;

            CachePlayerSpawn();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }

            if (fadeImage == null)
            {
                CreateRuntimeFadeImage();
            }

            ResetFadeImage();
            _isInitialized = true;
        }

        private void HandleGameStateChanged(GameManager.GameState state)
        {
            if (state == GameManager.GameState.GameOver)
            {
                BeginGameOver();
            }
        }
    }
}
