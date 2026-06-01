using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private string startSceneName = "GameStartScene";

    private GameObject pausePanel;
    private GameObject settingsPanel;
    private Button resumeButton;
    private Button settingsButton;
    private Button closeSettingsButton;
    private Button mainMenuButton;
    private Button quitButton;
    private bool isPaused;

    private void Awake()
    {
        EnsureCanvasPresentation();

        pausePanel = FindChild("Canvas/PausePanel");
        settingsPanel = FindChild("Canvas/PausePanel/SettingsPanel");

        resumeButton = BindButton("Canvas/PausePanel/MenuBox/ResumeButton", Resume);
        settingsButton = BindButton("Canvas/PausePanel/MenuBox/SettingsButton", OpenSettings);
        closeSettingsButton = BindButton("Canvas/PausePanel/SettingsPanel/CloseSettingsButton", CloseSettings);
        mainMenuButton = BindButton("Canvas/PausePanel/MenuBox/MainMenuButton", GoToMainMenu);
        quitButton = BindButton("Canvas/PausePanel/MenuBox/QuitButton", QuitGame);

        EnsureEventSystem();
        SetPaused(false);
    }

    private void Update()
    {
        if (IsEscapePressed())
        {
            SetPaused(!isPaused);
        }
        else if (isPaused)
        {
            HandlePausePointerClick();
        }
    }

    public void Resume()
    {
        SetPaused(false);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(startSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetPaused(bool paused)
    {
        isPaused = paused;

        if (pausePanel != null)
        {
            pausePanel.SetActive(paused);
        }

        if (!paused)
        {
            CloseSettings();
        }

        Time.timeScale = paused ? 0f : 1f;
        Cursor.visible = paused;
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;

        if (paused)
        {
            EnsureEventSystem();
            SelectButton("Canvas/PausePanel/MenuBox/ResumeButton");
        }
        else if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private Button BindButton(string path, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = FindChild(path);
        if (buttonObject == null)
        {
            return null;
        }

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            return null;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
        button.interactable = true;

        if (button.targetGraphic != null)
        {
            button.targetGraphic.raycastTarget = true;
        }

        return button;
    }

    private GameObject FindChild(string path)
    {
        Transform found = transform.Find(path);
        return found != null ? found.gameObject : null;
    }

    private bool IsEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private void HandlePausePointerClick()
    {
        if (!WasPrimaryPointerPressed(out Vector2 screenPosition))
        {
            return;
        }

        if (TryClickButton(closeSettingsButton, screenPosition, CloseSettings))
            return;

        if (TryClickButton(resumeButton, screenPosition, Resume))
            return;

        if (TryClickButton(settingsButton, screenPosition, OpenSettings))
            return;

        if (TryClickButton(mainMenuButton, screenPosition, GoToMainMenu))
            return;

        TryClickButton(quitButton, screenPosition, QuitGame);
    }

    private bool WasPrimaryPointerPressed(out Vector2 screenPosition)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        screenPosition = Vector2.zero;
        return false;
#else
        screenPosition = Input.mousePosition;
        return Input.GetMouseButtonDown(0);
#endif
    }

    private bool TryClickButton(Button button, Vector2 screenPosition, UnityEngine.Events.UnityAction action)
    {
        if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
        {
            return false;
        }

        RectTransform rectTransform = button.transform as RectTransform;
        if (rectTransform == null || !RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, null))
        {
            return false;
        }

        action.Invoke();
        return true;
    }

    private void EnsureCanvasPresentation()
    {
        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            return;
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 1000);
        canvas.enabled = true;

        RectTransform canvasTransform = canvas.transform as RectTransform;
        if (canvasTransform != null)
        {
            canvasTransform.localScale = Vector3.one;
            canvasTransform.localPosition = Vector3.zero;
            canvasTransform.localRotation = Quaternion.identity;
            canvasTransform.anchorMin = Vector2.zero;
            canvasTransform.anchorMax = Vector2.zero;
            canvasTransform.pivot = Vector2.zero;
            canvasTransform.anchoredPosition = Vector2.zero;
            canvasTransform.sizeDelta = Vector2.zero;
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void SelectButton(string path)
    {
        if (EventSystem.current == null)
        {
            return;
        }

        GameObject buttonObject = FindChild(path);
        EventSystem.current.SetSelectedGameObject(buttonObject);
    }

    private void EnsureEventSystem()
    {
        EventSystem eventSystem = EventSystem.current != null ? EventSystem.current : FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        eventSystem.gameObject.SetActive(true);

#if ENABLE_INPUT_SYSTEM
        InputSystemUIInputModule inputSystemModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputSystemModule == null)
        {
            inputSystemModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        if (inputSystemModule.actionsAsset == null)
        {
            inputSystemModule.AssignDefaultActions();
        }

        inputSystemModule.enabled = true;

        StandaloneInputModule standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneModule != null)
        {
            standaloneModule.enabled = false;
        }
#else
        StandaloneInputModule standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneModule == null)
        {
            standaloneModule = eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }

        standaloneModule.enabled = true;
#endif
    }
}
