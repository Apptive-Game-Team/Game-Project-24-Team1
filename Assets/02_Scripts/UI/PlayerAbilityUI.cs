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
            public RectTransform cardClipRect;
            public CanvasGroup canvasGroup;
            public Image iconImage;
            public Image beamImage;
            public Image baseGlowImage;
            public Image plateImage;
            public Image selectedFrameFillImage;
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
        [SerializeField] private AbilitySlot dashSlot = new AbilitySlot { state = AbilityState.Nothing };
        [SerializeField] private AbilitySlot paralyzeSlot = new AbilitySlot { state = AbilityState.Paralyze };
        [SerializeField] private AbilitySlot madSlot = new AbilitySlot { state = AbilityState.Mad };
        [SerializeField] private AbilitySlot bombSlot = new AbilitySlot { state = AbilityState.Bomb };

        [Header("Visuals")]
        [SerializeField] private bool autoBuildSlots = true;
        [SerializeField] private bool createCanvasIfMissing = true;
        [SerializeField] private Vector2 canvasAnchoredPosition = new Vector2(320f, 190f);
        [SerializeField] private Vector2 carouselRootSize = new Vector2(1080f, 460f);
        [SerializeField] private Vector2 slotSize = new Vector2(315f, 360f);
        [SerializeField] private Color slotColor = new Color(0.07f, 0.12f, 0.17f, 0.86f);
        [SerializeField] private Color selectedRingColor = new Color(0.35f, 0.95f, 1f, 1f);
        [SerializeField] private Color beamColor = new Color(0.24f, 0.88f, 1f, 0.55f);
        [SerializeField] private Color baseGlowColor = new Color(0.15f, 0.74f, 1f, 0.8f);
        [SerializeField] private Color emptyTint = new Color(0.65f, 0.65f, 0.65f, 1f);
        [SerializeField] private float usableAlpha = 1f;
        [SerializeField] private float emptyAlpha = 0.35f;

        [Header("Carousel")]
        [SerializeField] private bool useCarouselLayout = true;
        [SerializeField] private float horizontalSpacing = 125f;
        [SerializeField] private float depthYOffset = 0f;
        [SerializeField] private float selectedScale = 1.18f;
        [SerializeField] private float sideScale = 0.92f;
        [SerializeField] private float farScale = 0.74f;
        [SerializeField] private float sideRotationY = 12f;
        [SerializeField] private float farRotationY = 0f;
        [SerializeField] private float sideAlphaMultiplier = 0.82f;
        [SerializeField] private float farAlphaMultiplier = 0.7f;
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
        private bool _initialized;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (abilityController == null)
            {
                abilityController = FindFirstObjectByType<AbilityController>();
            }

            AssignIconSprites();
            AssignLabels();
            _slots = new[] { dashSlot, paralyzeSlot, madSlot, bombSlot };

            if (!_initialized)
            {
                _lastState = abilityController != null ? abilityController.CurrentState : AbilityState.Nothing;
            }

            EnsureSlotParent();

            if (autoBuildSlots)
            {
                BuildMissingSlots();
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                EnsureSlotReferences(_slots[i]);
            }

            _initialized = true;
        }

        private void Start()
        {
            Refresh();
        }

        private void LateUpdate()
        {
            // 대시 같은 능력 사용 처리가 Update에서 끝난 뒤에 UI를 읽어야 바로 반투명 상태가 맞게 보여서 LateUpdate에서 갱신함.
            Refresh();
        }

        public void Refresh()
        {
            Initialize();

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

            // 캔버스에 직접 배치해둔 게 아니라, 시작할 때 무기 카드 4개를 코드로 만들어주는 구조.
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
            // 선택된 무기 밑에서 파란 빛이 위로 올라오는 느낌을 주는 빔 이미지.
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
            // 빔이 바닥에서 나오는 것처럼 보이게 해주는 바닥 글로우.
            slot.baseGlowImage = baseImage;

            GameObject cardClipObject = CreateUIObject("CardClip", root.transform);
            RectMask2D cardClipMask = cardClipObject.AddComponent<RectMask2D>();
            cardClipMask.padding = Vector4.zero;
            RectTransform cardClipRect = cardClipObject.GetComponent<RectTransform>();
            cardClipRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardClipRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardClipRect.pivot = new Vector2(0.5f, 0.5f);
            cardClipRect.anchoredPosition = new Vector2(0f, 42f);
            cardClipRect.sizeDelta = new Vector2(225f, 186f);
            // 뒤에 있는 카드가 중앙 카드를 침범한 부분은 여기서 잘라서, 진짜 뒤에 숨어있는 것처럼 보이게 함.
            slot.cardClipRect = cardClipRect;

            GameObject plateObject = CreateUIObject("FloatingWeaponPlate", cardClipObject.transform);
            Image plateImage = plateObject.AddComponent<Image>();
            plateImage.sprite = _plateSprite;
            plateImage.color = slotColor;
            plateImage.raycastTarget = false;
            RectTransform plateRect = plateImage.rectTransform;
            plateRect.anchorMin = new Vector2(0.5f, 0.5f);
            plateRect.anchorMax = new Vector2(0.5f, 0.5f);
            plateRect.pivot = new Vector2(0.5f, 0.5f);
            plateRect.anchoredPosition = Vector2.zero;
            plateRect.sizeDelta = new Vector2(225f, 186f);
            plateRect.localRotation = Quaternion.Euler(12f, 0f, 0f);
            // 무기 아이콘이 올라가는 어두운 카드 판. 살짝 기울여서 평면 UI가 덜 밋밋하게 보이게 함.
            slot.plateImage = plateImage;

            GameObject frameFillObject = CreateUIObject("SelectedFrameFill", plateObject.transform);
            Image frameFillImage = frameFillObject.AddComponent<Image>();
            frameFillImage.sprite = _plateSprite;
            frameFillImage.color = new Color(slotColor.r, slotColor.g, slotColor.b, 0.62f);
            frameFillImage.raycastTarget = false;
            Stretch(frameFillImage.rectTransform, Vector2.zero);
            // 선택된 카드 안쪽이 너무 뻥 뚫려 보이지 않게 파란 프레임 안을 살짝 채워주는 레이어.
            slot.selectedFrameFillImage = frameFillImage;

            GameObject frameObject = CreateUIObject("SelectedFrame", plateObject.transform);
            Image frameImage = frameObject.AddComponent<Image>();
            frameImage.sprite = _frameSprite;
            frameImage.color = selectedRingColor;
            frameImage.raycastTarget = false;
            Stretch(frameImage.rectTransform, new Vector2(-8f, -8f));
            // 현재 선택된 무기를 강조하는 파란 테두리.
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
            // 실제 무기/능력 아이콘 이미지.
            slot.iconImage = iconImage;
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
                // 대시 쿨타임이거나 자원이 없으면 카드 전체를 반투명하게 해서 지금 못 쓴다는 걸 보여줌.
                slot.canvasGroup.alpha = isEmpty ? emptyAlpha : usableAlpha;
                slot.canvasGroup.interactable = !isEmpty;
                slot.canvasGroup.blocksRaycasts = !isEmpty;
            }

            if (slot.selectedIndicator != null)
            {
                // 선택된 카드만 파란 테두리가 켜지게 함.
                slot.selectedIndicator.SetActive(abilityController.CurrentState == slot.state);
            }

            if (slot.selectedFrameFillImage != null)
            {
                slot.selectedFrameFillImage.enabled = abilityController.CurrentState == slot.state;
                slot.selectedFrameFillImage.color = new Color(slotColor.r, slotColor.g, slotColor.b, 0.62f);
            }

            if (slot.plateImage != null)
            {
                RectTransform plateRect = slot.plateImage.rectTransform;
                plateRect.sizeDelta = new Vector2(225f, 186f);
                slot.plateImage.color = slotColor;
            }

            if (slot.beamImage != null)
            {
                // 빔 효과도 선택된 무기에서만 켜짐.
                slot.beamImage.enabled = abilityController.CurrentState == slot.state;
            }

            if (slot.baseGlowImage != null)
            {
                slot.baseGlowImage.enabled = abilityController.CurrentState == slot.state;
                slot.baseGlowImage.color = baseGlowColor;
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

                int layoutIndex = GetCarouselLayoutIndex(i, selectedIndex, visibleCount);
                int depth = GetLayoutDepth(layoutIndex);
                bool isEmpty = abilityController.GetResourceCount(slot.state) <= 0;

                float scale = GetScale(layoutIndex);
                float rotationY = GetRotationY(layoutIndex);
                float positionAlpha = GetPositionAlpha(layoutIndex);
                float resourceAlpha = isEmpty ? emptyAlpha : usableAlpha;
                // 카드들이 가만히 붙어있지 않고 살짝 떠다니는 것처럼 보이게 사인파로 위아래 움직임을 줌.
                float floatOffset = Mathf.Sin((Time.unscaledTime * floatSpeed) + (i * 0.7f)) * floatAmplitude;
                // 선택된 중앙 무기는 아주 약하게 커졌다 작아지는 펄스를 줘서 현재 선택 느낌을 더 살림.
                float pulse = layoutIndex == 0 ? 1f + Mathf.Sin(Time.unscaledTime * selectedPulseSpeed) * 0.035f : 1f;

                Vector2 targetPosition = GetCarouselPosition(layoutIndex, floatOffset);
                Quaternion targetRotation = Quaternion.Euler(layoutIndex == 3 ? 0f : 4f, rotationY, GetZRotation(layoutIndex));
                Vector3 targetScale = Vector3.one * (scale * pulse);

                float lerpAmount = Time.unscaledDeltaTime * carouselLerpSpeed;
                // 바로 순간이동하지 않고 보간해서 무기들이 원형으로 돌아가는 느낌을 만듦.
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
                    color.a *= layoutIndex == 0 ? 1f : 0f;
                    slot.beamImage.color = color;
                }

                ApplyCardClip(slot, layoutIndex);

                // 중앙 선택 카드가 무조건 제일 앞에 그려지게 해서 뒤 카드들이 중앙 카드를 덮지 못하게 함.
                slot.rectTransform.SetSiblingIndex(layoutIndex == 0 ? 100 : Mathf.Max(0, 2 - depth));
            }
        }

        private void ApplyCardClip(AbilitySlot slot, int layoutIndex)
        {
            if (slot.cardClipRect == null || slot.plateImage == null) return;

            RectTransform clipRect = slot.cardClipRect;
            RectTransform plateRect = slot.plateImage.rectTransform;

            switch (layoutIndex)
            {
                case 1:
                    // 오른쪽 뒤 카드: 중앙 카드 안쪽으로 들어간 부분은 마스크로 잘라서 뒤에 숨어있는 것처럼 보이게 함.
                    clipRect.anchoredPosition = new Vector2(22f, 42f);
                    clipRect.sizeDelta = new Vector2(190f, 186f);
                    plateRect.anchoredPosition = new Vector2(-22f, 0f);
                    break;
                case 2:
                    // 왼쪽 뒤 카드도 오른쪽과 반대로 같은 방식으로 잘라줌.
                    clipRect.anchoredPosition = new Vector2(-22f, 42f);
                    clipRect.sizeDelta = new Vector2(190f, 186f);
                    plateRect.anchoredPosition = new Vector2(22f, 0f);
                    break;
                case 3:
                    // 아래 뒤 카드는 윗부분이 중앙 카드 뒤로 들어가고, 아이콘만 살짝 보이는 느낌으로 창을 낮고 얇게 둠.
                    clipRect.anchoredPosition = new Vector2(0f, -22f);
                    clipRect.sizeDelta = new Vector2(218f, 88f);
                    plateRect.anchoredPosition = Vector2.zero;
                    break;
                default:
                    clipRect.anchoredPosition = new Vector2(0f, 42f);
                    clipRect.sizeDelta = new Vector2(225f, 186f);
                    plateRect.anchoredPosition = Vector2.zero;
                    break;
            }

            plateRect.sizeDelta = new Vector2(225f, 186f);
        }

        private void SnapSelectedSlotForward()
        {
            if (_slots == null) return;

            for (int i = 0; i < _slots.Length; i++)
            {
                AbilitySlot slot = _slots[i];
                if (slot == null || slot.rectTransform == null || slot.state != _lastState) continue;

                // 무기 바꿀 때 선택된 카드가 살짝 앞으로 튀어나오는 느낌을 주는 한 번짜리 보정.
                slot.rectTransform.localScale = Vector3.one * (selectedScale * 0.86f);
                slot.rectTransform.anchoredPosition += new Vector2(_lastSwitchDirection * 120f, 36f);
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
                case AbilityState.Nothing:
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
                case AbilityState.Nothing:
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
                case AbilityState.Nothing:
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

        private int GetCarouselLayoutIndex(int index, int selectedIndex, int visibleCount)
        {
            // 현재 선택된 무기를 기준으로 0=중앙, 1=오른쪽, 2=왼쪽, 3=아래 뒤쪽 위치를 정함.
            int forwardOffset = (index - selectedIndex + visibleCount) % visibleCount;

            if (visibleCount <= 3)
            {
                if (forwardOffset == 0) return 0;
                return forwardOffset == 1 ? 1 : 2;
            }

            if (forwardOffset == 0) return 0;
            if (forwardOffset == 1) return 1;
            if (forwardOffset == 2) return 2;
            return 3;
        }

        private Vector2 GetCarouselPosition(int layoutIndex, float floatOffset)
        {
            switch (layoutIndex)
            {
                case 0:
                    // 선택된 무기는 중앙 위쪽에 크게 배치.
                    return new Vector2(0f, 78f + floatOffset);
                case 1:
                    // 다음 무기는 오른쪽 뒤에 배치.
                    return new Vector2(horizontalSpacing, 40f + (floatOffset * 0.28f));
                case 2:
                    // 이전 쪽 무기는 왼쪽 뒤에 배치.
                    return new Vector2(-horizontalSpacing, 40f + (floatOffset * 0.28f));
                default:
                    // 마지막 무기는 아래 뒤쪽에 살짝 보이게 배치.
                    return new Vector2(0f, -depthYOffset + (floatOffset * 0.12f));
            }
        }

        private int GetLayoutDepth(int layoutIndex)
        {
            switch (layoutIndex)
            {
                case 0:
                    return 0;
                case 1:
                case 2:
                    return 1;
                default:
                    return 2;
            }
        }

        private float GetScale(int layoutIndex)
        {
            if (layoutIndex == 0) return selectedScale;
            if (layoutIndex == 1 || layoutIndex == 2) return sideScale;
            return farScale;
        }

        private float GetRotationY(int layoutIndex)
        {
            if (layoutIndex == 1) return -sideRotationY;
            if (layoutIndex == 2) return sideRotationY;
            return farRotationY;
        }

        private float GetZRotation(int layoutIndex)
        {
            if (layoutIndex == 1) return -2f;
            if (layoutIndex == 2) return 2f;
            return 0f;
        }

        private float GetPositionAlpha(int layoutIndex)
        {
            if (layoutIndex == 0) return 1f;
            if (layoutIndex == 1 || layoutIndex == 2) return sideAlphaMultiplier;
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
                _plateSprite = CreateRoundedRectSprite(128, 96, 4, false);
            }

            if (_frameSprite == null)
            {
                _frameSprite = CreateRoundedRectSprite(136, 104, 5, true);
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
