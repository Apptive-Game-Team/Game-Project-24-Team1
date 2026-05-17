// 게임 시작 화면의 버튼들을 관리하는 스크립트
// 시작 버튼, 설정 버튼, 종료 버튼이 눌렸을 때 어떤 행동을 할지 정해준다.

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Nexush.Core
{
    public class StartMenuController : MonoBehaviour
    {
        [SerializeField] private string gameSceneName = "MainMapScene"; // 게임 시작 버튼을 눌렀을 때 이동할 씬 이름
        [SerializeField] private GameObject settingsPanel; // 설정 화면 오브젝트를 담아두는 변수
        // Inspector에서 직접 넣지 않아도, 아래 Awake에서 Canvas/SettingsPanel을 찾아서 자동으로 연결한다.

        private void Awake() // 이 씬이 시작될 때 가장 먼저 자동으로 실행된다.
        {
            if (settingsPanel == null)
            {
                // 설정창이 연결되어 있지 않으면 이름 경로로 찾아서 연결한다.
                settingsPanel = FindChildByPath("Canvas/SettingsPanel");
            }

            // 각 버튼 오브젝트를 찾아서, 클릭했을 때 실행될 함수를 연결한다.
            BindButton("Canvas/MenuPanel/StartButton", StartGame);
            BindButton("Canvas/MenuPanel/SettingsButton", OpenSettings);
            BindButton("Canvas/MenuPanel/QuitButton", QuitGame);
            BindButton("Canvas/SettingsPanel/BackButton", CloseSettings);
        }

        public void StartGame() // 게임 시작 버튼을 눌렀을 때 실행되는 함수
        {
            // 혹시 이전에 일시정지 상태였어도 게임 시간이 정상 속도로 흐르게 만든다.
            Time.timeScale = 1f;

            // 시작 화면 UI가 MainMapScene까지 남아있지 않도록 정리한다.
            DestroyStartMenuObjects();

            // gameSceneName에 적힌 씬, 지금은 MainMapScene으로 화면을 전환한다.
            SceneManager.LoadScene(gameSceneName);
        }

        public void OpenSettings() // 설정 버튼을 눌렀을 때 설정창을 켠다.
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
        }

        public void CloseSettings() // 설정창의 뒤로가기 버튼을 눌렀을 때 설정창을 끈다.
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        public void QuitGame() // 게임 종료 버튼을 눌렀을 때 실행된다.
        {
#if UNITY_EDITOR
            // Unity 에디터에서 테스트 중이면 Play 모드를 멈춘다.
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // 실제 빌드된 게임에서는 프로그램을 종료한다.
            Application.Quit();
#endif
        }

        private void BindButton(string path, UnityEngine.Events.UnityAction action) // 버튼 경로와 실행할 함수를 연결하는 공통 함수
        {
            // path 예시: Canvas/MenuPanel/StartButton
            GameObject buttonObject = FindChildByPath(path);
            if (buttonObject == null)
            {
                Debug.LogWarning($"[StartMenuController] Button not found: {path}");
                return;
            }

            Button button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogWarning($"[StartMenuController] Button component missing: {path}");
                return;
            }

            // 같은 함수가 중복 등록되지 않도록 먼저 제거한 뒤 다시 등록한다.
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private GameObject FindChildByPath(string path) // "Canvas/MenuPanel/StartButton" 같은 경로로 오브젝트를 찾는다.
        {
            // "/" 기준으로 문자열을 나눠서 Canvas -> MenuPanel -> StartButton 순서대로 찾아간다.
            string[] parts = path.Split('/');
            if (parts.Length == 0)
            {
                return null;
            }

            GameObject root = GameObject.Find(parts[0]);
            if (root == null)
            {
                return null;
            }

            Transform current = root.transform;
            for (int i = 1; i < parts.Length; i++)
            {
                current = FindDirectChildIncludingInactive(current, parts[i]);
                if (current == null)
                {
                    return null;
                }
            }

            return current.gameObject;
        }

        private Transform FindDirectChildIncludingInactive(Transform parent, string childName) // 꺼져있는 자식 오브젝트까지 포함해서 찾는다.
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private void DestroyStartMenuObjects() // 메인 게임 씬으로 넘어가기 전에 시작 화면 관련 오브젝트를 제거한다.
        {
            DestroyIfExists("Canvas");
            DestroyIfExists("EventSystem");
            Destroy(gameObject);
        }

        private void DestroyIfExists(string objectName) // 이름으로 오브젝트를 찾고, 있으면 삭제한다.
        {
            GameObject target = GameObject.Find(objectName);
            if (target != null)
            {
                Destroy(target);
            }
        }
    }
}
