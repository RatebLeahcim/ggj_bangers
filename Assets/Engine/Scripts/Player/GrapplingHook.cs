using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GrapplingHook : MonoBehaviour
{
    [Header("Grappling Hook Settings")]
    [Tooltip("Speed at which the hook travels upward")]
    [SerializeField] private float hookSpeed = 18f;
    
    [Tooltip("Layer mask for surfaces the hook can attach to")]
    [SerializeField] private LayerMask grappleableLayer;
    
    [Tooltip("Width of the hook raycast")]
    [SerializeField] private float hookWidth = 0.2f;
    
    [Tooltip("Offset from player center where the hook originates (e.g., left side of player)")]
    [SerializeField] private Vector2 hookOriginOffset = new Vector2(0f, 0.7f);

    [Header("Swing Settings")]
    [Tooltip("Overall speed multiplier for all swing forces (higher = snappier swinging)")]
    [SerializeField] private float swingSpeedScale = 1.5f;
    
    [Tooltip("Base force applied when swinging left/right")]
    [SerializeField] private float swingForce = 12f;
    
    [Tooltip("Damping applied to swing motion (lower = faster deceleration, 1.0 = no damping)")]
    [SerializeField] private float swingDamping = 0.98f;
    
    [Tooltip("Maximum swing speed - caps how fast the player can swing")]
    [SerializeField] private float maxSwingSpeed = 10f;
    
    [Header("Pendulum Gravity")]
    [Tooltip("Gravity multiplier when swinging DOWN (higher = faster acceleration through bottom)")]
    [SerializeField] private float descendingGravityMultiplier = 2.0f;
    
    [Tooltip("Gravity multiplier when swinging UP (lower = more resistance, feels heavier)")]
    [SerializeField] private float ascendingGravityMultiplier = 1.5f;
    
    [Header("Momentum Building")]
    [Tooltip("Effectiveness when pushing WITH swing direction (0-1)")]
    [SerializeField] private float withMomentumEffectiveness = 0.6f;
    
    [Tooltip("Effectiveness when starting from stationary - allows initiating a swing (0-1)")]
    [SerializeField] private float stationaryEffectiveness = 0.6f;
    
    [Tooltip("Braking force when pushing AGAINST swing direction (higher = faster stop)")]
    [SerializeField] private float againstMomentumBraking = 0.1f;
    
    [Tooltip("Velocity threshold to consider player as stationary")]
    [SerializeField] private float stationaryThreshold = 0.5f;
    
    [Tooltip("How much height reduces swing effectiveness (0 = no reduction, 1 = full reduction when at top)")]
    [SerializeField] private float heightPenaltyStrength = 0.6f;
    
    [Header("Release Settings")]
    [Tooltip("Multiplier applied to horizontal velocity when releasing the hook (1.0 = no boost)")]
    [SerializeField] private float releaseHorizontalBoostMultiplier = 3.0f;
    
    [Tooltip("Additional upward boost when releasing (adds to current vertical velocity)")]
    [SerializeField] private float releaseUpwardBoost = 2f;
    
    [Tooltip("Jump force applied when releasing the grapple (like a regular jump)")]
    [SerializeField] private float releaseJumpForce = 8f;
    
    [Tooltip("If true, the release jump overrides downward velocity. If false, jump force is added to current velocity.")]
    [SerializeField] private bool jumpOverridesDownwardVelocity = true;
    
    [Tooltip("Minimum upward velocity after release (prevents weak jumps when releasing at bottom of swing)")]
    [SerializeField] private float minimumReleaseUpwardVelocity = 5f;
    
    [Tooltip("Speed at which the rope retracts when player releases the hook")]
    [SerializeField] private float releaseRetractSpeed = 25f;
    
    [Tooltip("Speed at which the cut rope ends retract (each half retracts in opposite directions)")]
    [SerializeField] private float cutRetractSpeed = 20f;
    
    [Header("Rope Length Control")]
    [Tooltip("If true, player can control rope length with up/down input while hanging")]
    [SerializeField] private bool enableRopeLengthControl = true;
    
    [Tooltip("Speed at which the rope reels in when pressing up")]
    [SerializeField] private float reelInSpeed = 8f;
    
    [Tooltip("Speed at which the rope reels out when pressing down")]
    [SerializeField] private float reelOutSpeed = 6f;
    
    [Tooltip("Minimum rope length when fully reeled in")]
    [SerializeField] private float minRopeLength = 1f;

    [Header("Visual Settings")]
    [Tooltip("Visual mode for the rope - use LineRenderer or SpriteRenderer")]
    [SerializeField] private RopeVisualMode visualMode = RopeVisualMode.LineRenderer;
    
    [Tooltip("Line renderer for the rope visual (used when visualMode is LineRenderer)")]
    [SerializeField] private LineRenderer ropeRenderer;
    
    [Tooltip("Sprite renderer for the rope visual (used when visualMode is SpriteRenderer)")]
    [SerializeField] private SpriteRopeRenderer spriteRopeRenderer;
    
    [Tooltip("Color of the rope")]
    [SerializeField] private Color ropeColor = Color.white;
    
    [Tooltip("Width of the rope")]
    [SerializeField] private float ropeWidth = 2.0f;
    
    [Header("Secondary Rope (for cut animation)")]
    [Tooltip("Optional secondary LineRenderer for cut animation - shows the attach-side rope segment")]
    [SerializeField] private LineRenderer secondaryRopeRenderer;
    
    [Tooltip("Optional secondary SpriteRopeRenderer for cut animation")]
    [SerializeField] private SpriteRopeRenderer secondarySpriteRopeRenderer;

    [Header("Rotation Normalization")]
    [Tooltip("If true, player rotation normalizes to target angle when hook is active")]
    [SerializeField] private bool normalizeRotationOnHook = true;
    
    [Tooltip("Target rotation angle (in degrees) that the player should rotate to when grappling. 0 = upright.")]
    [SerializeField] private float targetRotationAngle = 0f;
    
    [Tooltip("Base speed for rotation normalization (degrees per second)")]
    [SerializeField] private float baseNormalizationSpeed = 180f;
    
    [Tooltip("Additional speed multiplier based on rotation angle (higher = faster correction for large rotations)")]
    [SerializeField] private float rotationSpeedMultiplier = 2f;
    
    [Tooltip("Angle threshold below which rotation snaps to target")]
    [SerializeField] private float rotationSnapThreshold = 1f;
    
    [Header("Tape Unwinding Effect")]
    [Tooltip("If true, player rotates as the tape/rope is deployed (unwinding effect)")]
    [SerializeField] private bool enableUnwindingRotation = true;
    
    [Tooltip("Degrees to rotate per unit of rope deployed. Negative = clockwise unwind.")]
    [SerializeField] private float degreesPerRopeUnit = -45f;
    
    [Tooltip("Maximum rotation from unwinding (prevents excessive spinning)")]
    [SerializeField] private float maxUnwindRotation = 360f;

    [Header("Status")]
    public GrapplingHookStatus Status = new();

    private Rigidbody2D _rb;
    private PlayerStateMachine _stateMachine;
    private PlayerInputBridge _inputBridge;
    
    private enum HookState { Idle, Shooting, Attached, Retracting, ReleaseRetracting, Cut }
    private HookState _currentState = HookState.Idle;
    
    private Vector2 _hookPosition;
    private Vector2 _attachPoint;
    private Rigidbody2D _attachedBody;
    private float _currentRopeLength;
    private float _shootDistance;

    // Rotation normalization state
    private RigidbodyConstraints2D _originalConstraints;
    private bool _rotationFrozen = false;
    
    // Unwinding state
    private float _unwindStartAngle = 0f;
    private float _previousShootDistance = 0f;
    
    // Release retraction state
    private Vector2 _releaseRetractStart;
    private Vector2 _releaseRetractEnd;
    private float _releaseRetractProgress;
    
    // Cut state - animates two rope halves retracting from cut point
    private Vector2 _cutPoint;
    private Vector2 _cutRopePlayerEnd;  // End retracting toward player
    private Vector2 _cutRopeAttachEnd;  // End retracting toward original attach
    private float _cutRetractProgress;
    
    // Rope length control state
    private float _maxRopeLengthForAttachment;  // Max length for current attachment (can reel out to this)

    public bool IsHooked => _currentState == HookState.Attached;
    public bool IsShooting => _currentState == HookState.Shooting;
    public bool IsActive => _currentState != HookState.Idle;
    
    /// <summary>
    /// Current distance the hook has traveled while shooting.
    /// </summary>
    public float CurrentShootDistance => _shootDistance;
    
    /// <summary>
    /// Current rope length when attached to a surface.
    /// </summary>
    public float CurrentRopeLength => _currentRopeLength;

    
    //FMOD Event Loading
    [Header("FMOD Events")]
    public FMODUnity.EventReference AttachSoundEvent;
    public FMODUnity.EventReference DetachSoundEvent;
    public FMODUnity.EventReference ShootSoundEvent;
    public FMODUnity.EventReference WhooshSoundEvent;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        SetupRopeRenderer();
    }

    private void Start()
    {
        _stateMachine = PlayerStateMachine.Instance;
        _inputBridge = PlayerInputBridge.Instance;
    }

    private void SetupRopeRenderer()
    {
        if (visualMode == RopeVisualMode.LineRenderer || spriteRopeRenderer == null)
        {
            if (ropeRenderer == null)
            {
                GameObject ropeObj = new GameObject("RopeRenderer");
                ropeObj.transform.SetParent(transform);
                ropeRenderer = ropeObj.AddComponent<LineRenderer>();
            }
            
            ropeRenderer.positionCount = 2;
            ropeRenderer.startWidth = ropeWidth;
            ropeRenderer.endWidth = ropeWidth;
            ropeRenderer.material = new Material(Shader.Find("Sprites/Default"));
            ropeRenderer.startColor = ropeColor;
            ropeRenderer.endColor = ropeColor;
            ropeRenderer.enabled = false;
        }
        
        if (visualMode == RopeVisualMode.SpriteRenderer && spriteRopeRenderer != null)
        {
            spriteRopeRenderer.SetColor(ropeColor);
            spriteRopeRenderer.SetWidth(ropeWidth);
            spriteRopeRenderer.Hide();
        }
    }

    private void Update()
    {
        Status.Regenerate(Time.deltaTime);
        
        switch (_currentState)
        {
            case HookState.Shooting:
                UpdateShooting();
                break;
            case HookState.Attached:
                UpdateAttached();
                break;
            case HookState.Retracting:
                UpdateRetracting();
                break;
            case HookState.ReleaseRetracting:
                UpdateReleaseRetracting();
                break;
            case HookState.Cut:
                UpdateCutAnimation();
                break;
        }
        
        if (normalizeRotationOnHook && _currentState != HookState.Idle)
        {
            if (enableUnwindingRotation && _currentState == HookState.Shooting)
            {
                ApplyUnwindingRotation();
            }
            else
            {
                NormalizeRotation();
            }
        }
        
        UpdateRopeVisual();
    }
    
    private void ApplyUnwindingRotation()
    {
        if (!_rotationFrozen && _rb != null)
        {
            _originalConstraints = _rb.constraints;
            _rb.constraints = _originalConstraints | RigidbodyConstraints2D.FreezeRotation;
            _rb.angularVelocity = 0f;
            _rotationFrozen = true;
            
            _unwindStartAngle = transform.eulerAngles.z;
            if (_unwindStartAngle > 180f) _unwindStartAngle -= 360f;
        }
        
        if (_rb != null)
        {
            _rb.angularVelocity = 0f;
        }
        
        float unwindRotation = _shootDistance * degreesPerRopeUnit;
        
        unwindRotation = Mathf.Clamp(unwindRotation, -maxUnwindRotation, maxUnwindRotation);
        
        float newAngle = _unwindStartAngle + unwindRotation;
        
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }
    
    private void NormalizeRotation()
    {
        if (!_rotationFrozen && _rb != null)
        {
            _originalConstraints = _rb.constraints;
            _rb.constraints = _originalConstraints | RigidbodyConstraints2D.FreezeRotation;
            _rb.angularVelocity = 0f;
            _rotationFrozen = true;
        }
        
        if (_rb != null)
        {
            _rb.angularVelocity = 0f;
        }
        
        float currentAngle = transform.eulerAngles.z;
        if (currentAngle > 180f) currentAngle -= 360f;
        
        float normalizedTarget = targetRotationAngle;
        if (normalizedTarget > 180f) normalizedTarget -= 360f;
        if (normalizedTarget < -180f) normalizedTarget += 360f;
        
        float angleDifference = currentAngle - normalizedTarget;
        
        if (angleDifference > 180f) angleDifference -= 360f;
        if (angleDifference < -180f) angleDifference += 360f;
        
        if (Mathf.Abs(angleDifference) < rotationSnapThreshold)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, normalizedTarget);
            return;
        }
        
        float angleRatio = Mathf.Abs(angleDifference) / 180f;
        float currentSpeed = baseNormalizationSpeed * (1f + angleRatio * rotationSpeedMultiplier);
        
        float maxRotationThisFrame = currentSpeed * Time.deltaTime;
        
        float newAngle;
        if (Mathf.Abs(angleDifference) <= maxRotationThisFrame)
        {
            newAngle = normalizedTarget;
        }
        else
        {
            newAngle = currentAngle - Mathf.Sign(angleDifference) * maxRotationThisFrame;
        }
        
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }
    
    private void RestoreRotationConstraints()
    {
        if (_rotationFrozen && _rb != null)
        {
            _rb.constraints = _originalConstraints;
            _rotationFrozen = false;
        }
    }

    private void FixedUpdate()
    {
        if (_currentState == HookState.Attached)
        {
            ApplySwingPhysics();
        }
    }

    public bool TryShootHook()
    {
        if (_currentState != HookState.Idle) return false;
        if (!Status.HasRopeAvailable()) return false;
        
        _hookPosition = (Vector2)transform.position + hookOriginOffset;
        _shootDistance = 0f;
        _currentState = HookState.Shooting;
        FMODUnity.RuntimeManager.PlayOneShot(ShootSoundEvent, transform.position);
        
        Vector2 ropeOrigin = (Vector2)transform.position + hookOriginOffset;
        InitializeRopeVisual(ropeOrigin, _hookPosition);
        ShowRopeVisual();
        
        return true;
    }

    public void ReleaseHook()
    {
        if (_currentState == HookState.Attached)
        {
            ApplyReleaseBoost();
            
            _releaseRetractStart = (Vector2)transform.position + hookOriginOffset;
            _releaseRetractEnd = _hookPosition;
            _releaseRetractProgress = 0f;
            
            _attachedBody = null;
            _currentState = HookState.ReleaseRetracting;
            FMODUnity.RuntimeManager.PlayOneShot(DetachSoundEvent, transform.position);
            RestoreRotationConstraints();

            if (_stateMachine != null)
            {
                _stateMachine.Conditions.IsHanging = false;
            }
        }
    }
    
    /// <param name="cutWorldPosition">World position where the cut was triggered (e.g., scissors position)</param>
    public void CutRope(Vector2 cutWorldPosition)
    {
        if (_currentState == HookState.Attached || _currentState == HookState.Shooting)
        {
            _cutRopePlayerEnd = (Vector2)transform.position + hookOriginOffset;
            _cutRopeAttachEnd = _hookPosition;
            
            _cutPoint = CalculateCutPointOnRope(cutWorldPosition.y, _cutRopePlayerEnd, _cutRopeAttachEnd);
            
            _cutRetractProgress = 0f;
            
            _attachedBody = null;
            _currentState = HookState.Cut;
            FMODUnity.RuntimeManager.PlayOneShot(DetachSoundEvent, transform.position);
            RestoreRotationConstraints();
            
            if (_stateMachine != null)
            {
                _stateMachine.Conditions.IsHanging = false;
            }
            
            // No boost applied - player just falls
        }
    }
    
    private Vector2 CalculateCutPointOnRope(float cutY, Vector2 ropeStart, Vector2 ropeEnd)
    {
        // Clamp the cut Y to be within the rope's Y range
        float minY = Mathf.Min(ropeStart.y, ropeEnd.y);
        float maxY = Mathf.Max(ropeStart.y, ropeEnd.y);
        float clampedY = Mathf.Clamp(cutY, minY, maxY);
        
        float ropeHeight = ropeEnd.y - ropeStart.y;
        if (Mathf.Abs(ropeHeight) < 0.01f)
        {
            return (ropeStart + ropeEnd) / 2f;
        }
        
        float t = (clampedY - ropeStart.y) / ropeHeight;
        t = Mathf.Clamp01(t);
        
        float cutX = Mathf.Lerp(ropeStart.x, ropeEnd.x, t);
        
        return new Vector2(cutX, clampedY);
    }
    

    public void CutRopeInstant()
    {
        if (_currentState == HookState.Attached || _currentState == HookState.Shooting)
        {
            _attachedBody = null;
            _currentState = HookState.Idle;
            HideRopeVisual();
            RestoreRotationConstraints();
            
            if (_stateMachine != null)
            {
                _stateMachine.Conditions.IsHanging = false;
            }
            
            // No boost applied - player just falls
        }
    }
    
    private void ApplyReleaseBoost()
    {
        Vector2 currentVelocity = _rb.linearVelocity;
        
        float boostedHorizontal = currentVelocity.x * releaseHorizontalBoostMultiplier;
        
        float boostedVertical = currentVelocity.y;
        
        if (jumpOverridesDownwardVelocity && boostedVertical < 0f)
        {
            boostedVertical = releaseJumpForce;
        }
        else
        {
            boostedVertical += releaseJumpForce;
        }
        
        boostedVertical += releaseUpwardBoost;
        
        if (boostedVertical < minimumReleaseUpwardVelocity)
        {
            boostedVertical = minimumReleaseUpwardVelocity;
        }
        
        _rb.linearVelocity = new Vector2(boostedHorizontal, boostedVertical);

    }

    private void UpdateShooting()
    {
        float maxDistance = Status.CurrentRopeLength;
        float travelDistance = hookSpeed * Time.deltaTime;
        
        Vector2 startPos = _hookPosition;
        Vector2 direction = Vector2.up;
        
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, travelDistance, grappleableLayer);
        
        if (hit.collider != null)
        {
            _attachPoint = hit.point;
            _attachedBody = hit.collider.attachedRigidbody;
            _currentRopeLength = Vector2.Distance(transform.position, _attachPoint);
            _maxRopeLengthForAttachment = _currentRopeLength;  // Store initial length as max
            _hookPosition = _attachPoint;
            _currentState = HookState.Attached;
            FMODUnity.RuntimeManager.PlayOneShot(AttachSoundEvent, transform.position);
            
            if (_stateMachine != null)
            {
                _stateMachine.Conditions.IsHanging = true;
            }
            return;
        }
        
        _hookPosition += direction * travelDistance;
        _shootDistance += travelDistance;
        
        if (_shootDistance >= maxDistance)
        {
            _currentState = HookState.Retracting;
        }
    }

    private void UpdateRetracting()
    {
        float retractSpeed = hookSpeed * 1.5f;
        Vector2 toPlayer = ((Vector2)transform.position - _hookPosition).normalized;
        _hookPosition += toPlayer * retractSpeed * Time.deltaTime;
        
        float distanceToPlayer = Vector2.Distance(_hookPosition, transform.position);
        if (distanceToPlayer < 0.5f)
        {
            _currentState = HookState.Idle;
            HideRopeVisual();
            RestoreRotationConstraints();
        }
    }
    
    private void UpdateReleaseRetracting()
    {
        // Animate the hook position retracting from attach point back to player
        float ropeLength = Vector2.Distance(_releaseRetractStart, _releaseRetractEnd);
        float retractAmount = releaseRetractSpeed * Time.deltaTime;
        _releaseRetractProgress += retractAmount / ropeLength;
        
        if (_releaseRetractProgress >= 1f)
        {
            // Retraction complete
            _currentState = HookState.Idle;
            HideRopeVisual();
        }
        else
        {
            // Animate the hook position moving toward player
            // The rope start stays at player, the end moves toward player
            _hookPosition = Vector2.Lerp(_releaseRetractEnd, _releaseRetractStart, _releaseRetractProgress);
        }
    }
    
    private void UpdateCutAnimation()
    {
        // Animate two rope halves retracting from the cut point
        // One half retracts toward the player, the other toward the original attach point
        
        float playerTocut = Vector2.Distance(_cutRopePlayerEnd, _cutPoint);
        float cutToAttach = Vector2.Distance(_cutPoint, _cutRopeAttachEnd);
        float maxDistance = Mathf.Max(playerTocut, cutToAttach);
        
        float retractAmount = cutRetractSpeed * Time.deltaTime;
        _cutRetractProgress += retractAmount / (maxDistance > 0f ? maxDistance : 1f);
        
        if (_cutRetractProgress >= 1f)
        {
            // Cut animation complete
            _currentState = HookState.Idle;
            HideRopeVisual();
            HideSecondaryRopeVisual();
        }
        else
        {
            // Calculate the current positions of both rope segment endpoints
            // Player-side segment: from _cutRopePlayerEnd to _cutPoint, shrinking toward player
            Vector2 playerSegmentEnd = Vector2.Lerp(_cutPoint, _cutRopePlayerEnd, _cutRetractProgress);
            
            // Attach-side segment: from _cutPoint to _cutRopeAttachEnd, shrinking toward attach
            Vector2 attachSegmentStart = Vector2.Lerp(_cutPoint, _cutRopeAttachEnd, _cutRetractProgress);
            
            // Update visuals - primary shows player segment, we'll need secondary for attach segment
            // For now, just animate the primary rope as the player-side segment
            _hookPosition = playerSegmentEnd;
            
            // Update secondary rope segment (toward attach point)
            UpdateSecondaryRopeVisual(attachSegmentStart, _cutRopeAttachEnd);
        }
    }

    private void UpdateAttached()
    {
        if (_attachedBody != null)
        {
            _attachPoint = _attachedBody.position + (_attachPoint - (Vector2)_attachedBody.transform.position);
        }
    }

    private void ApplySwingPhysics()
    {
        Vector2 playerPos = transform.position;
        Vector2 toAttach = _attachPoint - playerPos;
        float currentDistance = toAttach.magnitude;
        
        if (currentDistance > _currentRopeLength)
        {
            Vector2 correctionDirection = toAttach.normalized;
            float correction = currentDistance - _currentRopeLength;
            _rb.position = playerPos + correctionDirection * correction;
            
            Vector2 ropeDirection = correctionDirection;
            Vector2 velocityAlongRope = Vector2.Dot(_rb.linearVelocity, ropeDirection) * ropeDirection;
            if (Vector2.Dot(_rb.linearVelocity, ropeDirection) < 0)
            {
                _rb.linearVelocity -= velocityAlongRope;
            }
        }
        
        Vector2 moveInput = Vector2.zero;
        if (_inputBridge != null)
        {
            _inputBridge.Consume(PlayerInputType.Move, out moveInput);
        }
        
        // Handle rope length control with vertical input
        if (enableRopeLengthControl && Mathf.Abs(moveInput.y) > 0.1f)
        {
            if (moveInput.y > 0)
            {
                // Up input - reel in (shorten rope)
                _currentRopeLength -= reelInSpeed * Time.fixedDeltaTime;
                _currentRopeLength = Mathf.Max(_currentRopeLength, minRopeLength);
            }
            else
            {
                // Down input - reel out (lengthen rope to configured max)
                _currentRopeLength += reelOutSpeed * Time.fixedDeltaTime;
                _currentRopeLength = Mathf.Min(_currentRopeLength, Status.MaxRopeLength);
            }
        }
        
        if (Mathf.Abs(moveInput.x) > 0.1f)
        {
            // Tangent perpendicular to rope - positive X input should swing right
            Vector2 tangent = new Vector2(toAttach.normalized.y, -toAttach.normalized.x);
            
            // Get current tangential velocity (how fast we're swinging)
            float tangentialVelocity = Vector2.Dot(_rb.linearVelocity, tangent);
            
            // Check if already at max swing speed in this direction
            if (Mathf.Abs(tangentialVelocity) >= maxSwingSpeed && 
                Mathf.Sign(moveInput.x) == Mathf.Sign(tangentialVelocity))
            {
                // Already at max speed in this direction, don't add more force
            }
            else
            {
                float inputAlignment = Mathf.Sign(moveInput.x) * Mathf.Sign(tangentialVelocity);
                
                float heightDifference = _attachPoint.y - playerPos.y;
                float normalizedHeight = Mathf.Clamp01(heightDifference / _currentRopeLength);
                float heightFactor = Mathf.Lerp(1f - heightPenaltyStrength, 1f, normalizedHeight);
                
                float swingEffectiveness;
                float scaledSwingForce = swingForce * swingSpeedScale;
                
                if (Mathf.Abs(tangentialVelocity) < stationaryThreshold)
                {
                    swingEffectiveness = stationaryEffectiveness * heightFactor;
                    _rb.AddForce(tangent * moveInput.x * scaledSwingForce * swingEffectiveness);
                }
                else if (inputAlignment > 0)
                {
                    swingEffectiveness = withMomentumEffectiveness * heightFactor;
                    _rb.AddForce(tangent * moveInput.x * scaledSwingForce * swingEffectiveness);
                }
                else
                {
                    float brakingForce = againstMomentumBraking * swingSpeedScale * Mathf.Abs(tangentialVelocity);
                    Vector2 brakeDirection = -_rb.linearVelocity.normalized;
                    _rb.AddForce(brakeDirection * brakingForce, ForceMode2D.Impulse);
                }
            }
        }
        
        // Apply damping
        _rb.linearVelocity *= swingDamping;
        
        // Apply pendulum gravity modifiers
        ApplyPendulumGravity();
        
        // Clamp to max swing speed
        Vector2 tangentDir = new Vector2(toAttach.normalized.y, -toAttach.normalized.x);
        float currentTangentSpeed = Vector2.Dot(_rb.linearVelocity, tangentDir);
        if (Mathf.Abs(currentTangentSpeed) > maxSwingSpeed)
        {
            Vector2 tangentVelocity = currentTangentSpeed * tangentDir;
            Vector2 radialVelocity = _rb.linearVelocity - tangentVelocity;
            _rb.linearVelocity = radialVelocity + tangentDir * Mathf.Sign(currentTangentSpeed) * maxSwingSpeed;
        }
    }

    private void ApplyPendulumGravity()
    {
        bool isAscending = _rb.linearVelocity.y > 0.1f;

        float gravityMultiplier;
        if (isAscending)
        {
            gravityMultiplier = ascendingGravityMultiplier - 1f;
        }
        else
        {
            gravityMultiplier = descendingGravityMultiplier - 1f;
        }

        Vector2 extraGravity = Physics2D.gravity * gravityMultiplier * Time.fixedDeltaTime;
        _rb.linearVelocity += extraGravity;
    }

    private void UpdateRopeVisual()
    {
        Vector2 ropeOrigin = (Vector2)transform.position + hookOriginOffset;
        
        // In cut state, the primary rope shows player-side segment
        // (from current player origin to the retracting _hookPosition which shrinks toward player)
        Vector2 ropeEnd = _currentState == HookState.Cut ? _hookPosition : _hookPosition;
        
        if (visualMode == RopeVisualMode.SpriteRenderer && spriteRopeRenderer != null)
        {
            if (!spriteRopeRenderer.IsVisible) return;
            
            // In Cut state, show rope from player to the shrinking endpoint
            if (_currentState == HookState.Cut)
            {
                spriteRopeRenderer.SetRopePoints(_cutRopePlayerEnd, _hookPosition);
            }
            else
            {
                spriteRopeRenderer.SetRopePoints(ropeOrigin, _hookPosition);
            }
        }
        else
        {
            if (ropeRenderer == null || !ropeRenderer.enabled) return;
            
            if (_currentState == HookState.Cut)
            {
                ropeRenderer.SetPosition(0, _cutRopePlayerEnd);
                ropeRenderer.SetPosition(1, _hookPosition);
            }
            else
            {
                ropeRenderer.SetPosition(0, ropeOrigin);
                ropeRenderer.SetPosition(1, _hookPosition);
            }
        }
    }
    
    private void InitializeRopeVisual(Vector2 start, Vector2 end)
    {
        if (visualMode == RopeVisualMode.SpriteRenderer && spriteRopeRenderer != null)
        {
            spriteRopeRenderer.SetRopePoints(start, end);
        }
        else if (ropeRenderer != null)
        {
            ropeRenderer.SetPosition(0, start);
            ropeRenderer.SetPosition(1, end);
        }
    }
    
    private void ShowRopeVisual()
    {
        if (visualMode == RopeVisualMode.SpriteRenderer && spriteRopeRenderer != null)
        {
            spriteRopeRenderer.Show();
        }
        else if (ropeRenderer != null)
        {
            ropeRenderer.enabled = true;
        }
    }
    
    private void HideRopeVisual()
    {
        if (visualMode == RopeVisualMode.SpriteRenderer && spriteRopeRenderer != null)
        {
            spriteRopeRenderer.Hide();
        }
        else if (ropeRenderer != null)
        {
            ropeRenderer.enabled = false;
        }
    }
    
    private void UpdateSecondaryRopeVisual(Vector2 start, Vector2 end)
    {
        if (visualMode == RopeVisualMode.SpriteRenderer && secondarySpriteRopeRenderer != null)
        {
            if (!secondarySpriteRopeRenderer.IsVisible)
            {
                secondarySpriteRopeRenderer.Show();
            }
            secondarySpriteRopeRenderer.SetRopePoints(start, end);
        }
        else if (secondaryRopeRenderer != null)
        {
            if (!secondaryRopeRenderer.enabled)
            {
                secondaryRopeRenderer.enabled = true;
            }
            secondaryRopeRenderer.SetPosition(0, start);
            secondaryRopeRenderer.SetPosition(1, end);
        }
        // If no secondary renderer is configured, the cut animation will only show player-side rope
    }
    
    private void HideSecondaryRopeVisual()
    {
        if (visualMode == RopeVisualMode.SpriteRenderer && secondarySpriteRopeRenderer != null)
        {
            secondarySpriteRopeRenderer.Hide();
        }
        else if (secondaryRopeRenderer != null)
        {
            secondaryRopeRenderer.enabled = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (Status != null)
        {
            Vector2 origin = (Vector2)transform.position + hookOriginOffset;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, origin + Vector2.up * Status.CurrentRopeLength);
        }
        
        if (_currentState == HookState.Attached)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_attachPoint, 0.3f);
        }
    }
}

/// <summary>
/// Visual rendering mode for the grappling hook rope.
/// </summary>
public enum RopeVisualMode
{
    /// <summary>Uses Unity's LineRenderer for a simple line-based rope.</summary>
    LineRenderer,
    
    /// <summary>Uses SpriteRopeRenderer for sprite-based rope with tiling/stretching support.</summary>
    SpriteRenderer
}
