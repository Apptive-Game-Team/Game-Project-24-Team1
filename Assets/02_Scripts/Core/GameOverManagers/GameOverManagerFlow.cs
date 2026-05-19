// 게임오버가 실제로 진행되는 순서를 담당하는 파일.
// 페이드, 검은 화면 유지, 게임오버 화면 띄우기, 다시시도, 홈으로 돌아가기를 처리한다.

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MushOut.Core
{
    public partial class GameOverManager
    {
        public void Retry()
        {
            Time.timeScale = 1f;
            ResetEnemyCrashFlag();
            ResetPlayerToSpawnPosition();
            HideGameOverScreen();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ChangeState(GameManager.GameState.Playing);
            }
        }

        public void ReturnHome()
        {
            Time.timeScale = 1f;
            CleanupRuntimeUI();
            ResetEnemyCrashFlag();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ChangeState(GameManager.GameState.Ready);
            }

            if (!string.IsNullOrEmpty(homeSceneName))
            {
                SceneManager.LoadScene(homeSceneName);
            }
        }

        private IEnumerator ProcessGameOverSequence()
        {
            _isProcessingGameOver = true;
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (fadeImage != null)
            {
                yield return Fade(0f, 1f);
            }

            if (blackScreenHoldDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(blackScreenHoldDuration);
            }

            if (playSoundOnGameOver && gameOverSound != null)
            {
                _audioSource.PlayOneShot(gameOverSound);
            }

            ShowGameOverScreen();

            if (fadeImage != null)
            {
                yield return Fade(1f, 0f);
                fadeImage.raycastTarget = false;
            }
        }

        private IEnumerator Fade(float fromAlpha, float toAlpha)
        {
            float elapsed = 0f;
            Color color = fadeImage.color;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                color.a = Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(elapsed / fadeDuration));
                fadeImage.color = color;
                yield return null;
            }

            color.a = toAlpha;
            fadeImage.color = color;
        }

        private static void ResetEnemyCrashFlag()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CrashedByEnemy = false;
            }
        }

        private void CleanupRuntimeUI()
        {
            DestroyIfExists(_gameOverCanvasObject);
            DestroyIfExists(_fadeCanvasObject);

            _gameOverCanvasObject = null;
            _fadeCanvasObject = null;
            fadeImage = null;
            _isInitialized = false;
            _isProcessingGameOver = false;
        }

        private static void DestroyIfExists(GameObject target)
        {
            if (target != null)
            {
                Destroy(target);
            }
        }
    }
}
