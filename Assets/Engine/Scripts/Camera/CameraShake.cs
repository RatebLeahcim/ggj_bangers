using UnityEngine;

public class CameraShake : MonoBehaviour
{
    Vector3 originalPos;
    float shakeTime;
    float shakeStrength;
    float frequency;

    void Awake()
    {
        
    }

    void Update()
    {
        if (shakeTime > 0)
        {
            originalPos = transform.localPosition;
            float x = (Mathf.PerlinNoise(Time.time * frequency, 0f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(0f, Time.time * frequency) - 0.5f) * 2f;

            transform.localPosition = originalPos + new Vector3(x, y, 0) * shakeStrength;

            shakeTime -= Time.deltaTime;
        }
    }

    public void Shake(float duration, float strength, float freq = 25f)
    {
        shakeTime = Mathf.Max(shakeTime, duration);
        shakeStrength = strength;
        frequency = freq;
    }
}
