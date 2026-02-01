using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    public Image[] HeartObjects;
    public Color ClearColor;
    private Color white = Color.white;

    private void Update()
    {
        if(PlayerStateMachine.Instance == null){ return; }
        float health = Mathf.InverseLerp(0,PlayerStateMachine.Instance.Health.MaxHealth,PlayerStateMachine.Instance.Health.Health);
        for(int i = 0; i < 10; i++)
        {
            if(health * 10 < i)
            {
                HeartObjects[i].color = ClearColor;
            }
        }
    }

    public void Reset()
    {
        for(int i = 0; i < 10; i++)
        {
            HeartObjects[i].color = white;
        }
    }
}