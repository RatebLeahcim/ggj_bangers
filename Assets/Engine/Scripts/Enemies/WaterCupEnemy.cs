using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class WaterCupEnemy : MonoBehaviour
{
    private float _spillFloat;
    private bool _spilled;
    public Sprite[] SpillSprites;
    public SpriteRenderer Renderer;
    public GameObject SpillObject;
    public Transform SpillLeftTransform, SpillRightTransform, SpillTransform;
    public VisualEffect SpillVFX;

    private void Update()
    {
        if(_spilled){ 
                    SpillObject.transform.SetPositionAndRotation(SpillTransform.position,SpillTransform.rotation);

            return; }

        float tip = -GetTipAmount();
        if(tip > 0.1f || tip < -0.1f)
        {
            Animate(tip);
        }
        if(tip > 0.98f){ Spill(true); }
        else if(tip < -0.98f){ Spill(false); }
    }

    public void Animate(float tip)
    {
        if(tip < 0){ Renderer.flipX = true; tip *= -1f; }
        float sprite = Mathf.Lerp(0,3,tip);
        int spriteInt = Mathf.CeilToInt(sprite);
        Renderer.sprite = SpillSprites[spriteInt];
    }

    private void Spill(bool right)
    {
        _spilled = true;
        Transform SpillTransform = right? SpillRightTransform : SpillLeftTransform;
        SpillObject.transform.SetPositionAndRotation(SpillTransform.position,SpillTransform.rotation);
        SpillObject.SetActive(true);
        StartCoroutine(SpillRoutine(right));
    }

    private IEnumerator SpillRoutine(bool right)
    {
        if (right)
        {
            while(_spillFloat < 1)
            {
                _spillFloat += Time.deltaTime;
                SpillVFX.SetFloat("SpillAmount",_spillFloat);
                yield return null;
            }
        }
        else
        {
            while(_spillFloat > -1)
            {
                _spillFloat -= Time.deltaTime;
                SpillVFX.SetFloat("SpillAmount",_spillFloat);
                yield return null;
            }
        }
    }

    private float GetTipAmount(float maxAngle = 90f)
    {
        float z = transform.eulerAngles.z;
        if (z > 180f) z -= 360f;
        return Mathf.Clamp(z / maxAngle, -1f, 1f);
    }
}
