using UnityEngine;

public class SpriteRopeRenderer : MonoBehaviour
{
    [Header("Sprite Settings")]
    [Tooltip("The sprite to use for the rope. Should be a horizontal rope segment (pointing right).")]
    [SerializeField] private Sprite ropeSprite;
    
    [Tooltip("Width of the rope in world units")]
    [SerializeField] private float ropeWidth = 0.1f;
    
    [Tooltip("If true, tiles the sprite along the rope length. If false, stretches the sprite.")]
    [SerializeField] private bool tileSprite = false;
    
    [Tooltip("Pixels per unit for tiling calculation. Higher = smaller tiles.")]
    [SerializeField] private float pixelsPerUnit = 120f;
    
    [Tooltip("Sorting layer for the rope sprite")]
    [SerializeField] private string sortingLayerName = "Default";
    
    [Tooltip("Order in sorting layer")]
    [SerializeField] private int sortingOrder = 0;
    
    [Header("Color Settings")]
    [Tooltip("Color tint applied to the rope sprite")]
    [SerializeField] private Color ropeTint = Color.white;

    private SpriteRenderer _spriteRenderer;
    private Vector2 _startPoint;
    private Vector2 _endPoint;
    private bool _isVisible = false;

    private void Awake()
    {
        CreateSpriteRenderer();
    }

    private void CreateSpriteRenderer()
    {
        GameObject ropeObj = new GameObject("RopeSprite");
        ropeObj.transform.SetParent(transform);
        ropeObj.transform.localPosition = Vector3.zero;
        
        _spriteRenderer = ropeObj.AddComponent<SpriteRenderer>();
        _spriteRenderer.sprite = ropeSprite;
        _spriteRenderer.color = ropeTint;
        _spriteRenderer.sortingLayerName = sortingLayerName;
        _spriteRenderer.sortingOrder = sortingOrder;
        
        if (tileSprite && ropeSprite != null)
        {
            _spriteRenderer.drawMode = SpriteDrawMode.Tiled;
        }
        else
        {
            _spriteRenderer.drawMode = SpriteDrawMode.Sliced;
        }
        
        // Start hidden
        _spriteRenderer.enabled = false;
    }

    public void RefreshSprite()
    {
        if (_spriteRenderer != null)
        {
            _spriteRenderer.sprite = ropeSprite;
            _spriteRenderer.color = ropeTint;
            
            if (tileSprite && ropeSprite != null)
            {
                _spriteRenderer.drawMode = SpriteDrawMode.Tiled;
            }
            else
            {
                _spriteRenderer.drawMode = SpriteDrawMode.Sliced;
            }
        }
    }

    /// <param name="start">The starting point (usually player position + offset)</param>
    /// <param name="end">The ending point (hook position)</param>
    public void SetRopePoints(Vector2 start, Vector2 end)
    {
        _startPoint = start;
        _endPoint = end;
        UpdateRopeVisual();
    }

    public void Show()
    {
        _isVisible = true;
        if (_spriteRenderer != null)
        {
            _spriteRenderer.enabled = true;
        }
    }

    /// <summary>
    /// Hides the rope visual.
    /// </summary>
    public void Hide()
    {
        _isVisible = false;
        if (_spriteRenderer != null)
        {
            _spriteRenderer.enabled = false;
        }
    }

    public bool IsVisible => _isVisible;

    private void UpdateRopeVisual()
    {
        if (_spriteRenderer == null || !_isVisible) return;
        
        Vector2 midpoint = (_startPoint + _endPoint) / 2f;
        
        float ropeLength = Vector2.Distance(_startPoint, _endPoint);
        
        Vector2 direction = _endPoint - _startPoint;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        _spriteRenderer.transform.position = midpoint;
        
        _spriteRenderer.transform.rotation = Quaternion.Euler(0, 0, angle);
        
        if (ropeSprite != null)
        {
            if (tileSprite)
            {
                _spriteRenderer.size = new Vector2(ropeLength, ropeWidth);
            }
            else
            {
                float spriteWidth = ropeSprite.rect.width / pixelsPerUnit;
                float spriteHeight = ropeSprite.rect.height / pixelsPerUnit;
                
                float scaleX = ropeLength / spriteWidth;
                float scaleY = ropeWidth / spriteHeight;
                
                _spriteRenderer.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            }
        }
        else
        {
            _spriteRenderer.transform.localScale = new Vector3(ropeLength, ropeWidth, 1f);
        }
    }

    public void SetColor(Color color)
    {
        ropeTint = color;
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = color;
        }
    }


    public void SetWidth(float width)
    {
        ropeWidth = width;
        UpdateRopeVisual();
    }


    public void SetTilingMode(bool tile)
    {
        tileSprite = tile;
        if (_spriteRenderer != null)
        {
            _spriteRenderer.drawMode = tile ? SpriteDrawMode.Tiled : SpriteDrawMode.Sliced;
        }
        UpdateRopeVisual();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_spriteRenderer != null)
        {
            _spriteRenderer.sprite = ropeSprite;
            _spriteRenderer.color = ropeTint;
            _spriteRenderer.sortingLayerName = sortingLayerName;
            _spriteRenderer.sortingOrder = sortingOrder;
            
            if (tileSprite && ropeSprite != null)
            {
                _spriteRenderer.drawMode = SpriteDrawMode.Tiled;
            }
            else
            {
                _spriteRenderer.drawMode = SpriteDrawMode.Sliced;
            }
        }
    }
#endif
}
