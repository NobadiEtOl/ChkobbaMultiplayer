using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Opponent health display with a delayed damage shadow.
/// Assign the sliders and text in the Inspector.
/// </summary>
public class OpponentHealthDisplay : MonoBehaviour
{
    [SerializeField] private Slider healthBarSlider;
    [SerializeField] private Slider damageBarSlider;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private float healthAnimationDuration = 0.3f;
    [SerializeField] private float damageShadowDelay = 0.15f;
    [SerializeField] private float damageShadowAnimationDuration = 0.3f;

    private int maxHealth = 20;
    private int currentHealth = 0;
    private bool isInitialized = false;
    private Coroutine healthAnimationCoroutine;
    private Coroutine damageShadowCoroutine;

    public void InitializeHealthDisplay(int maxHealthPoints)
    {
        

        if (maxHealthPoints <= 0)
        {
            
            return;
        }

        maxHealth = maxHealthPoints;
        currentHealth = maxHealth;

        if (healthBarSlider == null)
        {
            
        }

        if (damageBarSlider == null)
        {
            
        }

        if (healthText == null)
        {
            
        }

        isInitialized = true;
        RefreshHealthDisplay();

        
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
        
    }

    public void UpdateHealth(int damageAccumulated)
    {
        if (!isInitialized)
        {
            
            return;
        }

        int newHealth = Mathf.Max(0, maxHealth - damageAccumulated);
        int previousDisplayedHealth = healthBarSlider != null ? Mathf.RoundToInt(healthBarSlider.value) : currentHealth;
        int previousDamageHealth = damageBarSlider != null ? Mathf.RoundToInt(damageBarSlider.value) : previousDisplayedHealth;

        currentHealth = newHealth;

        if (healthAnimationCoroutine != null)
        {
            StopCoroutine(healthAnimationCoroutine);
        }

        if (damageShadowCoroutine != null)
        {
            StopCoroutine(damageShadowCoroutine);
        }

        healthAnimationCoroutine = StartCoroutine(AnimateSliderValue(healthBarSlider, previousDisplayedHealth, currentHealth, healthAnimationDuration, true));
        damageShadowCoroutine = StartCoroutine(AnimateDamageShadow(previousDamageHealth, currentHealth));

        
    }

    private void RefreshHealthDisplay()
    {
        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = currentHealth;
        }

        if (damageBarSlider != null)
        {
            damageBarSlider.maxValue = maxHealth;
            damageBarSlider.value = currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{currentHealth} / {maxHealth}";
        }
    }

    private IEnumerator AnimateDamageShadow(int fromValue, int toValue)
    {
        if (damageBarSlider == null)
        {
            yield break;
        }

        yield return new WaitForSeconds(damageShadowDelay);

        yield return AnimateSliderValue(damageBarSlider, fromValue, toValue, damageShadowAnimationDuration, false);

        damageShadowCoroutine = null;
    }

    private IEnumerator AnimateSliderValue(Slider slider, int fromValue, int toValue, float duration, bool updateText)
    {
        if (slider == null)
        {
            yield break;
        }

        slider.maxValue = maxHealth;

        if (duration <= 0f)
        {
            slider.value = toValue;

            if (updateText && healthText != null)
            {
                healthText.text = $"{toValue} / {maxHealth}";
            }

            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);
            slider.value = Mathf.Lerp(fromValue, toValue, progress);

            if (updateText && healthText != null)
            {
                int displayHealth = Mathf.RoundToInt(slider.value);
                healthText.text = $"{displayHealth} / {maxHealth}";
            }

            yield return null;
        }

        slider.value = toValue;

        if (updateText && healthText != null)
        {
            healthText.text = $"{toValue} / {maxHealth}";
        }

        if (slider == healthBarSlider)
        {
            currentHealth = toValue;
            healthAnimationCoroutine = null;
        }
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public bool IsDefeated()
    {
        return currentHealth <= 0;
    }
}
