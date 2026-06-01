using UnityEngine;
using UnityEngine.UI;
using MushOut.Core;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MushOut.UI
{
    public class EscapeScreenEffect : MonoBehaviour
    {
        private const string RuntimeObjectName = "[Runtime] EscapeScreenEffect";
        private const int SortingOrder = 9000;

        [Header("Look")]
        [SerializeField] private Color edgeColor = new Color(0.95f, 0.05f, 0.02f, 1f);
        [SerializeField, Range(0f, 1f)] private float idleAlpha = 0.18f;
        [SerializeField, Range(0f, 1f)] private float pulseAlpha = 0.06f;
        [SerializeField] private float pulseSpeed = 2.8f;
        [SerializeField] private float fadeSpeed = 3.5f;

        [Header("Post Process")]
        [SerializeField] private bool usePostProcessVignette = true;
        [SerializeField, Range(0f, 1f)] private float vignetteIdleIntensity = 0.34f;
        [SerializeField, Range(0f, 1f)] private float vignettePulseIntensity = 0.16f;
        [SerializeField, Range(0f, 1f)] private float vignetteSmoothness = 0.27f;

        [Header("Texture")]
        [SerializeField] private int textureWidth = 384;
        [SerializeField] private int textureHeight = 216;
        [SerializeField, Range(0f, 1f)] private float clearCenterSize = 0.84f;
        [SerializeField, Range(0f, 1f)] private float edgeThickness = 0.07f;
        [SerializeField, Range(0f, 0.35f)] private float noiseStrength = 0.012f;
        [SerializeField, Range(0f, 0.2f)] private float waveAmplitude = 0.032f;
        [SerializeField] private float waveFrequency = 4.8f;
        [SerializeField] private float waveSpeed = 1.15f;
        [SerializeField, Range(4f, 60f)] private float textureRefreshRate = 24f;

        private CanvasGroup _canvasGroup;
        private Image _image;
        private Texture2D _vignetteTexture;
        private Sprite _vignetteSprite;
        private bool _active;
        private float _targetAlpha;
        private float _nextTextureRefreshTime;
        private Volume _runtimeVolume;
        private Vignette _runtimeVignette;
        private GameManager _subscribedGameManager;

        public static EscapeScreenEffect Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateRuntimeInstance()
        {
            if (Instance != null || FindFirstObjectByType<EscapeScreenEffect>() != null) return;

            GameObject effectObject = new GameObject(RuntimeObjectName, typeof(RectTransform));
            effectObject.AddComponent<EscapeScreenEffect>();
        }

        public static void SetActiveEffect(bool active, bool playEntryPulse = true)
        {
            EscapeScreenEffect effect = Instance != null ? Instance : FindFirstObjectByType<EscapeScreenEffect>();
            if (effect == null)
            {
                GameObject effectObject = new GameObject(RuntimeObjectName, typeof(RectTransform));
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
            SubscribeToGameManager();
            SetVisualAlpha(0f);
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
            if (_canvasGroup == null) return;

            SubscribeToGameManager();
            EnsurePostProcessVignette();

            float pulse = _active ? Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.5f + 0.5f : 0f;
            float desiredAlpha = _active ? Mathf.Clamp01(_targetAlpha + pulse * pulseAlpha) : 0f;
            float nextAlpha = Mathf.MoveTowards(_canvasGroup.alpha, desiredAlpha, fadeSpeed * Time.unscaledDeltaTime);
            SetVisualAlpha(nextAlpha);
            UpdatePostProcessVignette(pulse);

            if (_active && Time.unscaledTime >= _nextTextureRefreshTime)
            {
                PaintVignetteTexture(Time.unscaledTime);
                _nextTextureRefreshTime = Time.unscaledTime + 1f / Mathf.Max(1f, textureRefreshRate);
            }
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
            SetActive(state == GameManager.GameState.Escaping, state == GameManager.GameState.Escaping);
        }

        private void SetActive(bool active, bool playEntryPulse)
        {
            BuildOverlayIfNeeded();
            EnsurePostProcessVignette();

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

        private void EnsurePostProcessVignette()
        {
            if (!usePostProcessVignette) return;
            if (_runtimeVignette != null) return;

            GameObject volumeObject = new GameObject("[EscapePostProcessVignette]");
            volumeObject.transform.SetParent(transform, false);

            _runtimeVolume = volumeObject.AddComponent<Volume>();
            _runtimeVolume.isGlobal = true;
            _runtimeVolume.priority = 1000f;
            _runtimeVolume.weight = 0f;
            _runtimeVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();

            _runtimeVignette = _runtimeVolume.profile.Add<Vignette>(true);
            _runtimeVignette.color.Override(edgeColor);
            _runtimeVignette.intensity.Override(0f);
            _runtimeVignette.smoothness.Override(vignetteSmoothness);
            _runtimeVignette.rounded.Override(false);
            _runtimeVignette.center.Override(new Vector2(0.5f, 0.5f));

            EnableCameraPostProcessing();

            if (_image != null)
            {
                _image.enabled = false;
            }
        }

        private void UpdatePostProcessVignette(float pulse)
        {
            if (_runtimeVolume == null || _runtimeVignette == null) return;

            EnableCameraPostProcessing();

            float desiredWeight = _active ? 1f : 0f;
            _runtimeVolume.weight = Mathf.MoveTowards(_runtimeVolume.weight, desiredWeight, fadeSpeed * Time.unscaledDeltaTime);

            float flicker = _active
                ? Mathf.Max(pulse, Mathf.Sin(Time.unscaledTime * (pulseSpeed * 1.45f)) * 0.5f + 0.5f)
                : 0f;
            float intensity = _active ? vignetteIdleIntensity + flicker * vignettePulseIntensity : 0f;
            _runtimeVignette.intensity.Override(Mathf.Clamp01(intensity));
            _runtimeVignette.smoothness.Override(vignetteSmoothness);
            _runtimeVignette.color.Override(edgeColor);
        }

        private static void EnableCameraPostProcessing()
        {
            Camera[] cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                UniversalAdditionalCameraData cameraData = cameras[i].GetComponent<UniversalAdditionalCameraData>();
                if (cameraData != null)
                {
                    cameraData.renderPostProcessing = true;
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

            _vignetteTexture = texture;
            PaintVignetteTexture(0f);
            _vignetteSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            return _vignetteSprite;
        }

        private void PaintVignetteTexture(float time)
        {
            if (_vignetteTexture == null) return;

            int width = _vignetteTexture.width;
            int height = _vignetteTexture.height;
            float baseInner = Mathf.Clamp01(clearCenterSize);
            float baseThickness = Mathf.Max(0.01f, edgeThickness);

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
                    float edgeCoord = dx > dy ? v : u;
                    float wave = GetEdgeWave(edgeCoord, time);
                    float softNoise = (Mathf.PerlinNoise(edgeCoord * 6.5f, time * 0.32f) - 0.5f) * noiseStrength;
                    float inner = Mathf.Clamp(baseInner + wave * waveAmplitude + softNoise, 0.78f, 0.92f);
                    float thickness = Mathf.Clamp(baseThickness * (0.92f + Mathf.Abs(wave) * 0.18f), 0.035f, 0.1f);
                    float outer = Mathf.Clamp01(inner + thickness);
                    float mask = Mathf.Max(SmoothStep(inner, outer, edge), SmoothStep(inner + 0.04f, 1f, radialEdge) * 0.38f);
                    float grain = (Hash01(x, y) - 0.5f) * noiseStrength;
                    float alpha = Mathf.Clamp01(mask + grain);
                    _vignetteTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            _vignetteTexture.Apply(false);
        }

        private float GetEdgeWave(float edgeCoord, float time)
        {
            float phase = time * waveSpeed;
            float primary = Mathf.Sin((edgeCoord * waveFrequency * Mathf.PI * 2f) + phase);
            float secondary = Mathf.Sin((edgeCoord * (waveFrequency * 1.73f) * Mathf.PI * 2f) - phase * 1.37f) * 0.55f;
            float tertiary = Mathf.Sin((edgeCoord * (waveFrequency * 0.46f) * Mathf.PI * 2f) + phase * 0.62f) * 0.35f;
            return Mathf.Clamp((primary + secondary + tertiary) * 0.5f, -1f, 1f);
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
