using UnityEngine;

public class CameraShake : MonoBehaviour
{
    Vector3 originalPos;
    float shakeTime;
    float shakeStrength;
    float frequency;

    void Awake()
    {
        originalPos = transform.localPosition;
    }

    void Update()
    {
        if (shakeTime > 0)
        {
            float x = (Mathf.PerlinNoise(Time.time * frequency, 0f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(0f, Time.time * frequency) - 0.5f) * 2f;

            transform.localPosition = originalPos + new Vector3(x, y, 0) * shakeStrength;

            shakeTime -= Time.deltaTime;
        }
        else
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                originalPos,
                Time.deltaTime * 15f
            );
        }
    }

    public void Shake(float duration, float strength, float freq = 25f)
    {
        shakeTime = Mathf.Max(shakeTime, duration);
        shakeStrength = strength;
        frequency = freq;
    }
}
