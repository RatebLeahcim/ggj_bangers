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

    public void StartGame()
    {
        _firstLoad = true;
    }

    private void Update()
    {
        if (!_firstLoad)
        {
            PlayerStateMachine.Instance.Conditions.IsInteracting = true;
            if(Tutorial.IsEnabled)
            {
                return;
            }
            else
            {
                TutorialScan(); 
            }
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

    private void TutorialScan()
    {
        if(PlayerInputBridge.Instance.Consume(PlayerInputType.Space,out bool value))
        {
            _firstLoad = true;
        }
        if(PlayerInputBridge.Instance.Consume(PlayerInputType.Enter,out bool value2))
        {
            Tutorial.IsEnabled = true;
        }
    }

    private void OnGameOver()
    {
        if(GameOverScreen.alpha < 1)
        {
            GameOverScreen.alpha = 1;
        }

        if(PlayerInputBridge.Instance.Consume(PlayerInputType.Enter,out bool value))
        {
            _gameOver = false;
            SceneManager.LoadScene(PlayerSceneName);
        }
    }
}
