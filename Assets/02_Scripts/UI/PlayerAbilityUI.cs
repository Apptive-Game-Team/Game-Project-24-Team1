using System;
using MushOut.Player;
using UnityEngine;
using UnityEngine.UI;

namespace MushOut.UI
{
    public class PlayerAbilityUI : MonoBehaviour
    {
        [Serializable]
        private class AbilitySlot
        {
            public AbilityState state;
            public string displayName;
            public string keyLabel;
            public Sprite iconSprite;
            public GameObject slotRoot;
            public RectTransform rectTransform;
            public CanvasGroup canvasGroup;
            public Image iconImage;
            public Image beamImage;
            public Image baseGlowImage;
            public Image plateImage;
            public GameObject selectedIndicator;
            public Text keyText;
            public Text nameText;
            public Text countText;
        }

        [Header("References")]
        [SerializeField] private AbilityController abilityController;

        [Header("Icons")]
        [SerializeField] private Sprite dashIcon;
        [SerializeField] private Sprite sleepSporeIcon;
        [SerializeField] private Sprite provocationIcon;
        [SerializeField] private Sprite bombSporeIcon;

        [Header("Slots")]
        [SerializeField] private AbilitySlot dashSlot = new AbilitySlot { state = AbilityState.Dash };
        [SerializeField] private AbilitySlot paralyzeSlot = new AbilitySlot { state = AbilityState.Paralyze };
        [SerializeField] private AbilitySlot madSlot = new AbilitySlot { state = AbilityState.Mad };
        [SerializeField] private AbilitySlot bombSlot = new AbilitySlot { state = AbilityState.Bomb };

        [Header("Visuals")]
        [SerializeField] private bool autoBuildSlots = true;
        [SerializeField] private bool createCanvasIfMissing = true;
        [SerializeField] private Vector2 canvasAnchoredPosition = new Vector2(320f, 190f);
        [SerializeField] private Vector2 carouselRootSize = new Vector2(1080f, 390f);
        [SerializeField] private Vector2 slotSize = new Vector2(285f, 315f);
        [SerializeField] private Color slotColor = new Color(0.07f, 0.12f, 0.17f, 0.86f);
        [SerializeField] private Color selectedRingColor = new Color(0.35f, 0.95f, 1f, 1f);
        [SerializeField] private Color beamColor = new Color(0.24f, 0.88f, 1f, 0.55f);
        [SerializeField] private Color baseGlowColor = new Color(0.15f, 0.74f, 1f, 0.8f);
        [SerializeField] private Color emptyTint = new Color(0.65f, 0.65f, 0.65f, 1f);
        [SerializeField] private float usableAlpha = 1f;
        [SerializeField] private float emptyAlpha = 0.35f;

        [Header("Carousel")]
        [SerializeField] private bool useCarouselLayout = true;
        [SerializeField] private float horizontalSpacing = 285f;
        [SerializeField] private float depthYOffset = 39f;
        [SerializeField] private float selectedScale = 1.25f;
        [SerializeField] private float sideScale = 0.82f;
        [SerializeField] private float farScale = 0.56f;
        [SerializeField] private float sideRotationY = 48f;
        [SerializeField] private float farRotationY = 64f;
        [SerializeField] private float sideAlphaMultiplier = 0.75f;
        [SerializeField] private float farAlphaMultiplier = 0.45f;
        [SerializeField] private float carouselLerpSpeed = 12f;
        [SerializeField] private float floatAmplitude = 13.5f;
        [SerializeField] private float floatSpeed = 2.4f;
        [SerializeField] private float selectedPulseSpeed = 4f;

        private static Sprite _plateSprite;
        private static Sprite _frameSprite;
        private static Sprite _softDiscSprite;
        private static Sprite _beamSprite;
        private readonly AbilitySlot[] _visibleSlots = new AbilitySlot[4];
        private AbilitySlot[] _slots;
        private Transform _slotParent;
        private AbilityState _lastState;
        private int _lastSwitchDirection = 1;

        private void Awake()
        {
            if (abilityController == null)
            {
                abilityController = FindFirstObjectByType<AbilityController>();
            }

            AssignIconSprites();
            AssignLabels();
            _slots = new[] { dashSlot, paralyzeSlot, madSlot, bombSlot };
            _lastState = abilityController != null ? abilityController.CurrentState : AbilityState.Dash;
            EnsureSlotParent();

            if (autoBuildSlots)
            {
                BuildMissingSlots();
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                EnsureSlotReferences(_slots[i]);
            }
        }

        private void Start()
        {
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (abilityController == null) return;

            EnsureRuntimeSlots();

            if (_lastState != abilityController.CurrentState)
            {
                _lastSwitchDirection = GetSwitchDirection(_lastState, abilityController.CurrentState);
                _lastState = abilityController.CurrentState;
                SnapSelectedSlotForward();
            }

            ApplySingleDisplayFallback();

            for (int i = 0; i < _slots.Length; i++)
            {
                RefreshSlot(_slots[i]);
            }

            if (useCarouselLayout)
            {
                ApplyCarouselLayout();
            }

            ApplySingleDisplayVisuals();
        }

        private void ApplySingleDisplayFallback()
        {
            if (dashSlot == null || dashSlot.slotRoot == null) return;
            if (paralyzeSlot.slotRoot != null && madSlot.slotRoot != null && bombSlot.slotRoot != null) return;

            AbilityState state = abilityController.CurrentState;
            dashSlot.state = state;
            dashSlot.iconSprite = GetIconForState(state);
            dashSlot.displayName = GetDisplayNameForState(state);
            dashSlot.keyLabel = GetKeyLabelForState(state);
        }

        private void ApplySingleDisplayVisuals()
        {
            if (dashSlot == null || dashSlot.slotRoot == null) return;
            if (paralyzeSlot.slotRoot != null && madSlot.slotRoot != null && bombSlot.slotRoot != null) return;

            AbilityState state = abilityController.CurrentState;
            bool isEmpty = abilityController.GetResourceCount(state) <= 0;

            if (dashSlot.iconImage != null)
            {
                dashSlot.iconImage.sprite = GetIconForState(state);
                dashSlot.iconImage.overrideSprite = GetIconForState(state);
                dashSlot.iconImage.color = isEmpty ? emptyTint : Color.white;
            }

            if (dashSlot.keyText != null)
            {
                dashSlot.keyText.text = GetKeyLabelForState(state);
            }

            if (dashSlot.nameText != null)
            {
                dashSlot.nameText.text = GetDisplayNameForState(state);
            }

            if (dashSlot.countText != null)
            {
                dashSlot.countText.text = $"x{abilityController.GetResourceCount(state)}";
            }

            if (dashSlot.selectedIndicator != null)
            {
                dashSlot.selectedIndicator.SetActive(true);
            }

            if (dashSlot.beamImage != null)
            {
                dashSlot.beamImage.enabled = true;
                dashSlot.beamImage.color = beamColor;
            }

            if (dashSlot.baseGlowImage != null)
            {
                dashSlot.baseGlowImage.color = baseGlowColor;
            }
        }

        private void EnsureRuntimeSlots()
        {
            if (!autoBuildSlots || _slots == null) return;

            bool hasMissingSlot = false;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null && _slots[i].slotRoot == null)
                {
                    hasMissingSlot = true;
                    break;
                }
            }

            if (!hasMissingSlot) return;

            BuildMissingSlots();

            for (int i = 0; i < _slots.Length; i++)
            {
                EnsureSlotReferences(_slots[i]);
            }
        }

        private void AssignIconSprites()
        {
            if (dashIcon == null) dashIcon = Resources.Load<Sprite>("AbilityIcons/dash");
            if (sleepSporeIcon == null) sleepSporeIcon = Resources.Load<Sprite>("AbilityIcons/sleep_spore");
            if (provocationIcon == null) provocationIcon = Resources.Load<Sprite>("AbilityIcons/Provocation");
            if (bombSporeIcon == null) bombSporeIcon = Resources.Load<Sprite>("AbilityIcons/boom");

            dashSlot.iconSprite = dashIcon;
            paralyzeSlot.iconSprite = sleepSporeIcon;
            madSlot.iconSprite = provocationIcon;
            bombSlot.iconSprite = bombSporeIcon;
        }

        private void AssignLabels()
        {
            dashSlot.displayName = "DASH";
            dashSlot.keyLabel = "1";
            paralyzeSlot.displayName = "SLEEP SPORE";
            paralyzeSlot.keyLabel = "2";
            madSlot.displayName = "TAUNT SPORE";
            madSlot.keyLabel = "3";
            bombSlot.displayName = "BOMB SPORE";
            bombSlot.keyLabel = "4";
        }

        private void EnsureSlotParent()
        {
            _slotParent = transform;

            if (!createCanvasIfMissing) return;
            if (transform is RectTransform && GetComponentInParent<Canvas>() != null) return;

            GameObject canvasObject = GameObject.Find("AbilityUICanvas");
            if (canvasObject == null)
            {
                canvasObject = new GameObject("AbilityUICanvas", typeof(RectTransform));
                Canvas canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 50;

                CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                canvasObject.AddComponent<GraphicRaycaster>();
            }

            GameObject rootObject = GameObject.Find("AbilityCarousel");
            if (rootObject == null)
            {
                rootObject = CreateUIObject("AbilityCarousel", canvasObject.transform);
            }
            else
            {
                rootObject.transform.SetParent(canvasObject.transform, false);
            }

            RectTransform rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 0f);
            rootRect.anchorMax = new Vector2(0f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = canvasAnchoredPosition;
            rootRect.sizeDelta = carouselRootSize;

            _slotParent = rootObject.transform;
        }

        private void BuildMissingSlots()
        {
            EnsureGeneratedSprites();
            BuildSlotIfMissing(dashSlot, "01_Dash");
            BuildSlotIfMissing(paralyzeSlot, "02_SleepSpore");
            BuildSlotIfMissing(madSlot, "03_Provocation");
            BuildSlotIfMissing(bombSlot, "04_BombSpore");
        }

        private void BuildSlotIfMissing(AbilitySlot slot, string slotName)
        {
            if (slot == null || slot.slotRoot != null) return;

            GameObject root = CreateUIObject(slotName, _slotParent);
            slot.slotRoot = root;
            slot.rectTransform = root.GetComponent<RectTransform>();
            slot.rectTransform.sizeDelta = slotSize;

            slot.canvasGroup = root.AddComponent<CanvasGroup>();

            GameObject beamObject = CreateUIObject("ProjectionBeam", root.transform);
            Image beamImage = beamObject.AddComponent<Image>();
            beamImage.sprite = _beamSprite;
            beamImage.color = beamColor;
            beamImage.raycastTarget = false;
            RectTransform beamRect = beamImage.rectTransform;
            beamRect.anchorMin = new Vector2(0.5f, 0f);
            beamRect.anchorMax = new Vector2(0.5f, 0f);
            beamRect.pivot = new Vector2(0.5f, 0f);
            beamRect.anchoredPosition = new Vector2(0f, 42f);
            beamRect.sizeDelta = new Vector2(232.5f, 225f);
            slot.beamImage = beamImage;

            GameObject baseObject = CreateUIObject("ProjectorBaseGlow", root.transform);
            Image baseImage = baseObject.AddComponent<Image>();
            baseImage.sprite = _softDiscSprite;
            baseImage.color = baseGlowColor;
            baseImage.raycastTarget = false;
            RectTransform baseRect = baseImage.rectTransform;
            baseRect.anchorMin = new Vector2(0.5f, 0f);
            baseRect.anchorMax = new Vector2(0.5f, 0f);
            baseRect.pivot = new Vector2(0.5f, 0.5f);
            baseRect.anchoredPosition = new Vector2(0f, 39f);
            baseRect.sizeDelta = new Vector2(237f, 63f);
            slot.baseGlowImage = baseImage;

            GameObject plateObject = CreateUIObject("FloatingWeaponPlate", root.transform);
            Image plateImage = plateObject.AddComponent<Image>();
            plateImage.sprite = _plateSprite;
            plateImage.color = slotColor;
            plateImage.raycastTarget = false;
            RectTransform plateRect = plateImage.rectTransform;
            plateRect.anchorMin = new Vector2(0.5f, 0.5f);
            plateRect.anchorMax = new Vector2(0.5f, 0.5f);
            plateRect.pivot = new Vector2(0.5f, 0.5f);
            plateRect.anchoredPosition = new Vector2(0f, 42f);
            plateRect.sizeDelta = new Vector2(225f, 186f);
            plateRect.localRotation = Quaternion.Euler(12f, 0f, 0f);
            slot.plateImage = plateImage;

            GameObject frameObject = CreateUIObject("SelectedFrame", plateObject.transform);
            Image frameImage = frameObject.AddComponent<Image>();
            frameImage.sprite = _frameSprite;
            frameImage.color = selectedRingColor;
            frameImage.raycastTarget = false;
            Stretch(frameImage.rectTransform, new Vector2(-8f, -8f));
            slot.selectedIndicator = frameObject;

            GameObject iconObject = CreateUIObject("WeaponIcon", plateObject.transform);
            Image iconImage = iconObject.AddComponent<Image>();
            iconImage.sprite = slot.iconSprite;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            RectTransform iconRect = iconImage.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 15f);
            iconRect.sizeDelta = new Vector2(144f, 108f);
            slot.iconImage = iconImage;

            GameObject keyObject = CreateUIObject("KeyLabel", plateObject.transform);
            Text keyText = keyObject.AddComponent<Text>();
            keyText.alignment = TextAnchor.MiddleCenter;
            keyText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            keyText.fontSize = 30;
            keyText.fontStyle = FontStyle.Bold;
            keyText.color = selectedRingColor;
            keyText.raycastTarget = false;
            RectTransform keyRect = keyText.rectTransform;
            keyRect.anchorMin = new Vector2(0f, 1f);
            keyRect.anchorMax = new Vector2(0f, 1f);
            keyRect.pivot = new Vector2(0.5f, 0.5f);
            keyRect.anchoredPosition = new Vector2(25.5f, -25.5f);
            keyRect.sizeDelta = new Vector2(42f, 42f);
            slot.keyText = keyText;

            GameObject nameObject = CreateUIObject("AbilityName", plateObject.transform);
            Text nameText = nameObject.AddComponent<Text>();
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameText.fontSize = 19;
            nameText.fontStyle = FontStyle.Bold;
            nameText.color = new Color(0.83f, 0.98f, 1f, 0.95f);
            nameText.raycastTarget = false;
            RectTransform nameRect = nameText.rectTransform;
            nameRect.anchorMin = new Vector2(0.5f, 0f);
            nameRect.anchorMax = new Vector2(0.5f, 0f);
            nameRect.pivot = new Vector2(0.5f, 0.5f);
            nameRect.anchoredPosition = new Vector2(0f, 27f);
            nameRect.sizeDelta = new Vector2(192f, 33f);
            slot.nameText = nameText;

            GameObject countObject = CreateUIObject("Count", root.transform);
            Text countText = countObject.AddComponent<Text>();
            countText.alignment = TextAnchor.MiddleCenter;
            countText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            countText.fontSize = 24;
            countText.fontStyle = FontStyle.Bold;
            countText.color = new Color(0.9f, 1f, 1f, 1f);
            countText.raycastTarget = false;

            RectTransform countRect = countText.rectTransform;
            countRect.anchorMin = new Vector2(0.5f, 0f);
            countRect.anchorMax = new Vector2(0.5f, 0f);
            countRect.pivot = new Vector2(0.5f, 0.5f);
            countRect.anchoredPosition = new Vector2(0f, 4.5f);
            countRect.sizeDelta = new Vector2(120f, 36f);
            slot.countText = countText;
        }

        private void RefreshSlot(AbilitySlot slot)
        {
            if (slot == null || slot.slotRoot == null) return;

            bool unlocked = abilityController.IsUnlocked(slot.state);
            slot.slotRoot.SetActive(unlocked);

            if (!unlocked) return;

            int count = abilityController.GetResourceCount(slot.state);
            bool isEmpty = count <= 0;

            if (slot.iconImage != null)
            {
                slot.iconImage.sprite = slot.iconSprite;
                slot.iconImage.color = isEmpty ? emptyTint : Color.white;
            }

            if (slot.canvasGroup != null)
            {
                slot.canvasGroup.alpha = isEmpty ? emptyAlpha : usableAlpha;
                slot.canvasGroup.interactable = !isEmpty;
                slot.canvasGroup.blocksRaycasts = !isEmpty;
            }

            if (slot.selectedIndicator != null)
            {
                slot.selectedIndicator.SetActive(abilityController.CurrentState == slot.state);
            }

            if (slot.beamImage != null)
            {
                slot.beamImage.enabled = abilityController.CurrentState == slot.state;
            }

            if (slot.baseGlowImage != null)
            {
                slot.baseGlowImage.color = abilityController.CurrentState == slot.state
                    ? baseGlowColor
                    : new Color(baseGlowColor.r, baseGlowColor.g, baseGlowColor.b, 0.34f);
            }

            if (slot.keyText != null)
            {
                slot.keyText.text = slot.keyLabel;
            }

            if (slot.nameText != null)
            {
                slot.nameText.text = slot.displayName;
            }

            if (slot.countText != null)
            {
                slot.countText.text = $"x{count}";
            }
        }

        private void ApplyCarouselLayout()
        {
            int visibleCount = 0;

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null && _slots[i].slotRoot != null && _slots[i].slotRoot.activeSelf)
                {
                    _visibleSlots[visibleCount] = _slots[i];
                    visibleCount++;
                }
            }

            if (visibleCount == 0) return;

            int selectedIndex = GetSelectedIndex(visibleCount);

            for (int i = 0; i < visibleCount; i++)
            {
                AbilitySlot slot = _visibleSlots[i];
                if (slot.rectTransform == null) continue;

                int offset = GetCircularOffset(i, selectedIndex, visibleCount);
                int distance = Mathf.Abs(offset);
                bool isEmpty = abilityController.GetResourceCount(slot.state) <= 0;

                float scale = GetScale(distance);
                float rotationY = GetRotationY(offset, distance);
                float positionAlpha = GetPositionAlpha(distance);
                float resourceAlpha = isEmpty ? emptyAlpha : usableAlpha;
                float floatOffset = Mathf.Sin((Time.unscaledTime * floatSpeed) + (i * 0.7f)) * floatAmplitude;
                float pulse = distance == 0 ? 1f + Mathf.Sin(Time.unscaledTime * selectedPulseSpeed) * 0.04f : 1f;

                Vector2 targetPosition = new Vector2(offset * horizontalSpacing, -distance * depthYOffset + floatOffset);
                Quaternion targetRotation = Quaternion.Euler(0f, rotationY, offset * -5f);
                Vector3 targetScale = Vector3.one * (scale * pulse);

                float lerpAmount = Time.unscaledDeltaTime * carouselLerpSpeed;
                slot.rectTransform.anchoredPosition = Vector2.Lerp(slot.rectTransform.anchoredPosition, targetPosition, lerpAmount);
                slot.rectTransform.localRotation = Quaternion.Lerp(slot.rectTransform.localRotation, targetRotation, lerpAmount);
                slot.rectTransform.localScale = Vector3.Lerp(slot.rectTransform.localScale, targetScale, lerpAmount);

                if (slot.canvasGroup != null)
                {
                    slot.canvasGroup.alpha = resourceAlpha * positionAlpha;
                }

                if (slot.beamImage != null)
                {
                    Color color = beamColor;
                    color.a *= distance == 0 ? 1f : 0.18f;
                    slot.beamImage.color = color;
                }

                slot.rectTransform.SetSiblingIndex(Mathf.Max(0, 10 - distance));
            }
        }

        private void SnapSelectedSlotForward()
        {
            if (_slots == null) return;

            for (int i = 0; i < _slots.Length; i++)
            {
                AbilitySlot slot = _slots[i];
                if (slot == null || slot.rectTransform == null || slot.state != _lastState) continue;

                slot.rectTransform.localScale = Vector3.one * (selectedScale * 0.8f);
                slot.rectTransform.anchoredPosition += new Vector2(_lastSwitchDirection * 129f, 42f);
                break;
            }
        }

        private int GetSwitchDirection(AbilityState previous, AbilityState next)
        {
            int previousIndex = GetStateIndex(previous);
            int nextIndex = GetStateIndex(next);
            int delta = nextIndex - previousIndex;

            if (Mathf.Abs(delta) > 2)
            {
                delta = -Mathf.Sign(delta) > 0f ? 1 : -1;
            }

            return delta >= 0 ? 1 : -1;
        }

        private int GetStateIndex(AbilityState state)
        {
            switch (state)
            {
                case AbilityState.Dash:
                    return 0;
                case AbilityState.Paralyze:
                    return 1;
                case AbilityState.Mad:
                    return 2;
                case AbilityState.Bomb:
                    return 3;
                default:
                    return 0;
            }
        }

        private Sprite GetIconForState(AbilityState state)
        {
            switch (state)
            {
                case AbilityState.Dash:
                    return dashIcon;
                case AbilityState.Paralyze:
                    return sleepSporeIcon;
                case AbilityState.Mad:
                    return provocationIcon;
                case AbilityState.Bomb:
                    return bombSporeIcon;
                default:
                    return dashIcon;
            }
        }

        private string GetDisplayNameForState(AbilityState state)
        {
            switch (state)
            {
                case AbilityState.Dash:
                    return "DASH";
                case AbilityState.Paralyze:
                    return "SLEEP SPORE";
                case AbilityState.Mad:
                    return "TAUNT SPORE";
                case AbilityState.Bomb:
                    return "BOMB SPORE";
                default:
                    return "DASH";
            }
        }

        private string GetKeyLabelForState(AbilityState state)
        {
            return (GetStateIndex(state) + 1).ToString();
        }

        private int GetSelectedIndex(int visibleCount)
        {
            for (int i = 0; i < visibleCount; i++)
            {
                if (_visibleSlots[i].state == abilityController.CurrentState)
                {
                    return i;
                }
            }

            return 0;
        }

        private int GetCircularOffset(int index, int selectedIndex, int visibleCount)
        {
            int offset = index - selectedIndex;
            int halfCount = visibleCount / 2;

            if (offset > halfCount)
            {
                offset -= visibleCount;
            }
            else if (offset < -halfCount)
            {
                offset += visibleCount;
            }

            return offset;
        }

        private float GetScale(int distance)
        {
            if (distance == 0) return selectedScale;
            if (distance == 1) return sideScale;
            return farScale;
        }

        private float GetRotationY(int offset, int distance)
        {
            if (distance == 0) return 0f;

            float rotation = distance == 1 ? sideRotationY : farRotationY;
            return -Mathf.Sign(offset) * rotation;
        }

        private float GetPositionAlpha(int distance)
        {
            if (distance == 0) return 1f;
            if (distance == 1) return sideAlphaMultiplier;
            return farAlphaMultiplier;
        }

        private void EnsureSlotReferences(AbilitySlot slot)
        {
            if (slot == null || slot.slotRoot == null) return;

            if (slot.rectTransform == null)
            {
                slot.rectTransform = slot.slotRoot.GetComponent<RectTransform>();
            }

            if (slot.canvasGroup == null)
            {
                slot.canvasGroup = slot.slotRoot.GetComponent<CanvasGroup>();
                if (slot.canvasGroup == null)
                {
                    slot.canvasGroup = slot.slotRoot.AddComponent<CanvasGroup>();
                }
            }
        }

        private static GameObject CreateUIObject(string objectName, Transform parent)
        {
            GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
            uiObject.transform.SetParent(parent, false);
            return uiObject;
        }

        private static void Stretch(RectTransform rectTransform, Vector2 padding)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = -padding;
            rectTransform.offsetMax = padding;
        }

        private static void EnsureGeneratedSprites()
        {
            if (_plateSprite == null)
            {
                _plateSprite = CreateRoundedRectSprite(96, 96, 14, false);
            }

            if (_frameSprite == null)
            {
                _frameSprite = CreateRoundedRectSprite(112, 112, 18, true);
            }

            if (_softDiscSprite == null)
            {
                _softDiscSprite = CreateSoftDiscSprite(128, 42);
            }

            if (_beamSprite == null)
            {
                _beamSprite = CreateBeamSprite(128, 160);
            }
        }

        private static Sprite CreateRoundedRectSprite(int width, int height, int radius, bool frameOnly)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = frameOnly ? "Generated Hologram Frame" : "Generated Hologram Plate";

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool inside = IsInsideRoundedRect(x, y, width, height, radius);
                    bool inner = IsInsideRoundedRect(x, y, width, height, radius, 6);
                    texture.SetPixel(x, y, inside && (!frameOnly || !inner) ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static bool IsInsideRoundedRect(int x, int y, int width, int height, int radius, int inset = 0)
        {
            int left = inset;
            int right = width - 1 - inset;
            int bottom = inset;
            int top = height - 1 - inset;

            if (x < left || x > right || y < bottom || y > top) return false;

            int cornerRadius = Mathf.Max(1, radius - inset);
            int cx = x < left + cornerRadius ? left + cornerRadius : x > right - cornerRadius ? right - cornerRadius : x;
            int cy = y < bottom + cornerRadius ? bottom + cornerRadius : y > top - cornerRadius ? top - cornerRadius : y;
            int dx = x - cx;
            int dy = y - cy;
            return (dx * dx) + (dy * dy) <= cornerRadius * cornerRadius;
        }

        private static Sprite CreateSoftDiscSprite(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "Generated Projection Base Glow";

            Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
            float radiusX = width * 0.48f;
            float radiusY = height * 0.44f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (x - center.x) / radiusX;
                    float dy = (y - center.y) / radiusY;
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float alpha = Mathf.Clamp01(1f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateBeamSprite(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "Generated Projection Beam";
            float centerX = (width - 1) * 0.5f;

            for (int y = 0; y < height; y++)
            {
                float t = y / (float)(height - 1);
                float halfWidth = Mathf.Lerp(width * 0.16f, width * 0.49f, t);
                float verticalFade = Mathf.Sin(t * Mathf.PI);

                for (int x = 0; x < width; x++)
                {
                    float horizontal = Mathf.Abs(x - centerX) / halfWidth;
                    float alpha = Mathf.Clamp01(1f - horizontal) * verticalFade * 0.85f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0f), 100f);
        }
    }
}
