using System.Collections;
using Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class RoomTransitionHandler : MonoBehaviour
{
    [SerializeField]
    private Image transitionImage = null;

    public IEnumerator MakeTransition(CinemachineVirtualCamera oldCamera, CinemachineVirtualCamera newCamera)
    {
        if (oldCamera)
        {
            yield return transitionImage.DOFade(1, 0.25f).SetEase(Ease.OutSine).WaitForCompletion();

            oldCamera.enabled = false;

            yield return new WaitForSeconds(0.25f);
        }

        Time.timeScale = 0;

        if (newCamera)
            newCamera.enabled = true;

        Time.timeScale = 1f;
        
        yield return transitionImage.DOFade(0, 0.25f).From(1).SetEase(Ease.InSine).SetUpdate(true).WaitForCompletion();

    }
}