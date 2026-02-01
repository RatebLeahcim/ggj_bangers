using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GManager : MonoBehaviour
{
    [SerializeField] private string ManagerSceneName = "GameManagerScene";
    [SerializeField] private string PlayerSceneName = "playground";

    public CanvasGroup MenuGroup;
    public CanvasGroup GameOverScreen;
    public TutorialManager Tutorial;
    public PlayerHealthUI HealthUI;

    private bool _firstLoad, _gameOver, _playerReady;

    private void Awake()
    {
        MenuGroup.alpha = 1;
        SceneManager.LoadScene(PlayerSceneName,LoadSceneMode.Additive);
    }

    public void RestartGame()
    {

        _gameOver = false;
        StartCoroutine(nameof(ReloadSceneAdditive));
        HealthUI.Reset();
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
        }
    }

    IEnumerator ReloadSceneAdditive()
    {
        Scene scene = SceneManager.GetSceneByName(PlayerSceneName);
        if (scene.IsValid() && scene.isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(scene);
        }
        yield return SceneManager.LoadSceneAsync(PlayerSceneName, LoadSceneMode.Additive);
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
    }

    private void OnGameOver()
    {
        if(GameOverScreen.alpha < 1)
        {
            GameOverScreen.alpha = 1;
        }
    }
}