using UnityEngine;

public class PlayerHealthObject : MonoBehaviour
{
    [Header("Refill Amount")]
    [Tooltip("Percentage amount to refill player health")]
    [SerializeField] private float _healthRefillAmount;
    private bool _used = false;

    private void Update()
    {
        if (_used)
        {
            Destroy(gameObject);
        }
    }
    public float GetHealth()
    {
        _used = true;
        return _healthRefillAmount;
    }
}
