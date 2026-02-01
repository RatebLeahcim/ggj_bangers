using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class DisabledPanel : MonoBehaviour
{
    private CanvasGroup _group;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();    
    }

    private void Update()
    {
        if(_group.alpha <= 0.025f)
        {
            _group.blocksRaycasts = false;
        }
        else
        {
            _group.blocksRaycasts = true;
        }
    }
}
