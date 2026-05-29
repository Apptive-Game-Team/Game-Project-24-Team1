using System;
using UnityEngine;
using UnityEngine.UI;

namespace MushOut.UI
{
    [DisallowMultipleComponent]
    public class MemorySporeUI : MonoBehaviour
    {
        public static MemorySporeUI Instance { get; private set; }

        [Header("Resource")]
        [SerializeField] private int memorySporeCount;

        [Header("Visuals")]
        [SerializeField] private Sprite memorySporeIcon;
        [SerializeField] private Vector2 anchoredPosition = new Vector2(-76f, 70f);
        [SerializeField] private Vector2 panelSize = new Vector2(330f, 132f);
        [SerializeField] private Vector2 iconSize = new Vector2(126f, 126f);
        [SerializeField] private Color panelColor = new Color(0.08f, 0.08f, 0.08f, 0.68f);
        [SerializeField] private Color textColor = Color.white;

        private Text _countText;

        public event Action<int> CountChanged;

        public int MemorySporeCount
        {
            get => memorySporeCount;
            private set
            {
                int nextValue = Mathf.Max(0, value);
                if (memorySporeCount == nextValue) return;

                memorySporeCount = nextValue;
                Refresh();
                CountChanged?.Invoke(memorySporeCount);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            BuildIfMissing();
            Refresh();
        }

        private void OnEnable()
        {
            BuildIfMissing();
            Refresh();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void AddMemorySpores(int amount)
        {
            if (amount <= 0) return;
            MemorySporeCount += amount;
        }

        public bool TryUseMemorySpore(int amount = 1)
        {
            if (amount <= 0) return true;
            if (memorySporeCount < amount) return false;

            MemorySporeCount -= amount;
            return true;
        }

        public void SetMemorySpores(int amount)
        {
            MemorySporeCount = amount;
        }

        private void BuildIfMissing()
        {
            if (_countText != null) return;

            Transform existing = transform.Find("MemorySporeCounter");
            if (existing != null)
            {
                _countText = existing.GetComponentInChildren<Text>(true);
                return;
            }

            GameObject panelObject = new GameObject("MemorySporeCounter", typeof(RectTransform));
            panelObject.transform.SetParent(transform, false);

            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = panelColor;
            panelImage.raycastTarget = false;

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(1f, 0f);
            panelRect.anchoredPosition = anchoredPosition;
            panelRect.sizeDelta = panelSize;

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform));
            iconObject.transform.SetParent(panelObject.transform, false);

            Image iconImage = iconObject.AddComponent<Image>();
            iconImage.sprite = memorySporeIcon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            RectTransform iconRect = iconImage.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(78f, 0f);
            iconRect.sizeDelta = iconSize;

            GameObject textObject = new GameObject("Count", typeof(RectTransform));
            textObject.transform.SetParent(panelObject.transform, false);

            _countText = textObject.AddComponent<Text>();
            _countText.alignment = TextAnchor.MiddleLeft;
            _countText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _countText.fontSize = 68;
            _countText.fontStyle = FontStyle.Bold;
            _countText.color = textColor;
            _countText.raycastTarget = false;

            RectTransform textRect = _countText.rectTransform;
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(158f, 0f);
            textRect.offsetMax = new Vector2(-22f, 0f);
        }

        private void Refresh()
        {
            if (_countText != null)
            {
                _countText.text = $"x{memorySporeCount}";
            }
        }
    }
}
