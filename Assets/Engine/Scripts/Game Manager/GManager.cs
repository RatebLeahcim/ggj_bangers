using UnityEngine;
using UnityEngine.SceneManagement;

public class GManager : MonoBehaviour
{
    [SerializeField] private string ManagerSceneName;
    [SerializeField] private string PlayerSceneName;

    public CanvasGroup MenuGroup;
    public CanvasGroup GameOverScreen;
    public TutorialManager Tutorial;

    private bool _firstLoad, _gameOver, _playerReady;

    private void Awake()
    {
        MenuGroup.alpha = 1;
    }

    public void RestartGame()
    {
        _gameOver = false;
        SceneManager.LoadScene(PlayerSceneName);
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
        }
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
