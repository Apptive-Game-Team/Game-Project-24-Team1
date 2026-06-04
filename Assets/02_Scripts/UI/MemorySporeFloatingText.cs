using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MushOut.UI
{
    public class MemorySporeFloatingText : MonoBehaviour
    {
        private const float DefaultDuration = 60f;

        [SerializeField] private string message = "기억이 흘러들어온다.";
        [SerializeField] private float duration = DefaultDuration;
        [SerializeField] private float floatDistance = 0.45f;
        [SerializeField] private float fontSize = 36f;
        [SerializeField] private Vector2 textBoxSize = new Vector2(520f, 150f);
        [SerializeField] private Vector3 worldScale = Vector3.one * 0.01f;
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.55f);
        [SerializeField] private TMP_FontAsset fontAsset;

        private CanvasGroup _canvasGroup;
        private Camera _camera;
        private Transform _followTarget;
        private Collider _followCollider;
        private Vector3 _followOffset;
        private Vector3 _animationOffset;

        public static MemorySporeFloatingText Show(string message, Vector3 position, float duration, TMP_FontAsset fontAsset)
        {
            GameObject textObject = new GameObject("MemorySporeFloatingText", typeof(RectTransform));
            textObject.transform.position = position;

            MemorySporeFloatingText floatingText = textObject.AddComponent<MemorySporeFloatingText>();
            floatingText.message = message;
            floatingText.duration = duration > 0f ? duration : DefaultDuration;
            floatingText.fontAsset = fontAsset;
            floatingText.Begin();
            return floatingText;
        }

        public static MemorySporeFloatingText Show(string message, Transform followTarget, Vector3 offset, float duration, TMP_FontAsset fontAsset)
        {
            Vector3 position = followTarget != null ? followTarget.position + offset : offset;
            MemorySporeFloatingText floatingText = Show(message, position, duration, fontAsset);
            floatingText._followTarget = followTarget;
            floatingText._followOffset = offset;
            return floatingText;
        }

        public static MemorySporeFloatingText Show(string message, Transform followTarget, Collider followCollider, Vector3 offset, float duration, TMP_FontAsset fontAsset)
        {
            Vector3 position = GetFollowPosition(followTarget, followCollider, offset, Vector3.zero);
            MemorySporeFloatingText floatingText = Show(message, position, duration, fontAsset);
            floatingText._followTarget = followTarget;
            floatingText._followCollider = followCollider;
            floatingText._followOffset = offset;
            floatingText.AttachToFollowTarget(position);
            return floatingText;
        }

        private void Begin()
        {
            _camera = Camera.main;

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            gameObject.AddComponent<GraphicRaycaster>().enabled = false;
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = textBoxSize;
            canvasRect.localScale = worldScale;

            GameObject panelObject = new GameObject("Panel", typeof(RectTransform));
            panelObject.transform.SetParent(transform, false);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image background = panelObject.AddComponent<Image>();
            background.color = backgroundColor;
            background.raycastTarget = false;

            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(panelObject.transform, false);

            Text text = textObject.AddComponent<Text>();
            text.text = message;
            text.fontSize = Mathf.RoundToInt(fontSize);
            text.color = textColor;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            Font uiFont = fontAsset != null ? fontAsset.sourceFontFile : null;
            if (uiFont != null)
            {
                text.font = uiFont;
            }

            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(24f, 14f);
            textRect.offsetMax = new Vector2(-24f, -14f);

            StartCoroutine(AnimateRoutine());
            EnableEditorPauseGuard();
        }

        private void OnDestroy()
        {
            DisableEditorPauseGuard();
        }

        private void AttachToFollowTarget(Vector3 worldPosition)
        {
            Transform parent = _followCollider != null ? _followCollider.transform : _followTarget;
            if (parent == null) return;

            transform.SetParent(parent, true);
            transform.position = worldPosition;
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera != null)
            {
                transform.rotation = Quaternion.LookRotation(_camera.transform.forward, _camera.transform.up);
            }

            if (_followTarget != null)
            {
                transform.position = GetFollowPosition(_followTarget, _followCollider, _followOffset, _animationOffset);
            }
        }

        private static Vector3 GetFollowPosition(Transform followTarget, Collider followCollider, Vector3 followOffset, Vector3 animationOffset)
        {
            if (followCollider != null)
            {
                Bounds bounds = followCollider.bounds;
                return bounds.center + Vector3.up * bounds.extents.y + followOffset + animationOffset;
            }

            if (followTarget != null)
            {
                return followTarget.position + followOffset + animationOffset;
            }

            return followOffset + animationOffset;
        }

        private IEnumerator AnimateRoutine()
        {
            duration = duration > 0f ? duration : DefaultDuration;

            Color startColor = textColor;
            Color endColor = new Color(textColor.r, textColor.g, textColor.b, 0f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = duration > 0f ? elapsed / duration : 1f;
                float eased = t * t * (3f - 2f * t);

                _animationOffset = Vector3.up * Mathf.Lerp(0f, floatDistance, eased);
                _canvasGroup.alpha = Mathf.Lerp(startColor.a, endColor.a, eased);

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }

#if UNITY_EDITOR
        private static int _activeEditorPauseGuards;
        private static double _editorPauseGuardUntil;
        private static bool _editorPauseGuardRegistered;

        private static void EnableEditorPauseGuard()
        {
            _activeEditorPauseGuards++;
            GuardEditorPauseFor(duration: 4.0);
        }

        public static void GuardEditorPauseFor(double duration)
        {
            _editorPauseGuardUntil = System.Math.Max(_editorPauseGuardUntil, EditorApplication.timeSinceStartup + duration);
            if (!_editorPauseGuardRegistered)
            {
                EditorApplication.update += ClearUnexpectedEditorPause;
                _editorPauseGuardRegistered = true;
            }

            ClearUnexpectedEditorPause();
        }

        private static void DisableEditorPauseGuard()
        {
            _activeEditorPauseGuards = Mathf.Max(0, _activeEditorPauseGuards - 1);
            if (_activeEditorPauseGuards == 0)
            {
                TryStopEditorPauseGuard();
            }
        }

        private static void ClearUnexpectedEditorPause()
        {
            if (EditorApplication.isPlaying && EditorApplication.isPaused)
            {
                EditorApplication.isPaused = false;
            }

            if (EditorApplication.timeSinceStartup >= _editorPauseGuardUntil)
            {
                TryStopEditorPauseGuard();
            }
        }

        private static void TryStopEditorPauseGuard()
        {
            if (_activeEditorPauseGuards > 0 || EditorApplication.timeSinceStartup < _editorPauseGuardUntil)
            {
                return;
            }

            EditorApplication.update -= ClearUnexpectedEditorPause;
            _editorPauseGuardRegistered = false;
        }
#else
        private static void EnableEditorPauseGuard() { }
        private static void DisableEditorPauseGuard() { }
        public static void GuardEditorPauseFor(double duration) { }
#endif
    }
}
