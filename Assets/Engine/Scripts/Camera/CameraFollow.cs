using UnityEngine;

/// <summary>
/// Smooth camera follow script for 2D games.
/// Follows a target with configurable smoothing, offset, and optional bounds.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The target transform to follow (usually the player)")]
    [SerializeField] private Transform target;
    
    [Tooltip("If true, automatically finds the player by tag on Start")]
    [SerializeField] private bool autoFindPlayer = true;
    
    [Tooltip("Tag to search for when auto-finding player")]
    [SerializeField] private string playerTag = "Player";

    [Header("Follow Settings")]
    [Tooltip("Offset from the target position (useful for looking ahead or keeping player off-center)")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    
    [Tooltip("How quickly the camera follows the target. Higher = snappier, Lower = smoother")]
    [SerializeField] private float smoothSpeed = 5f;
    
    [Tooltip("If true, camera follows instantly with no smoothing")]
    [SerializeField] private bool instantFollow = false;

    [Header("Look Ahead")]
    [Tooltip("If true, camera looks ahead in the direction the target is moving")]
    [SerializeField] private bool useLookAhead = false;
    
    [Tooltip("How far ahead the camera looks based on target velocity")]
    [SerializeField] private float lookAheadDistance = 2f;
    
    [Tooltip("How quickly the look-ahead adjusts")]
    [SerializeField] private float lookAheadSmoothing = 3f;

    [Header("Deadzone")]
    [Tooltip("If true, camera only moves when target exits the deadzone")]
    [SerializeField] private bool useDeadzone = false;
    
    [Tooltip("Size of the deadzone (target can move this much before camera follows)")]
    [SerializeField] private Vector2 deadzoneSize = new Vector2(1f, 1f);

    [Header("Bounds (Optional)")]
    [Tooltip("If true, constrains camera within specified bounds")]
    [SerializeField] private bool useBounds = false;
    
    [Tooltip("Minimum X and Y position for camera")]
    [SerializeField] private Vector2 boundsMin = new Vector2(-50f, -50f);
    
    [Tooltip("Maximum X and Y position for camera")]
    [SerializeField] private Vector2 boundsMax = new Vector2(50f, 50f);

    private Vector3 _currentVelocity;
    private Vector3 _lookAheadOffset;
    private Rigidbody2D _targetRigidbody;

    private void Start()
    {
        if (autoFindPlayer && target == null)
        {
            FindPlayer();
        }
        
        if (target != null)
        {
            _targetRigidbody = target.GetComponent<Rigidbody2D>();
            
            // Snap to target position immediately on start
            Vector3 targetPosition = GetTargetPosition();
            transform.position = targetPosition;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;
        
        Vector3 targetPosition = GetTargetPosition();
        
        // Apply deadzone
        if (useDeadzone)
        {
            targetPosition = ApplyDeadzone(targetPosition);
        }
        
        // Apply look-ahead
        if (useLookAhead)
        {
            targetPosition = ApplyLookAhead(targetPosition);
        }
        
        // Apply bounds
        if (useBounds)
        {
            targetPosition = ApplyBounds(targetPosition);
        }
        
        // Move camera
        if (instantFollow)
        {
            transform.position = targetPosition;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        }
    }

    private Vector3 GetTargetPosition()
    {
        return target.position + offset;
    }

    private Vector3 ApplyDeadzone(Vector3 targetPosition)
    {
        Vector3 currentPos = transform.position;
        Vector3 delta = targetPosition - currentPos;
        
        // Only move if outside deadzone
        if (Mathf.Abs(delta.x) < deadzoneSize.x / 2f)
        {
            targetPosition.x = currentPos.x;
        }
        
        if (Mathf.Abs(delta.y) < deadzoneSize.y / 2f)
        {
            targetPosition.y = currentPos.y;
        }
        
        return targetPosition;
    }

    private Vector3 ApplyLookAhead(Vector3 targetPosition)
    {
        Vector2 targetVelocity = Vector2.zero;
        
        if (_targetRigidbody != null)
        {
            targetVelocity = _targetRigidbody.linearVelocity;
        }
        
        // Calculate look-ahead based on velocity direction
        Vector3 desiredLookAhead = new Vector3(
            Mathf.Sign(targetVelocity.x) * Mathf.Min(Mathf.Abs(targetVelocity.x), 1f) * lookAheadDistance,
            Mathf.Sign(targetVelocity.y) * Mathf.Min(Mathf.Abs(targetVelocity.y), 1f) * lookAheadDistance * 0.5f,
            0f
        );
        
        // Smooth the look-ahead transition
        _lookAheadOffset = Vector3.Lerp(_lookAheadOffset, desiredLookAhead, lookAheadSmoothing * Time.deltaTime);
        
        return targetPosition + _lookAheadOffset;
    }

    private Vector3 ApplyBounds(Vector3 targetPosition)
    {
        targetPosition.x = Mathf.Clamp(targetPosition.x, boundsMin.x, boundsMax.x);
        targetPosition.y = Mathf.Clamp(targetPosition.y, boundsMin.y, boundsMax.y);
        return targetPosition;
    }

    /// <summary>
    /// Finds and sets the player as the target using the configured tag.
    /// </summary>
    public void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            target = playerObj.transform;
            _targetRigidbody = playerObj.GetComponent<Rigidbody2D>();
            Debug.Log($"CameraFollow: Found player '{playerObj.name}'");
        }
        else
        {
            Debug.LogWarning($"CameraFollow: Could not find GameObject with tag '{playerTag}'");
        }
    }

    /// <summary>
    /// Sets the target transform for the camera to follow.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            _targetRigidbody = target.GetComponent<Rigidbody2D>();
        }
    }

    /// <summary>
    /// Immediately snaps the camera to the target position.
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        
        Vector3 targetPosition = GetTargetPosition();
        if (useBounds)
        {
            targetPosition = ApplyBounds(targetPosition);
        }
        transform.position = targetPosition;
    }

    /// <summary>
    /// Sets the camera bounds.
    /// </summary>
    public void SetBounds(Vector2 min, Vector2 max)
    {
        boundsMin = min;
        boundsMax = max;
        useBounds = true;
    }

    /// <summary>
    /// Disables camera bounds.
    /// </summary>
    public void ClearBounds()
    {
        useBounds = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Draw deadzone
        if (useDeadzone)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Vector3 center = Application.isPlaying ? transform.position : (target != null ? target.position + offset : transform.position);
            center.z = 0;
            Gizmos.DrawCube(center, new Vector3(deadzoneSize.x, deadzoneSize.y, 0.1f));
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(center, new Vector3(deadzoneSize.x, deadzoneSize.y, 0.1f));
        }
        
        // Draw bounds
        if (useBounds)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Vector3 boundsCenter = new Vector3((boundsMin.x + boundsMax.x) / 2f, (boundsMin.y + boundsMax.y) / 2f, 0f);
            Vector3 boundsSize = new Vector3(boundsMax.x - boundsMin.x, boundsMax.y - boundsMin.y, 0.1f);
            Gizmos.DrawWireCube(boundsCenter, boundsSize);
        }
    }
#endif
}
