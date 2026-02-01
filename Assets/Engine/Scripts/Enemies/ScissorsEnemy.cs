using UnityEngine;

/// <summary>
/// A scissors enemy that patrols along a track and can cut the player's grappling hook tape.
/// The scissors consist of two blades that rotate around a pivot point, each with its own BoxCollider2D.
/// When either blade's collider overlaps with an active grappling hook rope, the grapple is released.
/// </summary>
public class ScissorsEnemy : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Speed at which the scissors move along the patrol path")]
    [SerializeField] private float moveSpeed = 2f;
    
    [Tooltip("Left boundary of the patrol path (local X offset from start position)")]
    [SerializeField] private float patrolLeftBound = -3f;
    
    [Tooltip("Right boundary of the patrol path (local X offset from start position)")]
    [SerializeField] private float patrolRightBound = 3f;
    
    [Tooltip("Pause duration at each patrol endpoint")]
    [SerializeField] private float pauseAtEndpoints = 0.5f;
    
    [Header("Facing Direction")]
    [Tooltip("How to flip the scissors when changing direction")]
    [SerializeField] private FlipMethod flipMethod = FlipMethod.ScaleX;
    
    [Tooltip("If true, starts facing left. If false, starts facing right.")]
    [SerializeField] private bool startFacingLeft = false;
    
    [Header("Scissor Animation")]
    [Tooltip("Speed of the scissor open/close animation (degrees per second)")]
    [SerializeField] private float snipSpeed = 180f;
    
    [Tooltip("Maximum angle the blades open to (from closed position)")]
    [SerializeField] private float maxOpenAngle = 45f;
    
    [Tooltip("Minimum angle the blades close to")]
    [SerializeField] private float minCloseAngle = 5f;
    
    [Header("Blade References")]
    [Tooltip("Transform for the top scissor blade (should have a BoxCollider2D child or component)")]
    [SerializeField] private Transform topBlade;
    
    [Tooltip("Transform for the bottom scissor blade (should have a BoxCollider2D child or component)")]
    [SerializeField] private Transform bottomBlade;
    
    [Header("Detection Settings")]
    [Tooltip("LayerMask for detecting the grappling hook rope")]
    [SerializeField] private LayerMask ropeLayerMask;
    
    [Tooltip("If true, uses trigger-based detection. If false, uses overlap checks.")]
    [SerializeField] private bool useTriggerDetection = true;

    // Internal state
    private Vector3 _startPosition;
    private float _currentPatrolX;
    private int _moveDirection = 1;
    private float _pauseTimer = 0f;
    private bool _isPaused = false;
    private bool _isFacingLeft = false;
    
    // Scissor animation state
    private float _currentBladeAngle;
    private bool _isOpening = true;
    
    // Cached references
    private BoxCollider2D _topBladeCollider;
    private BoxCollider2D _bottomBladeCollider;
    private ScissorBlade _topBladeScript;
    private ScissorBlade _bottomBladeScript;

    private void Awake()
    {
        _startPosition = transform.position;
        _currentPatrolX = 0f;
        _currentBladeAngle = minCloseAngle;
        
        // Initialize facing direction
        _isFacingLeft = startFacingLeft;
        _moveDirection = startFacingLeft ? -1 : 1;
        ApplyFacingDirection();
        
        // Get or add ScissorBlade scripts to blades for trigger detection
        if (topBlade != null)
        {
            _topBladeCollider = topBlade.GetComponentInChildren<BoxCollider2D>();
            if (useTriggerDetection)
            {
                _topBladeScript = topBlade.GetComponentInChildren<ScissorBlade>();
                if (_topBladeScript == null && _topBladeCollider != null)
                {
                    _topBladeScript = _topBladeCollider.gameObject.AddComponent<ScissorBlade>();
                }
                if (_topBladeScript != null)
                {
                    _topBladeScript.Initialize(this);
                }
            }
        }
        
        if (bottomBlade != null)
        {
            _bottomBladeCollider = bottomBlade.GetComponentInChildren<BoxCollider2D>();
            if (useTriggerDetection)
            {
                _bottomBladeScript = bottomBlade.GetComponentInChildren<ScissorBlade>();
                if (_bottomBladeScript == null && _bottomBladeCollider != null)
                {
                    _bottomBladeScript = _bottomBladeCollider.gameObject.AddComponent<ScissorBlade>();
                }
                if (_bottomBladeScript != null)
                {
                    _bottomBladeScript.Initialize(this);
                }
            }
        }
    }

    private void Update()
    {
        UpdatePatrolMovement();
        UpdateScissorAnimation();
        
        // If not using triggers, manually check for rope overlap
        if (!useTriggerDetection)
        {
            CheckForRopeOverlap();
        }
    }

    private void UpdatePatrolMovement()
    {
        // Handle pause at endpoints
        if (_isPaused)
        {
            _pauseTimer -= Time.deltaTime;
            if (_pauseTimer <= 0f)
            {
                _isPaused = false;
            }
            return;
        }
        
        // Move along patrol path
        _currentPatrolX += _moveDirection * moveSpeed * Time.deltaTime;
        
        // Check bounds and reverse direction
        if (_currentPatrolX >= patrolRightBound)
        {
            _currentPatrolX = patrolRightBound;
            _moveDirection = -1;
            _isFacingLeft = true;
            ApplyFacingDirection();
            StartPause();
        }
        else if (_currentPatrolX <= patrolLeftBound)
        {
            _currentPatrolX = patrolLeftBound;
            _moveDirection = 1;
            _isFacingLeft = false;
            ApplyFacingDirection();
            StartPause();
        }
        
        // Apply position
        transform.position = _startPosition + new Vector3(_currentPatrolX, 0f, 0f);
    }

    private void StartPause()
    {
        if (pauseAtEndpoints > 0f)
        {
            _isPaused = true;
            _pauseTimer = pauseAtEndpoints;
        }
    }

    /// <summary>
    /// Flips the scissors to face the current movement direction.
    /// </summary>
    private void ApplyFacingDirection()
    {
        switch (flipMethod)
        {
            case FlipMethod.ScaleX:
                // Flip by inverting the X scale
                Vector3 scale = transform.localScale;
                scale.x = _isFacingLeft ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
                transform.localScale = scale;
                break;
                
            case FlipMethod.RotateY180:
                // Flip by rotating 180 degrees on Y axis
                transform.rotation = Quaternion.Euler(0f, _isFacingLeft ? 180f : 0f, 0f);
                break;
                
            case FlipMethod.RotateZ180:
                // Flip by rotating 180 degrees on Z axis (for top-down or specific sprite orientations)
                transform.rotation = Quaternion.Euler(0f, 0f, _isFacingLeft ? 180f : 0f);
                break;
        }
    }

    private void UpdateScissorAnimation()
    {
        // Animate blade angle
        float angleChange = snipSpeed * Time.deltaTime;
        
        if (_isOpening)
        {
            _currentBladeAngle += angleChange;
            if (_currentBladeAngle >= maxOpenAngle)
            {
                _currentBladeAngle = maxOpenAngle;
                _isOpening = false;
            }
        }
        else
        {
            _currentBladeAngle -= angleChange;
            if (_currentBladeAngle <= minCloseAngle)
            {
                _currentBladeAngle = minCloseAngle;
                _isOpening = true;
            }
        }
        
        // Apply rotation to blades
        // Top blade rotates counter-clockwise (positive angle)
        // Bottom blade rotates clockwise (negative angle)
        if (topBlade != null)
        {
            topBlade.localRotation = Quaternion.Euler(0f, 0f, _currentBladeAngle);
        }
        
        if (bottomBlade != null)
        {
            bottomBlade.localRotation = Quaternion.Euler(0f, 0f, -_currentBladeAngle);
        }
    }

    private void CheckForRopeOverlap()
    {
        // Manual overlap check if not using trigger detection
        if (_topBladeCollider != null && CheckColliderForRope(_topBladeCollider))
        {
            CutGrapplingHook();
            return;
        }
        
        if (_bottomBladeCollider != null && CheckColliderForRope(_bottomBladeCollider))
        {
            CutGrapplingHook();
        }
    }

    private bool CheckColliderForRope(BoxCollider2D collider)
    {
        // Get the collider bounds in world space
        Bounds bounds = collider.bounds;
        
        // Check for overlapping colliders on the rope layer
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            bounds.center, 
            bounds.size, 
            collider.transform.eulerAngles.z, 
            ropeLayerMask
        );
        
        return hits.Length > 0;
    }

    /// Called when a blade detects contact with a grappling hook rope.
    /// Cuts the rope by forcing the grappling hook to release.
    /// </summary>
    public void CutGrapplingHook()
    {
        CutGrapplingHookAtPoint(transform.position);
    }
    
    /// <summary>
    /// Cuts the grappling hook rope at a specific world position.
    /// </summary>
    /// <param name="cutPoint">World position where the cut occurs</param>
    public void CutGrapplingHookAtPoint(Vector2 cutPoint)
    {
        // Find the player's grappling hook
        GrapplingHook playerGrapple = FindPlayerGrapplingHook();
        
        if (playerGrapple != null && playerGrapple.IsActive)
        {
            // Cut the rope - this will NOT give the player a jump boost
            playerGrapple.CutRope(cutPoint);
            
            // Optional: Add visual/audio feedback here
            OnGrappleCut();
        }
    }


    private GrapplingHook FindPlayerGrapplingHook()
    {
        GrapplingHook hook = FindFirstObjectByType<GrapplingHook>();
        return hook;
    }

    /// <summary>
    /// Called when a grapple is successfully cut. Override or extend for effects.
    /// </summary>
    protected virtual void OnGrappleCut()
    {
        
    }

    private void OnDrawGizmosSelected()
    {
        // Draw patrol path in editor
        Vector3 startPos = Application.isPlaying ? _startPosition : transform.position;
        Vector3 leftPoint = startPos + new Vector3(patrolLeftBound, 0f, 0f);
        Vector3 rightPoint = startPos + new Vector3(patrolRightBound, 0f, 0f);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(leftPoint, rightPoint);
        Gizmos.DrawWireSphere(leftPoint, 0.2f);
        Gizmos.DrawWireSphere(rightPoint, 0.2f);
        
        // Draw blade arcs
        if (topBlade != null)
        {
            Gizmos.color = Color.red;
            DrawBladeArc(topBlade.position, minCloseAngle, maxOpenAngle);
        }
        
        if (bottomBlade != null)
        {
            Gizmos.color = Color.blue;
            DrawBladeArc(bottomBlade.position, -maxOpenAngle, -minCloseAngle);
        }
    }

    private void DrawBladeArc(Vector3 pivot, float minAngle, float maxAngle)
    {
        float bladeLength = 1f;
        int segments = 10;
        float angleStep = (maxAngle - minAngle) / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (minAngle + angleStep * i) * Mathf.Deg2Rad;
            float angle2 = (minAngle + angleStep * (i + 1)) * Mathf.Deg2Rad;
            
            Vector3 p1 = pivot + new Vector3(Mathf.Cos(angle1), Mathf.Sin(angle1), 0f) * bladeLength;
            Vector3 p2 = pivot + new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2), 0f) * bladeLength;
            
            Gizmos.DrawLine(p1, p2);
        }
    }
}

/// <summary>
/// Method used to flip the scissors when changing patrol direction.
/// </summary>
public enum FlipMethod
{
    /// <summary>Flip by negating the X scale (most common for 2D sprites).</summary>
    ScaleX,
    
    /// <summary>Flip by rotating 180 degrees on Y axis.</summary>
    RotateY180,
    
    /// <summary>Flip by rotating 180 degrees on Z axis.</summary>
    RotateZ180
}
