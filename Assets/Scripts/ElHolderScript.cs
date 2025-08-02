using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UIElements;

public class ElHolderScript : MonoBehaviour
{
    public static ElHolderScript LocalInstance;
    List<Animator> animatorHands = new List<Animator>();
    [SerializeField] public List<GameObject> frameObjects = new List<GameObject>();
    private int currentActivePlayer = -1;
    [SerializeField] public List<GameObject> powerTransformObjects = new List<GameObject>();
    [SerializeField] public List<GameObject> powerDictionary = new List<GameObject>();
    [SerializeField] private Transform centerTransform;
    
    // Material for the frames - assign this in the inspector
    [SerializeField] private Material frameMaterial;

    // Variables for FrameShader turn indication
    [SerializeField] private float activePlayerIntensity = 0.8f;
    [SerializeField] private float inactivePlayerIntensity = 0.3f;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private Color turnIndicationColor = Color.green;
    [SerializeField] private float color3AnimationSpeed = 0.5f;

    void Start()
    {
        if (LocalInstance == null)
        {
            LocalInstance = this;
        }
        else
        {
            Debug.LogWarning("ElHolderScript: LocalInstance already set, using existing instance.");
        }

        List<GameObject> handObjects = new List<GameObject>();

        foreach (Transform child in gameObject.transform)
        {
            handObjects.Add(child.gameObject);
            frameObjects.Add(child.gameObject);
            Animator animator = child.GetComponent<Animator>();
            if (animator != null)
            {
                animatorHands.Add(animator);
            }
        }

        // Apply the assigned material to all frames
        ApplyFrameMaterial();
    }

    private int GetHandIndex(int playerNumber)
    {
        int playerCount = DeckController.LocalInstance?.playerCount ?? 4;

        if (playerCount == 2)
        {
            return playerNumber == 0 ? 0 : 2;
        }
        else
        {
            return playerNumber;
        }
    }

    public void UpdateCurrentPlayer(int newPlayerNumber)
    {
        if (currentActivePlayer != -1)
        {
            int currentHandIndex = GetHandIndex(currentActivePlayer);

            if (currentHandIndex >= 0 && currentHandIndex < animatorHands.Count)
            {
                animatorHands[currentHandIndex].SetBool("turnLoop", false);
                animatorHands[currentHandIndex].Play("Idle", 0, 0f);
            }

            ResetFrameBackgroundColor(currentActivePlayer);
        }

        int newHandIndex = GetHandIndex(newPlayerNumber);
        if (newHandIndex >= 0 && newHandIndex < animatorHands.Count)
        {
            SetFrameBackgroundToWhite(newPlayerNumber);

            animatorHands[newHandIndex].SetBool("turnLoop", true);
            animatorHands[newHandIndex].Play("ElPlayingAnimationClip", 0, 0f);
        }

        currentActivePlayer = newPlayerNumber;
    }

    void Update()
    {

    }

    [ContextMenu("TokenActionTrue")]
    public void TokenActionTrue()
    {
        TokenActionTrue(0);
    }

    public void TokenActionTrue(int playerNumber)
    {
        if (playerNumber >= 0 && playerNumber < animatorHands.Count)
        {
            animatorHands[playerNumber].SetBool("tokenAction", true);
            animatorHands[playerNumber].Play("ElTokenAnimationClip", 0, 0f);
        }
    }

    public void OnTokenAnimationEnd(int playerNumber)
    {
        TokenActionFalse(playerNumber);
    }

    [ContextMenu("TokenActionFalse")]
    public void TokenActionFalse()
    {
        TokenActionFalse(0);
    }

    public void TokenActionFalse(int playerNumber)
    {
        if (playerNumber >= 0 && playerNumber < animatorHands.Count)
        {
            animatorHands[playerNumber].SetBool("tokenAction", false);
            animatorHands[playerNumber].Play("ElPlayingAnimationClip", 0, 0);
        }
    }

    [ContextMenu("TurnLoopTrue")]
    public void TurnActionTrue()
    {
        TurnActionTrue(0);
    }

    public void TurnActionTrue(int playerNumber)
    {
        if (playerNumber >= 0 && playerNumber < animatorHands.Count)
        {
            animatorHands[playerNumber].SetBool("turnLoop", true);
            animatorHands[playerNumber].Play("ElPlayingAnimationClip", 0, 0f);
        }
    }

    public void OnTurnAnimationEnd(int playerNumber)
    {
        TurnActionFalse(playerNumber);
    }

    [ContextMenu("TurnLoopFalse")]
    public void TurnActionFalse()
    {
        TurnActionFalse(0);
    }

    public void TurnActionFalse(int playerNumber)
    {
        if (playerNumber >= 0 && playerNumber < animatorHands.Count)
        {
            animatorHands[playerNumber].SetBool("turnLoop", false);
            animatorHands[playerNumber].Play("Idle", 0, 0f);
        }
    }

    public void TokenActionTrueAll()
    {
        for (int i = 0; i < animatorHands.Count; i++)
        {
            TokenActionTrue(i);
        }
    }

    public void TurnActionTrueAll()
    {
        for (int i = 0; i < animatorHands.Count; i++)
        {
            TurnActionTrue(i);
        }
    }

    public void ReturnAllHandsToIdle()
    {
        for (int i = 0; i < animatorHands.Count; i++)
        {
            TurnActionFalse(i);
            TokenActionFalse(i);
        }
        currentActivePlayer = -1;
    }

    public void SetHandMode(int playerCount)
    {
        if (playerCount == 2)
        {
            if (animatorHands.Count > 1)
                animatorHands[1].gameObject.SetActive(false);
            if (animatorHands.Count > 3)
                animatorHands[3].gameObject.SetActive(false);
        }
        else if (playerCount == 4)
        {
            for (int i = 0; i < animatorHands.Count; i++)
            {
                animatorHands[i].gameObject.SetActive(true);
            }
        }
    }

    public void ShowcasePower(int playerNumber, string powerName)
    {
        Debug.Log($"Showcasing power: {powerName} for player {playerNumber}");
        int handIndex = GetHandIndex(playerNumber);
        if (playerNumber == DeckController.LocalInstance.thisPlayerNumber) return;
        powerName = powerName.Replace(" ", "");

        if (handIndex < 0 || handIndex >= powerTransformObjects.Count)
        {
            Debug.LogWarning($"Invalid hand index {handIndex} for player {playerNumber}");
            return;
        }

        if (!powerTransformObjects[handIndex].activeInHierarchy)
        {
            Debug.LogWarning($"Hand {handIndex} is inactive, skipping power showcase");
            return;
        }

        GameObject powerPrefab = null;
        string targetName = powerName + "_Token";

        foreach (GameObject prefab in powerDictionary)
        {
            if (prefab != null && prefab.name.Contains(powerName))
            {
                powerPrefab = prefab;
                break;
            }
        }

        if (powerPrefab == null)
        {
            Debug.LogWarning($"Power prefab not found for: {targetName}");
            return;
        }

        StartCoroutine(ShowcasePowerCoroutine(handIndex, powerName, powerPrefab));
    }

    private IEnumerator ShowcasePowerCoroutine(int handIndex, string powerName, GameObject powerPrefab)
    {
        yield return new WaitForSeconds(0.5f);
        Debug.Log($"Showcasing power: {powerName} for hand index: {handIndex}");

        Transform startTransform = powerTransformObjects[handIndex].transform;

        GameObject powerInstance = Instantiate(powerPrefab);
        powerInstance.transform.SetParent(centerTransform.parent, true);
        SpriteRenderer sr = powerInstance.GetComponent<SpriteRenderer>();
        GameObject smokeObject = powerInstance.transform.GetChild(0).gameObject;
        smokeObject.SetActive(false);

        powerInstance.transform.position = startTransform.position;
        powerInstance.transform.localScale = Vector3.one * 100f;

        Vector3 centerPosition = centerTransform.position;

        float moveDuration = 0.1f;
        float fadeDuration = 0.2f;

        Vector3 endScale = new Vector3(200, 200, 200);

        Sequence powerSequence = DOTween.Sequence();

        powerSequence.AppendInterval(0.35f);

        powerSequence.Append(powerInstance.transform.DOMove(centerPosition, moveDuration));
        powerSequence.Join(powerInstance.transform.DOScale(endScale, moveDuration));

        powerSequence.AppendInterval(0);

        powerSequence.Append(powerInstance.transform.DORotate(powerInstance.transform.rotation.eulerAngles + new Vector3(0, 0, 15), 0.025f).SetLoops(2, LoopType.Yoyo));
        powerSequence.Append(powerInstance.transform.DORotate(powerInstance.transform.rotation.eulerAngles + new Vector3(0, 0, -12), 0.025f).SetLoops(2, LoopType.Yoyo));
        powerSequence.Join(powerInstance.transform.DOMove(centerPosition + new Vector3(0, 0, 170), 0.15f).SetLoops(2, LoopType.Yoyo));
        powerSequence.Append(powerInstance.transform.DORotate(powerInstance.transform.rotation.eulerAngles + new Vector3(0, 0, 13), 0.025f).SetLoops(2, LoopType.Yoyo));
        powerSequence.Append(powerInstance.transform.DORotate(powerInstance.transform.rotation.eulerAngles + new Vector3(0, 0, -16), 0.025f).SetLoops(2, LoopType.Yoyo));
        powerSequence.Join(powerInstance.transform.DOMove(centerPosition + new Vector3(0, 0, 100), 0.15f).SetLoops(2, LoopType.Yoyo));

        yield return powerSequence.WaitForCompletion();

        Sequence fadeSequence = DOTween.Sequence();

        if (sr != null)
        {
            fadeSequence.Append(sr.DOFade(0f, fadeDuration).SetEase(Ease.InQuad));
        }

        smokeObject.SetActive(true);
        Debug.LogError($"Activating power instance: {powerInstance.name}");

        Animator childAnimator = smokeObject.GetComponent<Animator>();
        if (childAnimator != null)
        {
            childAnimator.Play(0);

            AnimatorStateInfo stateInfo = childAnimator.GetCurrentAnimatorStateInfo(0);
            float animationLength = stateInfo.length;

            fadeSequence.AppendInterval(animationLength);
        }
        else
        {
            fadeSequence.AppendInterval(0.5f);
        }

        yield return fadeSequence.WaitForCompletion();

        Debug.LogError($"Destroying power instance: {powerInstance.name}");
        Destroy(powerInstance);
    }

    public void PlayTokenAnimationOnce(int playerNumber)
    {
        int handIndex = GetHandIndex(playerNumber);

        if (handIndex < 0 || handIndex >= animatorHands.Count)
        {
            Debug.LogWarning($"Invalid hand index {handIndex} for player {playerNumber}");
            return;
        }

        if (!animatorHands[handIndex].gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"Hand {handIndex} is inactive, skipping token animation");
            return;
        }

        StartCoroutine(TokenAnimationOnceCoroutine(handIndex));
    }

    private IEnumerator TokenAnimationOnceCoroutine(int handIndex)
    {
        animatorHands[handIndex].SetBool("tokenAction", true);
        animatorHands[handIndex].Play("ElTokenAnimationClip", 0, 0f);

        AnimatorStateInfo stateInfo = animatorHands[handIndex].GetCurrentAnimatorStateInfo(0);
        float animationDuration = stateInfo.length;

        Debug.Log($"Token animation duration: {animationDuration} seconds");

        yield return new WaitForSeconds(animationDuration);

        animatorHands[handIndex].SetBool("tokenAction", false);

        int playerNumber = GetPlayerNumberFromHandIndex(handIndex);
        if (playerNumber == currentActivePlayer)
        {
            animatorHands[handIndex].SetBool("turnLoop", true);
            animatorHands[handIndex].Play("ElPlayingAnimationClip", 0, 0f);
        }
        else
        {
            animatorHands[handIndex].Play("ElDrumRollAnimationClip", 0, 0f);
        }
    }

    private int GetPlayerNumberFromHandIndex(int handIndex)
    {
        int playerCount = DeckController.LocalInstance?.playerCount ?? 4;

        if (playerCount == 2)
        {
            return handIndex == 0 ? 0 : 1;
        }
        else
        {
            return handIndex;
        }
    }

    // Custom_SimpleTwoColorLines Shader Methods
    public void SetFrameBackgroundColor(int frameIndex, Color color1, Color color2)
    {
        if (frameIndex < 0 || frameIndex >= frameObjects.Count)
        {
            Debug.LogWarning($"Invalid frame index {frameIndex}. Available frames: {frameObjects.Count}");
            return;
        }

        if (frameObjects[frameIndex] == null || !frameObjects[frameIndex].activeInHierarchy)
        {
            Debug.LogWarning($"Frame {frameIndex} is null or inactive");
            return;
        }

        Renderer frameRenderer = frameObjects[frameIndex].GetComponent<Renderer>();
        if (frameRenderer == null)
        {
            Debug.LogWarning($"Frame {frameIndex} does not have a Renderer component");
            return;
        }

        // Check if the material uses the Custom_SimpleTwoColorLines shader
        if (frameRenderer.material.shader.name != "Custom/SimpleTwoColorLines")
        {
            Debug.LogWarning($"Frame {frameIndex} material does not use Custom/SimpleTwoColorLines shader. Current shader: {frameRenderer.material.shader.name}");
            return;
        }

        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        frameRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetColor("_Color1", color1);
        propertyBlock.SetColor("_Color2", color2);

        frameRenderer.SetPropertyBlock(propertyBlock);

        Debug.Log($"Set frame {frameIndex} color1 to {color1} and color2 to {color2}");
    }

    public void SetFrameBackgroundToWhite(int playerNumber)
    {
        int frameIndex = GetHandIndex(playerNumber);
        Debug.Log($"SetFrameBackgroundToWhite called for player {playerNumber}, frame index: {frameIndex}");
        
        // Set Color3 to green for turn indication
        SetFrameColor3ToGreen(frameIndex);
        StartCoroutine(FadeFrameColor3Intensity(frameIndex, activePlayerIntensity, fadeDuration));
    }



    public void ResetFrameBackgroundColor(int playerNumber)
    {
        int frameIndex = GetHandIndex(playerNumber);
        Debug.Log($"ResetFrameBackgroundColor called for player {playerNumber}, frame index: {frameIndex}");
        
        // Reset Color3 back to original
        ResetFrameColor3ToOriginal(frameIndex);
        StartCoroutine(FadeFrameColor3IntensityToOriginal(frameIndex, fadeDuration));
    }

    private IEnumerator FadeFrameColor3Intensity(int frameIndex, float targetIntensity, float duration)
    {
        if (frameIndex < 0 || frameIndex >= frameObjects.Count)
        {
            Debug.LogWarning($"Invalid frame index {frameIndex}. Available frames: {frameObjects.Count}");
            yield break;
        }

        if (frameObjects[frameIndex] == null || !frameObjects[frameIndex].activeInHierarchy)
        {
            Debug.LogWarning($"Frame {frameIndex} is null or inactive");
            yield break;
        }

        Renderer frameRenderer = frameObjects[frameIndex].GetComponent<Renderer>();
        if (frameRenderer == null)
        {
            Debug.LogWarning($"Frame {frameIndex} does not have a Renderer component");
            yield break;
        }

        // Check if the material uses the Custom_SimpleTwoColorLines shader
        if (frameRenderer.material.shader.name != "Custom/SimpleTwoColorLines")
        {
            Debug.LogWarning($"Frame {frameIndex} material does not use Custom/SimpleTwoColorLines shader. Current shader: {frameRenderer.material.shader.name}");
            yield break;
        }

        // Get the original Color3Intensity
        float originalIntensity = 0.3f; // Default value
        if (frameRenderer.material.HasProperty("_Color3Intensity"))
        {
            originalIntensity = frameRenderer.material.GetFloat("_Color3Intensity");
        }
        else
        {
            Debug.LogWarning($"Material on {frameObjects[frameIndex].name} does not have _Color3Intensity property. Using default value.");
            // If the material doesn't have the property, we'll use the default value
        }

        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        frameRenderer.GetPropertyBlock(propertyBlock);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            // Animate Color3Intensity for turn indication
            float currentIntensity = Mathf.Lerp(originalIntensity, targetIntensity, t);
            propertyBlock.SetFloat("_Color3Intensity", currentIntensity);

            frameRenderer.SetPropertyBlock(propertyBlock);

            yield return null;
        }

        // Final value
        propertyBlock.SetFloat("_Color3Intensity", targetIntensity);
        frameRenderer.SetPropertyBlock(propertyBlock);

        Debug.Log($"Highlighted frame {frameIndex} with Color3Intensity: {targetIntensity}");
    }

    private IEnumerator FadeFrameColor3IntensityToOriginal(int frameIndex, float duration)
    {
        if (frameIndex < 0 || frameIndex >= frameObjects.Count)
        {
            Debug.LogWarning($"Invalid frame index {frameIndex}. Available frames: {frameObjects.Count}");
            yield break;
        }

        if (frameObjects[frameIndex] == null)
        {
            Debug.LogWarning($"Frame {frameIndex} is null");
            yield break;
        }

        Renderer frameRenderer = frameObjects[frameIndex].GetComponent<Renderer>();
        if (frameRenderer == null)
        {
            Debug.LogWarning($"Frame {frameIndex} does not have a Renderer component");
            yield break;
        }

        // Check if the material uses the Custom_SimpleTwoColorLines shader
        if (frameRenderer.material.shader.name != "Custom/SimpleTwoColorLines")
        {
            Debug.LogWarning($"Frame {frameIndex} material does not use Custom/SimpleTwoColorLines shader. Current shader: {frameRenderer.material.shader.name}");
            yield break;
        }

        // Get the original Color3Intensity
        float originalIntensity = 0.3f; // Default value
        if (frameRenderer.material.HasProperty("_Color3Intensity"))
        {
            originalIntensity = frameRenderer.material.GetFloat("_Color3Intensity");
        }
        else
        {
            Debug.LogWarning($"Material on {frameObjects[frameIndex].name} does not have _Color3Intensity property. Using default value.");
            // If the material doesn't have the property, we'll use the default value
        }
        float currentIntensity = activePlayerIntensity; // Current highlighted intensity

        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        frameRenderer.GetPropertyBlock(propertyBlock);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            // Animate Color3Intensity back to original
            float lerpedIntensity = Mathf.Lerp(currentIntensity, originalIntensity, t);
            propertyBlock.SetFloat("_Color3Intensity", lerpedIntensity);

            frameRenderer.SetPropertyBlock(propertyBlock);

            yield return null;
        }

        // Clear property block to return to original material
        frameRenderer.SetPropertyBlock(null);

        Debug.Log($"Faded frame {frameIndex} back to original Color3Intensity");
    }

    private IEnumerator FadeFrameBackgroundColor(int frameIndex, Color targetColor1, Color targetColor2, float duration)
    {
        if (frameIndex < 0 || frameIndex >= frameObjects.Count)
        {
            Debug.LogWarning($"Invalid frame index {frameIndex}. Available frames: {frameObjects.Count}");
            yield break;
        }

        if (frameObjects[frameIndex] == null || !frameObjects[frameIndex].activeInHierarchy)
        {
            Debug.LogWarning($"Frame {frameIndex} is null or inactive");
            yield break;
        }

        Renderer frameRenderer = frameObjects[frameIndex].GetComponent<Renderer>();
        if (frameRenderer == null)
        {
            Debug.LogWarning($"Frame {frameIndex} does not have a Renderer component");
            yield break;
        }

        // Get the original material colors and Color3Intensity
        Color originalColor1 = frameRenderer.material.GetColor("_Color1");
        Color originalColor2 = frameRenderer.material.GetColor("_Color2");
        float originalColor3Intensity = frameRenderer.material.GetFloat("_Color3Intensity");

        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        frameRenderer.GetPropertyBlock(propertyBlock);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            // Animate colors and Color3Intensity for highlight effect
            Color currentColor1 = Color.Lerp(originalColor1, targetColor1, t);
            Color currentColor2 = Color.Lerp(originalColor2, targetColor2, t);
            float currentColor3Intensity = Mathf.Lerp(originalColor3Intensity, activePlayerIntensity, t);

            propertyBlock.SetColor("_Color1", currentColor1);
            propertyBlock.SetColor("_Color2", currentColor2);
            propertyBlock.SetFloat("_Color3Intensity", currentColor3Intensity);

            frameRenderer.SetPropertyBlock(propertyBlock);

            yield return null;
        }

        // Final values
        propertyBlock.SetColor("_Color1", targetColor1);
        propertyBlock.SetColor("_Color2", targetColor2);
        propertyBlock.SetFloat("_Color3Intensity", activePlayerIntensity);

        frameRenderer.SetPropertyBlock(propertyBlock);

        Debug.Log($"Highlighted frame {frameIndex} with enhanced Color3Intensity, colors: color1: {targetColor1}, color2: {targetColor2}");
    }


    private IEnumerator FadeFrameBackgroundToOriginal(int frameIndex, float duration)
    {
        if (frameIndex < 0 || frameIndex >= frameObjects.Count)
        {
            Debug.LogWarning($"Invalid frame index {frameIndex}. Available frames: {frameObjects.Count}");
            yield break;
        }

        if (frameObjects[frameIndex] == null)
        {
            Debug.LogWarning($"Frame {frameIndex} is null");
            yield break;
        }

        Renderer frameRenderer = frameObjects[frameIndex].GetComponent<Renderer>();
        if (frameRenderer == null)
        {
            Debug.LogWarning($"Frame {frameIndex} does not have a Renderer component");
            yield break;
        }

        // Get the original material colors and Color3Intensity
        Color originalColor1 = frameRenderer.material.GetColor("_Color1");
        Color originalColor2 = frameRenderer.material.GetColor("_Color2");
        float originalColor3Intensity = frameRenderer.material.GetFloat("_Color3Intensity");

        // Current highlighted values
        Color currentColor1 = originalColor1; // Keep same colors
        Color currentColor2 = originalColor2; // Keep same colors
        float currentColor3Intensity = activePlayerIntensity; // Current highlighted intensity

        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        frameRenderer.GetPropertyBlock(propertyBlock);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            // Colors stay the same, only animate Color3Intensity back to original
            Color lerpedColor1 = originalColor1; // No color change
            Color lerpedColor2 = originalColor2; // No color change
            float lerpedColor3Intensity = Mathf.Lerp(currentColor3Intensity, originalColor3Intensity, t);

            propertyBlock.SetColor("_Color1", lerpedColor1);
            propertyBlock.SetColor("_Color2", lerpedColor2);
            propertyBlock.SetFloat("_Color3Intensity", lerpedColor3Intensity);

            frameRenderer.SetPropertyBlock(propertyBlock);

            yield return null;
        }

        // Clear property block to return to original material
        frameRenderer.SetPropertyBlock(null);

        Debug.Log($"Faded frame {frameIndex} back to original Color3Intensity, colors unchanged");
    }


    [ContextMenu("Set Active Player Frame to White")]
    public void SetActivePlayerFrameToWhite()
    {
        if (currentActivePlayer >= 0)
        {
            SetFrameBackgroundToWhite(currentActivePlayer);
        }
        else
        {
            Debug.LogWarning("No active player set");
        }
    }

    // Method to apply the assigned frame material to all frames
    [ContextMenu("Apply Frame Material")]
    public void ApplyFrameMaterial()
    {
        if (frameMaterial == null)
        {
            Debug.LogError("Frame material is not assigned! Please assign a material in the inspector.\n" +
                "UnityEngine.Debug:LogError (object)\n" +
                "ElHolderScript:ApplyFrameMaterial () (at Assets/Scripts/ElHolderScript.cs:" + 
                (new System.Diagnostics.StackTrace(true)).GetFrame(0).GetFileLineNumber() + ")\n" +
                "ElHolderScript:Start () (at Assets/Scripts/ElHolderScript.cs:50)");
            return;
        }

        // Apply the material to all frame objects
        foreach (GameObject frameObject in frameObjects)
        {
            if (frameObject != null)
            {
                Renderer renderer = frameObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = frameMaterial;
                    Debug.Log($"Applied frame material to {frameObject.name}");
                }
                else
                {
                    Debug.LogWarning($"Frame object {frameObject.name} does not have a Renderer component");
                }
            }
        }
    }

    // Method to apply the assigned frame material to a specific frame
    public void ApplyFrameMaterialToFrame(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= frameObjects.Count)
        {
            Debug.LogWarning($"Invalid frame index {frameIndex}. Available frames: {frameObjects.Count}");
            return;
        }

        GameObject frameObject = frameObjects[frameIndex];
        if (frameObject == null)
        {
            Debug.LogWarning($"Frame object at index {frameIndex} is null");
            return;
        }

        if (frameMaterial == null)
        {
            Debug.LogError("Frame material is not assigned! Please assign a material in the inspector.");
            return;
        }

        Renderer renderer = frameObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = frameMaterial;
            Debug.Log($"Applied frame material to frame {frameIndex} ({frameObject.name})");
        }
        else
        {
            Debug.LogWarning($"Frame object {frameObject.name} does not have a Renderer component");
        }
    }

    // Method to set custom colors for a specific frame using MaterialPropertyBlock
    public void SetFrameCustomColors(int frameIndex, Color color1, Color color2, Color color3)
    {
        if (frameIndex < 0 || frameIndex >= frameObjects.Count)
        {
            Debug.LogWarning($"Invalid frame index {frameIndex}. Available frames: {frameObjects.Count}");
            return;
        }

        GameObject frameObject = frameObjects[frameIndex];
        if (frameObject == null)
        {
            Debug.LogWarning($"Frame object at index {frameIndex} is null");
            return;
        }

        Renderer renderer = frameObject.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogWarning($"Frame object {frameObject.name} does not have a Renderer component");
            return;
        }

        // Use MaterialPropertyBlock to modify properties without changing the original material
        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(propertyBlock);

        // Set colors if the shader has these properties
        if (renderer.material.HasProperty("_Color1"))
            propertyBlock.SetColor("_Color1", color1);
        if (renderer.material.HasProperty("_Color2"))
            propertyBlock.SetColor("_Color2", color2);
        if (renderer.material.HasProperty("_Color3"))
            propertyBlock.SetColor("_Color3", color3);

        renderer.SetPropertyBlock(propertyBlock);

        Debug.Log($"Set frame {frameIndex} colors: Color1={color1}, Color2={color2}, Color3={color3}");
    }

    // Method to set custom colors for all frames
    public void SetAllFramesCustomColors(Color color1, Color color2, Color color3)
    {
        for (int i = 0; i < frameObjects.Count; i++)
        {
            SetFrameCustomColors(i, color1, color2, color3);
        }
    }

    // Method to set custom shader properties for a specific frame using MaterialPropertyBlock
    public void SetFrameShaderProperties(int frameIndex, float lineScale = 50f, float animationSpeed = 0.5f, 
        float sharpness = 5f, float waveAmount = 0.5f, float waveFrequency = 10f, float turbulence = 0.3f)
    {
        if (frameIndex < 0 || frameIndex >= frameObjects.Count)
        {
            Debug.LogWarning($"Invalid frame index {frameIndex}. Available frames: {frameObjects.Count}");
            return;
        }

        GameObject frameObject = frameObjects[frameIndex];
        if (frameObject == null)
        {
            Debug.LogWarning($"Frame object at index {frameIndex} is null");
            return;
        }

        Renderer renderer = frameObject.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogWarning($"Frame object {frameObject.name} does not have a Renderer component");
            return;
        }

        // Use MaterialPropertyBlock to modify properties without changing the original material
        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(propertyBlock);

        // Set properties if the shader has them
        if (renderer.material.HasProperty("_LineScale"))
            propertyBlock.SetFloat("_LineScale", lineScale);
        if (renderer.material.HasProperty("_AnimationSpeed"))
            propertyBlock.SetFloat("_AnimationSpeed", animationSpeed);
        if (renderer.material.HasProperty("_Sharpness"))
            propertyBlock.SetFloat("_Sharpness", sharpness);
        if (renderer.material.HasProperty("_WaveAmount"))
            propertyBlock.SetFloat("_WaveAmount", waveAmount);
        if (renderer.material.HasProperty("_WaveFrequency"))
            propertyBlock.SetFloat("_WaveFrequency", waveFrequency);
        if (renderer.material.HasProperty("_Turbulence"))
            propertyBlock.SetFloat("_Turbulence", turbulence);

        renderer.SetPropertyBlock(propertyBlock);

        Debug.Log($"Set frame {frameIndex} shader properties: LineScale={lineScale}, AnimationSpeed={animationSpeed}, Sharpness={sharpness}");
    }

    // Method to set Color3 to green for turn indication
    private void SetFrameColor3ToGreen(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= frameObjects.Count)
        {
            Debug.LogWarning($"Invalid frame index {frameIndex}. Available frames: {frameObjects.Count}");
            return;
        }

        GameObject frameObject = frameObjects[frameIndex];
        if (frameObject == null)
        {
            Debug.LogWarning($"Frame object at index {frameIndex} is null");
            return;
        }

        Renderer renderer = frameObject.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogWarning($"Frame object {frameObject.name} does not have a Renderer component");
            return;
        }

        // Use MaterialPropertyBlock to set Color3 to green
        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(propertyBlock);

        if (renderer.material.HasProperty("_Color3"))
        {
            propertyBlock.SetColor("_Color3", turnIndicationColor);
            renderer.SetPropertyBlock(propertyBlock);
            Debug.Log($"Set frame {frameIndex} Color3 to {turnIndicationColor} for turn indication");
        }
        
        // Set Color3 animation speed if the shader has this property
        if (renderer.material.HasProperty("_Color3AnimationSpeed"))
        {
            MaterialPropertyBlock speedPropertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(speedPropertyBlock);
            speedPropertyBlock.SetFloat("_Color3AnimationSpeed", color3AnimationSpeed);
            renderer.SetPropertyBlock(speedPropertyBlock);
            Debug.Log($"Set frame {frameIndex} Color3 animation speed to {color3AnimationSpeed}");
        }
        else
        {
            Debug.LogWarning($"Frame {frameIndex} material does not have _Color3 property");
        }
    }

    // Method to reset Color3 back to original material value
    private void ResetFrameColor3ToOriginal(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= frameObjects.Count)
        {
            Debug.LogWarning($"Invalid frame index {frameIndex}. Available frames: {frameObjects.Count}");
            return;
        }

        GameObject frameObject = frameObjects[frameIndex];
        if (frameObject == null)
        {
            Debug.LogWarning($"Frame object at index {frameIndex} is null");
            return;
        }

        Renderer renderer = frameObject.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogWarning($"Frame object {frameObject.name} does not have a Renderer component");
            return;
        }

        // Clear the MaterialPropertyBlock to return to original material values
        renderer.SetPropertyBlock(null);
        Debug.Log($"Reset frame {frameIndex} Color3 and Color3 animation speed to original material values");
    }
}