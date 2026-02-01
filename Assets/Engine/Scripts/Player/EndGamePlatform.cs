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
            GManager.Instance.TriggerEnding();
        }
    }
}
