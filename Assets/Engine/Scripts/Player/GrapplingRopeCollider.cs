using UnityEngine;

/// <summary>
/// Adds a dynamic BoxCollider2D to the grappling hook rope that updates its position
/// and size to match the rope visual. This allows the rope to be detected by enemies
/// like the Scissors that can cut the rope.
/// 
/// Attach this component to the same GameObject as the GrapplingHook or as a child.
/// </summary>
public class GrapplingRopeCollider : MonoBehaviour
{
    [Header("Collider Settings")]
    [Tooltip("Width of the rope collider (should match visual rope width)")]
    [SerializeField] private float colliderWidth = 0.15f;
    
    [Tooltip("Tag to apply to the collider GameObject for detection")]
    [SerializeField] private string ropeTag = "GrapplingRope";
    
    [Tooltip("Layer to assign to the collider (leave empty to use default)")]
    [SerializeField] private string ropeLayer = "GrapplingRope";
    
    [Header("References")]
    [Tooltip("Reference to the GrapplingHook component (auto-found if null)")]
    [SerializeField] private GrapplingHook grapplingHook;

    private GameObject _colliderObject;
    private BoxCollider2D _boxCollider;
    
    private void Awake()
    {
        if (grapplingHook == null)
        {
            grapplingHook = GetComponentInParent<GrapplingHook>();
            if (grapplingHook == null)
            {
                grapplingHook = GetComponent<GrapplingHook>();
            }
        }
        
        CreateColliderObject();
    }

    private void CreateColliderObject()
    {
        // Create a separate GameObject for the rope collider
        _colliderObject = new GameObject("RopeCollider");
        _colliderObject.transform.SetParent(transform);
        _colliderObject.transform.localPosition = Vector3.zero;
        
        // Apply tag if it exists
        if (!string.IsNullOrEmpty(ropeTag))
        {
            try
            {
                _colliderObject.tag = ropeTag;
            }
            catch
            {
                Debug.LogWarning($"GrapplingRopeCollider: Tag '{ropeTag}' does not exist. Please create it in the Tag Manager.");
            }
        }
        
        // Apply layer if it exists
        if (!string.IsNullOrEmpty(ropeLayer))
        {
            int layerIndex = LayerMask.NameToLayer(ropeLayer);
            if (layerIndex >= 0)
            {
                _colliderObject.layer = layerIndex;
            }
            else
            {
                Debug.LogWarning($"GrapplingRopeCollider: Layer '{ropeLayer}' does not exist. Please create it in the Layer Manager.");
            }
        }
        
        // Add BoxCollider2D as trigger
        _boxCollider = _colliderObject.AddComponent<BoxCollider2D>();
        _boxCollider.isTrigger = true;
        
        // Start disabled
        _colliderObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (grapplingHook == null) return;
        
        // Only show collider when hook is active
        if (grapplingHook.IsActive)
        {
            UpdateCollider();
            
            if (!_colliderObject.activeSelf)
            {
                _colliderObject.SetActive(true);
            }
        }
        else
        {
            if (_colliderObject.activeSelf)
            {
                _colliderObject.SetActive(false);
            }
        }
    }

    private void UpdateCollider()
    {
        if (_boxCollider == null || grapplingHook == null) return;

        // Get rope endpoints from the grappling hook
        // We need to access the hook position and player position
        Vector2 playerPos = grapplingHook.transform.position;
        
        // Hook origin offset - we'll estimate this based on standard setup
        // Ideally this would be exposed from GrapplingHook, but we can approximate
        Vector2 hookOriginOffset = new Vector2(0f, 0.7f);
        Vector2 ropeStart = playerPos + hookOriginOffset;
        
        // For the end position, we need to get it from the grappling hook's internal state
        // Since we can't access private fields directly, we'll use reflection or
        // the visual renderer's position if available
        
        // Alternative approach: Get position from the SpriteRopeRenderer if available
        SpriteRopeRenderer spriteRenderer = grapplingHook.GetComponentInChildren<SpriteRopeRenderer>();
        if (spriteRenderer != null && spriteRenderer.IsVisible)
        {
            // Use the sprite renderer's child transform as the midpoint
            Transform ropeSprite = spriteRenderer.transform.Find("RopeSprite");
            if (ropeSprite != null)
            {
                // Match the rope sprite's transform
                _colliderObject.transform.position = ropeSprite.position;
                _colliderObject.transform.rotation = ropeSprite.rotation;
                
                // Get size from the sprite renderer
                SpriteRenderer sr = ropeSprite.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    if (sr.drawMode == SpriteDrawMode.Tiled)
                    {
                        _boxCollider.size = sr.size;
                    }
                    else
                    {
                        // For stretched mode, use localScale
                        _boxCollider.size = new Vector2(
                            ropeSprite.localScale.x,
                            colliderWidth
                        );
                    }
                }
                return;
            }
        }
        
        // Fallback: Use LineRenderer if available
        LineRenderer lineRenderer = grapplingHook.GetComponentInChildren<LineRenderer>();
        if (lineRenderer != null && lineRenderer.enabled && lineRenderer.positionCount >= 2)
        {
            Vector3 start = lineRenderer.GetPosition(0);
            Vector3 end = lineRenderer.GetPosition(1);
            
            PositionColliderBetweenPoints(start, end);
        }
    }

    private void PositionColliderBetweenPoints(Vector3 start, Vector3 end)
    {
        // Calculate midpoint
        Vector3 midpoint = (start + end) / 2f;
        
        // Calculate length
        float length = Vector3.Distance(start, end);
        
        // Calculate angle
        Vector3 direction = end - start;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Apply transform
        _colliderObject.transform.position = midpoint;
        _colliderObject.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        
        // Set collider size
        _boxCollider.size = new Vector2(length, colliderWidth);
        _boxCollider.offset = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (_colliderObject != null)
        {
            Destroy(_colliderObject);
        }
    }
}
