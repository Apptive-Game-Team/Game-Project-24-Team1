using MushOut.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MushOut.UI
{
    public class SuccessScreenEffect : MonoBehaviour
    {
        private const string RuntimeObjectName = "[Runtime] SuccessScreenEffect";
        private const string SuccessImageResourcePath = "UI/escape";
        private const int SortingOrder = 10000;

        [SerializeField] private float fadeSpeed = 4f;

        private CanvasGroup _canvasGroup;
        private Image _image;
        private GameManager _subscribedGameManager;
        private bool _visible;

        public static SuccessScreenEffect Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateRuntimeInstance()
        {
            if (Instance != null || FindFirstObjectByType<SuccessScreenEffect>() != null) return;

            GameObject effectObject = new GameObject(RuntimeObjectName, typeof(RectTransform));
            effectObject.AddComponent<SuccessScreenEffect>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildOverlayIfNeeded();
            SubscribeToGameManager();
            SetVisible(false, immediate: true);
        }

        private void OnDestroy()
        {
            UnsubscribeFromGameManager();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            SubscribeToGameManager();

            if (_canvasGroup == null) return;

            float targetAlpha = _visible ? 1f : 0f;
            _canvasGroup.alpha = Mathf.MoveTowards(
                _canvasGroup.alpha,
                targetAlpha,
                fadeSpeed * Time.unscaledDeltaTime);
        }

        private void SubscribeToGameManager()
        {
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null || gameManager == _subscribedGameManager) return;

            UnsubscribeFromGameManager();
            _subscribedGameManager = gameManager;
            _subscribedGameManager.OnGameStateChanged += HandleGameStateChanged;
            HandleGameStateChanged(_subscribedGameManager.CurrentState);
        }

        private void UnsubscribeFromGameManager()
        {
            if (_subscribedGameManager == null) return;

            _subscribedGameManager.OnGameStateChanged -= HandleGameStateChanged;
            _subscribedGameManager = null;
        }

        private void HandleGameStateChanged(GameManager.GameState state)
        {
            bool success = state == GameManager.GameState.Success;
            SetVisible(success);

            if (success)
            {
                EscapeScreenEffect.SetActiveEffect(false, false);
            }
        }

        private void SetVisible(bool visible, bool immediate = false)
        {
            BuildOverlayIfNeeded();
            _visible = visible;

            if (_canvasGroup == null) return;

            if (immediate)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
            }

            _canvasGroup.blocksRaycasts = visible;
            _canvasGroup.interactable = visible;
        }

        private void BuildOverlayIfNeeded()
        {
            if (_canvasGroup != null && _image != null) return;

            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            GameObject imageObject = new GameObject("[SuccessImage]");
            imageObject.transform.SetParent(transform, false);

            _image = imageObject.AddComponent<Image>();
            _image.sprite = LoadSuccessSprite();
            _image.preserveAspect = true;
            _image.raycastTarget = false;
            StretchToParent(_image.rectTransform);
        }

        private static Sprite LoadSuccessSprite()
        {
            Sprite sprite = Resources.Load<Sprite>(SuccessImageResourcePath);
            if (sprite != null) return sprite;

            Texture2D texture = Resources.Load<Texture2D>(SuccessImageResourcePath);
            if (texture == null)
            {
                Debug.LogError($"[SuccessScreenEffect] Ending image not found: Resources/{SuccessImageResourcePath}");
                return null;
            }

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
