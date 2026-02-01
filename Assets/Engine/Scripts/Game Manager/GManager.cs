using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GManager : MonoBehaviour
{
    [SerializeField] private string ManagerSceneName = "GameManagerScene";
    [SerializeField] private string PlayerSceneName = "playground";
    private static GManager _instance;
    public static GManager Instance => _instance;

    public CanvasGroup MenuGroup;
    public CanvasGroup GameOverScreen;
    public CanvasGroup EndingScreen;
    public TutorialManager Tutorial;
    public PlayerHealthUI HealthUI;
    public CameraDisable CameraDisabler;


    public FMODUnity.EventReference MainMenuMusicEvent;
    public FMODUnity.EventReference MainGameMusicEvent;
    public FMODUnity.EventReference GameOverMusicEvent;
    FMOD.Studio.EventInstance menuMusic;
    FMOD.Studio.EventInstance levelMusic;
    FMOD.Studio.EventInstance overMusic;

    private bool _firstLoad, _gameOver, _playerReady, _playerWin;
    public bool InGame { get { return _firstLoad && !_gameOver && !_playerWin; } }

    private void Awake()
    {
        if(_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(this);
        }
        MenuGroup.alpha = 1;
        StartCoroutine(nameof(LoadSceneAdditive));
        menuMusic = FMODUnity.RuntimeManager.CreateInstance(MainMenuMusicEvent);
        levelMusic = FMODUnity.RuntimeManager.CreateInstance(MainGameMusicEvent);
        overMusic = FMODUnity.RuntimeManager.CreateInstance(GameOverMusicEvent);
        menuMusic.start();
    }

    public void RestartGame()
    {
        _playerWin = false;
        overMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        levelMusic.start();
        _gameOver = false;
        StartCoroutine(nameof(ReloadSceneAdditive));
        HealthUI.Reset();
    }

   public void TriggerEnding()
    {
        _playerWin = true;
    }

    private void Update()
    {
        if (!_firstLoad)
        {
            PlayerStateMachine.Instance.Conditions.IsInteracting = true;
        }
        else
        {
            if (!_playerReady)
            {
                _playerReady = true;
                PlayerStateMachine.Instance.Conditions.IsInteracting = false;
            }
            if(MenuGroup.alpha > 0)
            {
                MenuGroup.alpha -= Time.deltaTime * 8;
            }
            if (PlayerStateMachine.Instance.Health.IsDead)
            {
                _gameOver = true;
            }
            if (_gameOver)
            {
                OnGameOver();
            }
            else
            {
                if(GameOverScreen.alpha > 0)
                {
                    GameOverScreen.alpha -= Time.deltaTime * 8;
                }
            }
            if (_playerWin)
            {
                if(EndingScreen.alpha < 1)
                {
                    PlayerStateMachine.Instance.Conditions.IsInteracting = true;
                    EndingScreen.alpha += Time.deltaTime * 4;
                }
            }
        }
    }

    IEnumerator ReloadSceneAdditive()
    {
        CameraDisabler.EnableCamera();
        Scene scene = SceneManager.GetSceneByName(PlayerSceneName);
        if (scene.IsValid() && scene.isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(scene);
        }
        yield return SceneManager.LoadSceneAsync(PlayerSceneName, LoadSceneMode.Additive);
        CameraDisabler.DisableCamera();
        _firstLoad = true;
        menuMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        levelMusic.start();
    }

    IEnumerator LoadSceneAdditive()
    {
        yield return SceneManager.LoadSceneAsync(PlayerSceneName, LoadSceneMode.Additive);
        CameraDisabler.DisableCamera();
    }

    public void TutorialOpen()
    {
        Tutorial.IsEnabled = true;
    }

    public void TutorialClose()
    {
        Tutorial.IsEnabled = false;
    }

    public void FirstPlay()
    {
        _firstLoad = true;
        menuMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        levelMusic.start();
    }

    private void OnGameOver()
    {
        if(GameOverScreen.alpha < 1)
        {
            levelMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            overMusic.start();
            GameOverScreen.alpha = 1;
        }
    }
}