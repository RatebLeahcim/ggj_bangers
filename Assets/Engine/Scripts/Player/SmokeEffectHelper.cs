using UnityEngine;

/// <summary>
/// Helper component to auto-hide the smoke sprite when not animating.
/// Attach this to the SmokeEffect GameObject alongside the Animator.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class SmokeEffectHelper : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    private Animator _animator;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();
        
        // Start hidden
        _spriteRenderer.enabled = false;
    }

    /// <summary>
    /// Called via Animation Event at the start of the smoke animation.
    /// </summary>
    public void OnSmokeStart()
    {
        _spriteRenderer.enabled = true;
    }

    /// <summary>
    /// Called via Animation Event at the end of the smoke animation.
    /// </summary>
    public void OnSmokeEnd()
    {
        _spriteRenderer.enabled = false;
    }
}
