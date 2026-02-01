using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ScissorBlade : MonoBehaviour
{
    private ScissorsEnemy _parentScissors;
    private Collider2D _collider;
    
    private const string ROPE_TAG = "GrapplingRope";
    
    private const string PLAYER_TAG = "Player";
    
    private const string ROPE_LAYER = "GrapplingRope";

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        
        if (_collider != null && !_collider.isTrigger)
        {
            _collider.isTrigger = true;
        }
    }

    public void Initialize(ScissorsEnemy parent)
    {
        _parentScissors = parent;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckCollision(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Also check on stay in case the rope/player moves into the blade
        CheckCollision(other);
    }

    private void CheckCollision(Collider2D other)
    {
        if (_parentScissors == null) return;
        
        // First check for player collision
        if (CheckAndDamagePlayer(other))
        {
            return;
        }
        
        // Then check for rope collision
        CheckAndCutRope(other);
    }

    private bool CheckAndDamagePlayer(Collider2D other)
    {
        // Only use tag check - GetComponentInParent would incorrectly match rope 
        // since it's a child of the player
        if (other.CompareTag(PLAYER_TAG))
        {
            _parentScissors.DamagePlayer();
            return true;
        }
        
        return false;
    }

    private void CheckAndCutRope(Collider2D other)
    {
        Vector2 cutPoint = _collider != null ? _collider.bounds.center : (Vector2)transform.position;
        
        if (other.CompareTag(ROPE_TAG))
        {
            _parentScissors.CutGrapplingHookAtPoint(cutPoint);
            return;
        }
        
        // Check by layer
        if (other.gameObject.layer == LayerMask.NameToLayer(ROPE_LAYER))
        {
            _parentScissors.CutGrapplingHookAtPoint(cutPoint);
            return;
        }
        
        SpriteRopeRenderer ropeRenderer = other.GetComponentInParent<SpriteRopeRenderer>();
        if (ropeRenderer != null && ropeRenderer.IsVisible)
        {
            _parentScissors.CutGrapplingHookAtPoint(cutPoint);
            return;
        }
        
        LineRenderer lineRenderer = other.GetComponent<LineRenderer>();
        if (lineRenderer != null && lineRenderer.enabled)
        {
            GrapplingHook hook = other.GetComponentInParent<GrapplingHook>();
            if (hook != null && hook.IsActive)
            {
                _parentScissors.CutGrapplingHookAtPoint(cutPoint);
            }
        }
    }
}
