using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

// 게임 중 ESC를 눌렀을 때 나오는 일시정지 메뉴를 관리하는 스크립트
// 계속하기, 설정, 게임 종료 버튼의 기능도 여기서 연결한다.
public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private string startSceneName = "GameStartScene"; // 메인 메뉴로 돌아갈 때 이동할 씬 이름

    private GameObject pausePanel; // ESC를 누르면 켜지고 꺼지는 일시정지 전체 패널
    private GameObject settingsPanel; // 일시정지 메뉴 안에 있는 설정창 패널
    private bool isPaused; // 현재 게임이 일시정지 상태인지 저장하는 변수

    private void Awake() // 씬이 시작될 때 한 번 실행되고, 필요한 UI와 버튼을 자동으로 연결한다.
    {
        // PauseMenu prefab 안에서 필요한 패널들을 경로로 찾아 저장한다.
        pausePanel = FindChild("Canvas/PausePanel");
        settingsPanel = FindChild("Canvas/PausePanel/SettingsPanel");

        // 버튼을 누르면 실행될 함수를 각각 연결한다.
        BindButton("Canvas/PausePanel/MenuBox/ResumeButton", Resume);
        BindButton("Canvas/PausePanel/MenuBox/SettingsButton", OpenSettings);
        BindButton("Canvas/PausePanel/SettingsPanel/CloseSettingsButton", CloseSettings);
        BindButton("Canvas/PausePanel/MenuBox/MainMenuButton", GoToMainMenu);
        BindButton("Canvas/PausePanel/MenuBox/QuitButton", QuitGame);

        // UI 버튼 클릭을 받기 위해 EventSystem이 없으면 자동으로 만든다.
        EnsureEventSystem();

        // 게임 시작 직후에는 일시정지 메뉴가 보이면 안 되므로 꺼둔다.
        SetPaused(false);
    }

    private void Update() // 매 프레임 실행되면서 ESC 입력을 확인한다.
    {
        if (IsEscapePressed())
        {
            // ESC를 누르면 현재 상태의 반대로 바꾼다. 꺼져 있으면 켜고, 켜져 있으면 끈다.
            SetPaused(!isPaused);
        }
    }

    public void Resume() // 계속하기 버튼을 눌렀을 때 게임을 다시 진행한다.
    {
        SetPaused(false);
    }

    public void OpenSettings() // 설정 버튼을 눌렀을 때 설정창을 켠다.
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void CloseSettings() // 설정창의 돌아가기 버튼을 눌렀을 때 설정창을 끈다.
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void GoToMainMenu() // 메인 메뉴로 돌아가는 버튼이 있을 때 사용하는 함수
    {
        // 씬을 바꾸기 전에 게임 시간을 정상 속도로 돌려놓는다.
        Time.timeScale = 1f;
        SceneManager.LoadScene(startSceneName);
    }

    public void QuitGame() // 게임 종료 버튼을 눌렀을 때 실행된다.
    {
#if UNITY_EDITOR
        // Unity 에디터에서는 Play 모드를 멈춘다.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 실제 빌드된 게임에서는 프로그램을 종료한다.
        Application.Quit();
#endif
    }

    private void SetPaused(bool paused) // 실제로 게임을 멈추거나 다시 시작하는 핵심 함수
    {
        isPaused = paused;

        if (pausePanel != null)
        {
            pausePanel.SetActive(paused);
        }

        if (!paused)
        {
            // 일시정지를 풀 때 설정창이 열려 있었다면 같이 닫는다.
            CloseSettings();
        }

        // paused가 true면 게임 시간을 0으로 만들어 멈추고, false면 다시 1로 돌린다.
        Time.timeScale = paused ? 0f : 1f;

        // 일시정지 중에는 마우스 커서를 보이게 해서 버튼을 누를 수 있게 한다.
        Cursor.visible = paused;
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void BindButton(string path, UnityEngine.Events.UnityAction action) // 버튼 경로와 실행할 함수를 연결하는 공통 함수
    {
        GameObject buttonObject = FindChild(path);
        if (buttonObject == null)
        {
            return;
        }

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        // 같은 함수가 중복 등록되지 않도록 먼저 제거한 뒤 다시 등록한다.
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private GameObject FindChild(string path) // PauseMenu 오브젝트 안에서 자식 오브젝트를 경로로 찾는다.
    {
        Transform found = transform.Find(path);
        return found != null ? found.gameObject : null;
    }

    private bool IsEscapePressed() // 현재 프로젝트의 입력 시스템에 맞춰 ESC가 눌렸는지 확인한다.
    {
#if ENABLE_INPUT_SYSTEM
        // 새 Input System을 사용하는 경우
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        // 기존 Input Manager를 사용하는 경우
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private void EnsureEventSystem() // UI 버튼 클릭을 처리하는 EventSystem이 없으면 자동으로 생성한다.
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
        // 새 Input System용 UI 입력 모듈
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
        // 기존 Input Manager용 UI 입력 모듈
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
    }
}
