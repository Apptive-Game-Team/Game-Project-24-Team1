// 게임오버 화면 UI를 만드는 파일.
// 배경 이미지, 홈으로 돌아가기 버튼, 다시시도 버튼, 버튼 클릭을 위한 EventSystem을 생성한다.

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MushOut.Core
{
    public partial class GameOverManager
    {
        private void CreateRuntimeFadeImage()
        {
            _fadeCanvasObject = CreateOverlayCanvas("[GameOverFadeCanvas]", FadeCanvasSortingOrder);

            GameObject imageObject = new GameObject("[Fade]");
            imageObject.transform.SetParent(_fadeCanvasObject.transform, false);

            fadeImage = imageObject.AddComponent<Image>();
            fadeImage.color = Color.clear;
            StretchToParent(fadeImage.rectTransform);
        }

        private void ShowGameOverScreen()
        {
            if (_gameOverCanvasObject == null)
            {
                CreateGameOverScreen();
            }

            _gameOverCanvasObject.SetActive(true);
        }

        private void HideGameOverScreen()
        {
            if (_gameOverCanvasObject != null)
            {
                _gameOverCanvasObject.SetActive(false);
            }

            _isProcessingGameOver = false;
        }

        private void CreateGameOverScreen()
        {
            EnsureEventSystem();
            _gameOverCanvasObject = CreateOverlayCanvas("[GameOverScreenCanvas]", GameOverCanvasSortingOrder);

            Texture2D backgroundTexture = ResolveGameOverBackground();
            CreateBackground(_gameOverCanvasObject.transform, backgroundTexture);
            CreateButton("\uD648\uC73C\uB85C \uB3CC\uC544\uAC00\uAE30", new Vector2(0f, -15f), ReturnHome);
            CreateButton("\uB2E4\uC2DC\uC2DC\uB3C4", new Vector2(0f, -105f), Retry);

            if (backgroundTexture == null)
            {
                Debug.LogWarning($"[GameOverManager] Game over background not found: {DefaultGameOverBackgroundPath}");
            }
        }

        private GameObject CreateOverlayCanvas(string objectName, int sortingOrder)
        {
            GameObject canvasObject = new GameObject(objectName);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvasObject;
        }

        private Texture2D ResolveGameOverBackground()
        {
            if (gameOverBackground != null)
                return gameOverBackground;

#if UNITY_EDITOR
            gameOverBackground = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultGameOverBackgroundPath);
#endif
            return gameOverBackground;
        }

        private static void CreateBackground(Transform parent, Texture2D texture)
        {
            GameObject backgroundObject = new GameObject("[Background]");
            backgroundObject.transform.SetParent(parent, false);

            RawImage background = backgroundObject.AddComponent<RawImage>();
            background.texture = texture;
            background.color = texture == null ? Color.black : Color.white;
            background.raycastTarget = false;
            StretchToParent(background.rectTransform);
        }

        private void CreateButton(string label, Vector2 anchoredPosition, UnityAction action)
        {
            GameObject buttonObject = new GameObject(label);
            buttonObject.transform.SetParent(_gameOverCanvasObject.transform, false);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.55f, 0.04f, 0.04f, 0.95f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            button.colors = CreateButtonColors();

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(360f, 76f);
            buttonRect.anchoredPosition = anchoredPosition;

            CreateButtonText(buttonObject.transform, label);
        }

        private static ColorBlock CreateButtonColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = new Color(0.55f, 0.04f, 0.04f, 0.95f);
            colors.highlightedColor = new Color(0.85f, 0.08f, 0.08f, 1f);
            colors.pressedColor = new Color(0.25f, 0.01f, 0.01f, 1f);
            colors.selectedColor = colors.highlightedColor;
            return colors;
        }

        private static void CreateButtonText(Transform parent, string label)
        {
            GameObject textObject = new GameObject("[Text]");
            textObject.transform.SetParent(parent, false);

            Text text = textObject.AddComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 38;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.raycastTarget = false;
            StretchToParent(text.rectTransform);
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("[GameOverEventSystem]");
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
                DontDestroyOnLoad(eventSystemObject);
            }

            if (eventSystem.GetComponent<BaseInputModule>() != null)
                return;

            Type inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModuleType != null)
            {
                eventSystem.gameObject.AddComponent(inputSystemModuleType);
            }
            else
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }
        }

        private void ResetFadeImage()
        {
            if (fadeImage == null)
                return;

            fadeImage.color = Color.clear;
            fadeImage.raycastTarget = true;
            fadeImage.gameObject.SetActive(true);
        }
    }
}
