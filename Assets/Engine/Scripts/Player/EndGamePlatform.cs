using UnityEngine;

public class EndGamePlatform : MonoBehaviour
{
    private Bounds _platformBounds;
    public Collider2D BoundsCollider;
    private bool _triggered;

    private void Start()
    {
        _platformBounds = BoundsCollider.bounds;
        BoundsCollider.enabled = false;
    }

    public void Update()
    {
        if(_triggered){return;}
        Vector2 playerPos = PlayerStateMachine.Instance.transform.position;
        if(_platformBounds.Contains(playerPos))
        {
            _triggered = true;
            DestroyAllEnemies();
            GManager.Instance.TriggerEnding();
        }
    }

    private void DestroyAllEnemies()
    {
        // Destroy all scissors enemies (their OnDestroy will stop audio)
        ScissorsEnemy[] scissors = FindObjectsByType<ScissorsEnemy>(FindObjectsSortMode.None);
        foreach (var enemy in scissors)
        {
            Destroy(enemy.gameObject);
        }

        // Destroy water cup enemies
        WaterCupEnemy[] waterCups = FindObjectsByType<WaterCupEnemy>(FindObjectsSortMode.None);
        foreach (var enemy in waterCups)
        {
            Destroy(enemy.gameObject);
        }
    }
}
