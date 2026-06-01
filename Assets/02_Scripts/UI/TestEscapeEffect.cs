using MushOut.Core;
using MushOut.UI;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TestEscapeEffect : MonoBehaviour
{
    private const string RuntimeObjectName = "[Runtime] TestEscapeEffect";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntimeListener()
    {
        if (FindFirstObjectByType<TestEscapeEffect>() != null) return;

        GameObject listener = new GameObject(RuntimeObjectName);
        DontDestroyOnLoad(listener);
        listener.AddComponent<TestEscapeEffect>();
    }

    private void Update()
    {
        if (WasPressed(KeyCode.T))
        {
            EscapeScreenEffect.PlayEnterPulse();
            GameManager.Instance.ChangeState(GameManager.GameState.Escaping);
        }

        if (WasPressed(KeyCode.Y))
        {
            EscapeScreenEffect.SetActiveEffect(false, false);
            GameManager.Instance.ChangeState(GameManager.GameState.Playing);
        }
    }

    private static bool WasPressed(KeyCode keyCode)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (keyCode == KeyCode.T) return Keyboard.current.tKey.wasPressedThisFrame;
            if (keyCode == KeyCode.Y) return Keyboard.current.yKey.wasPressedThisFrame;
        }
#endif

        return Input.GetKeyDown(keyCode);
    }
}
