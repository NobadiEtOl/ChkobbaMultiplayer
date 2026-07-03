using UnityEngine;
using DG.Tweening;

public class BreathingAnimation : MonoBehaviour
{
    [SerializeField] private float duration = 0.8f;
    [SerializeField] private float targetScale = 1.15f;
    
    private Tween breathTween;
    private Vector3 initialScale;
    private bool shouldBeAnimating = false;

    private void Awake()
    {
        initialScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (shouldBeAnimating)
        {
            PlayTween();
        }
    }

    private void OnDisable()
    {
        KillTween();
    }

    public void StartAnimation()
    {
        shouldBeAnimating = true;
        if (gameObject.activeInHierarchy)
        {
            PlayTween();
        }
    }

    public void StopAnimation()
    {
        shouldBeAnimating = false;
        KillTween();
        transform.localScale = initialScale;
    }

    private void PlayTween()
    {
        if (breathTween != null && breathTween.IsActive()) return;
        
        transform.localScale = initialScale;
        breathTween = transform.DOScale(initialScale * targetScale, duration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
    }

    private void KillTween()
    {
        if (breathTween != null)
        {
            breathTween.Kill();
            breathTween = null;
        }
    }
}