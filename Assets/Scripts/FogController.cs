using System.Collections;
using UnityEngine;

public class FogController : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private string animationClip;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private Renderer fogRenderer; // Assign your SpriteRenderer or MeshRenderer here

    private Material fogMaterial;
    private Coroutine fadeCoroutine;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (fogRenderer == null)
            fogRenderer = GetComponent<Renderer>();
        if (fogRenderer != null)
            fogMaterial = fogRenderer.material;

        SetAlpha(0f); // Start fully transparent
    }

    public void StartFog()
    {
        
        if (animator != null)
        {
            animator.enabled = true;
            animator.speed = 1f;
            animator.Play(animationClip);
        }
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeAlpha(1f));
    }

    [ContextMenu("Stop Fog")]
    public void StopFog()
    {
        
        if (animator != null)
            animator.speed = 0f;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeAlpha(0f));
    }

    private IEnumerator FadeAlpha(float targetAlpha)
    {
        if (fogMaterial == null) yield break;
        float startAlpha = fogMaterial.color.a;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            SetAlpha(newAlpha);
            yield return null;
        }
        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float a)
    {
        if (fogMaterial == null) return;
        Color c = fogMaterial.color;
        c.a = a;
        fogMaterial.color = c;
    }
}