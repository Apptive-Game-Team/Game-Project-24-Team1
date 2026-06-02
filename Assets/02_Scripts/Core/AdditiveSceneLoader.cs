using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MushOut.Core
{
    /// <summary>
    /// 주 씬(예: GamePlayScene) 로딩 시, Inspector에 정의된 종속 씬들을 Additive(중첩) 모드로 비동기 로드해주는 클래스입니다.
    /// </summary>
    public class AdditiveSceneLoader : MonoBehaviour
    {
        [Header("중첩 로드할 씬 설정")]
        [Tooltip("로딩할 씬들의 이름을 목록에 추가합니다.")]
        [SerializeField] private List<string> _scenesToLoad = new List<string>();

        [Header("라이팅 및 활성화 설정")]
        [Tooltip("로드 완료 후 액티브 씬으로 설정할 씬의 이름입니다. 라이팅/스카이박스 등이 정의된 씬을 권장합니다. 비워두면 주 씬이 유지됩니다.")]
        [SerializeField] private string _activeSceneName = "GamePlayScene";

        private void Start()
        {
            StartCoroutine(LoadAdditiveScenesRoutine());
        }

        /// <summary>
        /// 설정된 씬들을 순차적으로 비동기 중첩 로드하는 코루틴입니다.
        /// </summary>
        private IEnumerator LoadAdditiveScenesRoutine()
        {
            foreach (string sceneName in _scenesToLoad)
            {
                if (string.IsNullOrEmpty(sceneName))
                {
                    continue;
                }

                // 이미 로드된 씬인지 체크하여 오동작 방지
                Scene scene = SceneManager.GetSceneByName(sceneName);
                if (scene.isLoaded)
                {
                    Debug.Log($"[AdditiveSceneLoader] {sceneName} 씬이 이미 로드되어 있어 생략합니다.");
                    continue;
                }

                // 비동기 씬 로드
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                
                // 로드가 완료될 때까지 대기
                while (!asyncLoad.isDone)
                {
                    yield return null;
                }

                Debug.Log($"[AdditiveSceneLoader] {sceneName} 씬 로드 완료.");
            }

            // 라이팅 및 기준이 될 액티브 씬 설정
            if (!string.IsNullOrEmpty(_activeSceneName))
            {
                Scene activeScene = SceneManager.GetSceneByName(_activeSceneName);
                if (activeScene.isLoaded)
                {
                    SceneManager.SetActiveScene(activeScene);
                    Debug.Log($"[AdditiveSceneLoader] 액티브 씬이 {_activeSceneName}(으)로 설정되었습니다.");
                }
                else
                {
                    Debug.LogWarning($"[AdditiveSceneLoader] 액티브 씬으로 지정된 {_activeSceneName} 씬이 로드되지 않았습니다.");
                }
            }
        }
    }
}
