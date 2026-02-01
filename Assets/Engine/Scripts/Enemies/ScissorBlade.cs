using UnityEngine;

/// <summary>
/// Component attached to each scissor blade to handle trigger-based detection
/// of the grappling hook rope. Works in conjunction with ScissorsEnemy.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ScissorBlade : MonoBehaviour
{
    private ScissorsEnemy _parentScissors;
    private Collider2D _collider;
    
    /// <summary>
    /// Tag to identify grappling hook rope objects.
    /// </summary>
    private const string ROPE_TAG = "GrapplingRope";
    
    /// <summary>
    /// Alternative: Layer name for the rope if using layer-based detection.
    /// </summary>
    private const string ROPE_LAYER = "GrapplingRope";

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        
        // Ensure the collider is set as a trigger
        if (_collider != null && !_collider.isTrigger)
        {
            _collider.isTrigger = true;
        }
    }

    /// <summary>
    /// Initialize the blade with a reference to the parent scissors enemy.
    /// </summary>
    public void Initialize(ScissorsEnemy parent)
    {
        _parentScissors = parent;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckAndCutRope(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Also check on stay in case the rope moves into the blade
        CheckAndCutRope(other);
    }

    private void CheckAndCutRope(Collider2D other)
    {
        if (_parentScissors == null) return;
        
        // Use the center of this blade's collider as the cut point
        Vector2 cutPoint = _collider != null ? _collider.bounds.center : (Vector2)transform.position;
        
        // Check by tag
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
        
        // Check if the collider belongs to a SpriteRopeRenderer or LineRenderer
        // that is part of an active grappling hook
        SpriteRopeRenderer ropeRenderer = other.GetComponentInParent<SpriteRopeRenderer>();
        if (ropeRenderer != null && ropeRenderer.IsVisible)
        {
            _parentScissors.CutGrapplingHookAtPoint(cutPoint);
            return;
        }
        
        // Also check for LineRenderer-based rope (if using that visual mode)
        LineRenderer lineRenderer = other.GetComponent<LineRenderer>();
        if (lineRenderer != null && lineRenderer.enabled)
        {
            // Try to find parent GrapplingHook
            GrapplingHook hook = other.GetComponentInParent<GrapplingHook>();
            if (hook != null && hook.IsActive)
            {
                _parentScissors.CutGrapplingHookAtPoint(cutPoint);
            }
        }
    }
}
