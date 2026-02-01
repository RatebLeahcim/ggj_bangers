using System.Linq;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public GManager Manager;
    public CanvasGroup Group;
    public CanvasGroup[] SlideGroups;
    public int SlideIterator;
    public bool IsEnabled;
    private bool _slidesOff;

    public void Enable()
    {
        IsEnabled = true;
    }

    private void Update()
    {
        if (IsEnabled)
        {
            _slidesOff = false;
            if(Group.alpha < 1){ Group.alpha = 1;}
            int l = SlideGroups.Length;
            for(int i = 0; i < l; i++)
            {
                if(i == SlideIterator)
                {
                    if(SlideGroups[i].alpha != 1)
                    {
                        SlideGroups[i].alpha += Time.deltaTime * 4f;
                    }
                }
                else
                {
                    if(SlideGroups[i].alpha > 0)
                    {
                        SlideGroups[i].alpha -= Time.deltaTime * 8;
                    }
                }
            }
            if(PlayerInputBridge.Instance.Consume(PlayerInputType.Enter,out bool value))
            {
                if(SlideIterator < SlideGroups.Length - 1)
                {
                    SlideIterator++;
                }
                else
                {
                    IsEnabled = false;
                    Manager.StartGame();
                }
            }
        }
        else
        {
            if (!_slidesOff)
            {
                Group.alpha -= Time.deltaTime;
                if(Group.alpha <= 0)
                {
                    _slidesOff = true;
                }
            }
        }
    }
}
