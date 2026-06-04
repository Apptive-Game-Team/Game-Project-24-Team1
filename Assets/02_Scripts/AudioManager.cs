using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [System.Serializable]
    public class SceneBGM
    {
        public string sceneName;
        public AudioClip bgmClip;
    }

    [Header("Scene BGM Settings")]
    [SerializeField] private SceneBGM[] sceneBGMs;

    [Header("Volume Settings")]
    [SerializeField] private float bgmVolume = 0.7f;
    [SerializeField] private float fadeTime = 1f;

    private AudioSource audioSource;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        // AudioManager가 이미 있으면 새로 생긴 것은 삭제
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 씬이 바뀌어도 AudioManager가 사라지지 않게 함
        DontDestroyOnLoad(gameObject);

        // AudioSource 자동으로 추가
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = bgmVolume;
    }

    private void OnEnable()
    {
        // 씬이 로드될 때마다 실행되는 이벤트 등록
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // 이벤트 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // 게임 시작 시 현재 씬 음악 재생
        PlayBGMForScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 바뀌면 해당 씬 음악 재생
        PlayBGMForScene(scene.name);
    }

    private void PlayBGMForScene(string sceneName)
    {
        AudioClip clip = GetBGMClip(sceneName);

        if (clip == null)
        {
            Debug.LogWarning("등록된 BGM이 없는 씬입니다: " + sceneName);
            return;
        }

        // 같은 음악이 이미 재생 중이면 다시 재생하지 않음
        if (audioSource.clip == clip && audioSource.isPlaying)
            return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(ChangeBGM(clip));
    }

    private AudioClip GetBGMClip(string sceneName)
    {
        foreach (SceneBGM sceneBGM in sceneBGMs)
        {
            if (sceneBGM.sceneName == sceneName)
            {
                return sceneBGM.bgmClip;
            }
        }

        return null;
    }

    private IEnumerator ChangeBGM(AudioClip newClip)
    {
        float startVolume = audioSource.volume;

        // 기존 음악 페이드 아웃
        while (audioSource.volume > 0f)
        {
            audioSource.volume -= startVolume * Time.deltaTime / fadeTime;
            yield return null;
        }

        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.Play();

        // 새 음악 페이드 인
        while (audioSource.volume < bgmVolume)
        {
            audioSource.volume += bgmVolume * Time.deltaTime / fadeTime;
            yield return null;
        }

        audioSource.volume = bgmVolume;
        fadeCoroutine = null;
    }
}