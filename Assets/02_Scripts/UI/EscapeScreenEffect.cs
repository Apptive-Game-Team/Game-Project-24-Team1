using UnityEngine;
using UnityEngine.UI;

namespace MushOut.UI
{
    public class EscapeScreenEffect : MonoBehaviour
    {
        private const string RuntimeObjectName = "[Runtime] EscapeScreenEffect";
        private const int SortingOrder = 9000;

        [Header("Look")]
        [SerializeField] private Color edgeColor = new Color(0.95f, 0.05f, 0.02f, 1f);
        [SerializeField, Range(0f, 1f)] private float idleAlpha = 0.45f;
        [SerializeField, Range(0f, 1f)] private float pulseAlpha = 0.18f;
        [SerializeField] private float pulseSpeed = 2.6f;
        [SerializeField] private float fadeSpeed = 3.5f;

        [Header("Texture")]
        [SerializeField] private int textureWidth = 256;
        [SerializeField] private int textureHeight = 144;
        [SerializeField, Range(0f, 1f)] private float clearCenterSize = 0.52f;
        [SerializeField, Range(0f, 1f)] private float edgeThickness = 0.36f;
        [SerializeField, Range(0f, 0.35f)] private float noiseStrength = 0.11f;

        private CanvasGroup _canvasGroup;
        private Image _image;
        private Sprite _vignetteSprite;
        private bool _active;
        private float _targetAlpha;

        public static EscapeScreenEffect Instance { get; private set; }

        public static void SetActiveEffect(bool active, bool playEntryPulse = true)
        {
            EscapeScreenEffect effect = Instance != null ? Instance : FindFirstObjectByType<EscapeScreenEffect>();
            if (effect == null)
            {
                GameObject effectObject = new GameObject(RuntimeObjectName);
                effect = effectObject.AddComponent<EscapeScreenEffect>();
            }

            effect.SetActive(active, playEntryPulse);
        }

        public static void PlayEnterPulse()
        {
            SetActiveEffect(true);
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
            SetVisualAlpha(0f);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (_canvasGroup == null) return;

            float pulse = _active ? Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.5f + 0.5f : 0f;
            float desiredAlpha = _active ? Mathf.Clamp01(_targetAlpha + pulse * pulseAlpha) : 0f;
            float nextAlpha = Mathf.MoveTowards(_canvasGroup.alpha, desiredAlpha, fadeSpeed * Time.unscaledDeltaTime);
            SetVisualAlpha(nextAlpha);
        }

        private void SetActive(bool active, bool playEntryPulse)
        {
            BuildOverlayIfNeeded();

            _active = active;
            _targetAlpha = active ? idleAlpha : 0f;

            if (active)
            {
                gameObject.SetActive(true);
                if (playEntryPulse)
                {
                    SetVisualAlpha(Mathf.Max(_canvasGroup.alpha, Mathf.Clamp01(idleAlpha + pulseAlpha)));
                }
            }
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

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            Transform imageTransform = transform.Find("[EscapeVignette]");
            GameObject imageObject = imageTransform != null ? imageTransform.gameObject : new GameObject("[EscapeVignette]");
            imageObject.transform.SetParent(transform, false);

            _image = imageObject.GetComponent<Image>();
            if (_image == null)
            {
                _image = imageObject.AddComponent<Image>();
            }

            _image.sprite = CreateVignetteSprite();
            _image.color = edgeColor;
            _image.raycastTarget = false;
            StretchToParent(_image.rectTransform);
        }

        private Sprite CreateVignetteSprite()
        {
            if (_vignetteSprite != null) return _vignetteSprite;

            int width = Mathf.Max(16, textureWidth);
            int height = Mathf.Max(16, textureHeight);
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "EscapeScreenVignette",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            float inner = Mathf.Clamp01(clearCenterSize);
            float outer = Mathf.Clamp01(inner + Mathf.Max(0.01f, edgeThickness));

            for (int y = 0; y < height; y++)
            {
                float v = height <= 1 ? 0f : y / (height - 1f);
                for (int x = 0; x < width; x++)
                {
                    float u = width <= 1 ? 0f : x / (width - 1f);
                    float dx = Mathf.Abs(u - 0.5f) * 2f;
                    float dy = Mathf.Abs(v - 0.5f) * 2f;
                    float edge = Mathf.Max(dx, dy);
                    float radialEdge = Mathf.Sqrt(dx * dx + dy * dy) * 0.72f;
                    float mask = Mathf.Max(SmoothStep(inner, outer, edge), SmoothStep(inner, 1f, radialEdge) * 0.55f);
                    float grain = (Hash01(x, y) - 0.5f) * noiseStrength;
                    float alpha = Mathf.Clamp01(mask + grain);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            _vignetteSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            return _vignetteSprite;
        }

        private void SetVisualAlpha(float alpha)
        {
            if (_canvasGroup == null) return;
            _canvasGroup.alpha = Mathf.Clamp01(alpha);
        }

        private static float SmoothStep(float edge0, float edge1, float value)
        {
            float t = Mathf.Clamp01((value - edge0) / Mathf.Max(0.0001f, edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        private static float Hash01(int x, int y)
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return (h ^ (h >> 16)) / 4294967295f;
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
