using System;
using UnityEngine;

[Serializable]
public class GrapplingHookStatus
{
    [Header("Rope Length Settings")]
    [Tooltip("Maximum rope length available")]
    [SerializeField] private float maxRopeLength = 10f;
    
    [Tooltip("Current available rope length")]
    [SerializeField] private float currentRopeLength = 10f;
    
    [Tooltip("Rate at which rope regenerates per second (0 = no regen)")]
    [SerializeField] private float regenRate = 0f;
    
    public float MaxRopeLength => maxRopeLength;
    public float CurrentRopeLength => currentRopeLength;
    public float RopePercentage => maxRopeLength > 0 ? currentRopeLength / maxRopeLength : 0f;
    
    public event Action<float, float> OnRopeLengthChanged;
    
    public void Initialize(float max, float current = -1)
    {
        maxRopeLength = max;
        currentRopeLength = current < 0 ? max : current;
        OnRopeLengthChanged?.Invoke(currentRopeLength, maxRopeLength);
    }
    
    public void SetRopeLength(float amount)
    {
        currentRopeLength = Mathf.Clamp(amount, 0f, maxRopeLength);
        OnRopeLengthChanged?.Invoke(currentRopeLength, maxRopeLength);
    }
    
    public void AddRopeLength(float amount)
    {
        SetRopeLength(currentRopeLength + amount);
    }
    
    public void ConsumeRopeLength(float amount)
    {
        SetRopeLength(currentRopeLength - amount);
    }
    
    public bool HasRopeAvailable(float amount = 0.1f)
    {
        return currentRopeLength >= amount;
    }
    
    public void Regenerate(float deltaTime)
    {
        if (regenRate > 0 && currentRopeLength < maxRopeLength)
        {
            AddRopeLength(regenRate * deltaTime);
        }
    }
    
    public void ResetToMax()
    {
        SetRopeLength(maxRopeLength);
    }
}
