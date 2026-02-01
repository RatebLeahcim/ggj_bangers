using UnityEngine;

public class GrappleSpriteSwitcher : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The GrapplingHook component to monitor")]
    [SerializeField] private GrapplingHook grapplingHook;
    
    [Tooltip("The SpriteRenderer to change sprites on (usually the player's sprite)")]
    [SerializeField] private SpriteRenderer targetRenderer;
    
    [Header("Rope Length Sprites")]
    [Tooltip("Sprite shown when rope is at minimum length (0% - fully reeled in)")]
    [SerializeField] private Sprite minLengthSprite;
    
    [Tooltip("Sprite shown when rope is at 50% length")]
    [SerializeField] private Sprite midLengthSprite;
    
    [Tooltip("Sprite shown when rope is at maximum length (100%)")]
    [SerializeField] private Sprite maxLengthSprite;
    
    [Header("Settings")]
    [Tooltip("If true, restores the original sprite when not grappling")]
    [SerializeField] private bool restoreOnRelease = true;
    
    private Sprite _originalSprite;
    private Sprite _currentGrappleSprite;
    private bool _wasActive = false;

    private void Start()
    {
        if (grapplingHook == null)
        {
            grapplingHook = GetComponent<GrapplingHook>();
        }
        
        if (targetRenderer == null && PlayerStateMachine.Instance != null)
        {
            targetRenderer = PlayerStateMachine.Instance.PlayerRenderer;
        }
        
        if (targetRenderer != null)
        {
            _originalSprite = targetRenderer.sprite;
        }
    }

    private void Update()
    {
        if (grapplingHook == null || targetRenderer == null) return;
        
        // Update sprite while shooting OR while hooked
        if (grapplingHook.IsShooting)
        {
            UpdateSpriteForShootDistance();
            _wasActive = true;
        }
        else if (grapplingHook.IsHooked)
        {
            UpdateSpriteForRopeLength();
            _wasActive = true;
        }
        else if (_wasActive && restoreOnRelease)
        {
            targetRenderer.sprite = _originalSprite;
            _wasActive = false;
            _currentGrappleSprite = null;
        }
    }

    private void UpdateSpriteForShootDistance()
    {
        float currentDistance = grapplingHook.CurrentShootDistance;
        float maxLength = grapplingHook.Status.MaxRopeLength;
        
        if (maxLength <= 0) return;
        
        float lengthPercent = Mathf.Clamp01(currentDistance / maxLength);
        
        ApplySpriteForPercent(lengthPercent);
    }

    private void UpdateSpriteForRopeLength()
    {
        float currentLength = grapplingHook.CurrentRopeLength;
        float maxLength = grapplingHook.Status.MaxRopeLength;
        
        if (maxLength <= 0) return;
        
        float lengthPercent = Mathf.Clamp01(currentLength / maxLength);
        
        ApplySpriteForPercent(lengthPercent);
    }

    private void ApplySpriteForPercent(float lengthPercent)
    {
        Sprite targetSprite;
        
        if (lengthPercent < 0.5f)
        {
            targetSprite = minLengthSprite;
        }
        else if (lengthPercent < 1.0f)
        {
            targetSprite = midLengthSprite;
        }
        else
        {
            targetSprite = maxLengthSprite;
        }
        
        if (targetSprite != null && targetSprite != _currentGrappleSprite)
        {
            targetRenderer.sprite = targetSprite;
            _currentGrappleSprite = targetSprite;
        }
    }
}
