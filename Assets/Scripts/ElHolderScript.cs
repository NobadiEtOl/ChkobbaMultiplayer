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

    // Only keep these two variables for Ebru shader
    [SerializeField] private float bandWidth = 0.18f;
    [SerializeField] private float fadeDuration = 0.3f;

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

    // Ebru Shader Methods
    public void SetFrameBackgroundColor(int frameIndex, Color mainBandColor, Color secondaryBandColor)
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

        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        frameRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetColor("_MainBandColor", mainBandColor);
        propertyBlock.SetColor("_SecondaryBandColor", secondaryBandColor);
        propertyBlock.SetFloat("_BandWidth", bandWidth);

        frameRenderer.SetPropertyBlock(propertyBlock);

        Debug.Log($"Set frame {frameIndex} main band color to {mainBandColor} and secondary band color to {secondaryBandColor}");
    }

    public void SetFrameBackgroundToWhite(int playerNumber)
    {
        int frameIndex = GetHandIndex(playerNumber);
        StartCoroutine(FadeFrameBandWidth(frameIndex, bandWidth * 2f, fadeDuration));
    }



    public void ResetFrameBackgroundColor(int playerNumber)
    {
        int frameIndex = GetHandIndex(playerNumber);
        StartCoroutine(FadeFrameBandWidthToOriginal(frameIndex, fadeDuration));
    }

    private IEnumerator FadeFrameBandWidth(int frameIndex, float targetBandWidth, float duration)
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

        // Get the original band width only
        float originalBandWidth = frameRenderer.material.GetFloat("_BandWidth");

        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        frameRenderer.GetPropertyBlock(propertyBlock);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            // Only animate band width, no color changes
            float currentBandWidth = Mathf.Lerp(originalBandWidth, targetBandWidth, t);
            propertyBlock.SetFloat("_BandWidth", currentBandWidth);

            frameRenderer.SetPropertyBlock(propertyBlock);

            yield return null;
        }

        // Final value
        propertyBlock.SetFloat("_BandWidth", targetBandWidth);
        frameRenderer.SetPropertyBlock(propertyBlock);

        Debug.Log($"Highlighted frame {frameIndex} with band width: {targetBandWidth}");
    }

    private IEnumerator FadeFrameBandWidthToOriginal(int frameIndex, float duration)
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

        // Get the original band width
        float originalBandWidth = frameRenderer.material.GetFloat("_BandWidth");
        float currentBandWidth = bandWidth * 2f; // Current highlighted band width

        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        frameRenderer.GetPropertyBlock(propertyBlock);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            // Only animate band width back to original
            float lerpedBandWidth = Mathf.Lerp(currentBandWidth, originalBandWidth, t);
            propertyBlock.SetFloat("_BandWidth", lerpedBandWidth);

            frameRenderer.SetPropertyBlock(propertyBlock);

            yield return null;
        }

        // Clear property block to return to original material
        frameRenderer.SetPropertyBlock(null);

        Debug.Log($"Faded frame {frameIndex} back to original band width");
    }

    private IEnumerator FadeFrameBackgroundColor(int frameIndex, Color targetMainColor, Color targetSecondaryColor, float duration)
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

        // Get the original material colors and band width
        Color originalMainColor = frameRenderer.material.GetColor("_MainBandColor");
        Color originalSecondaryColor = frameRenderer.material.GetColor("_SecondaryBandColor");
        float originalBandWidth = frameRenderer.material.GetFloat("_BandWidth");

        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        frameRenderer.GetPropertyBlock(propertyBlock);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            // Keep colors the same, only animate band width
            Color currentMainColor = Color.Lerp(originalMainColor, targetMainColor, t);
            Color currentSecondaryColor = Color.Lerp(originalSecondaryColor, targetSecondaryColor, t);

            // Animate band width to highlight effect (increase band width for highlight)
            float currentBandWidth = Mathf.Lerp(originalBandWidth, bandWidth * 2f, t); // Double the band width for highlight

            propertyBlock.SetColor("_MainBandColor", currentMainColor);
            propertyBlock.SetColor("_SecondaryBandColor", currentSecondaryColor);
            propertyBlock.SetFloat("_BandWidth", currentBandWidth);

            frameRenderer.SetPropertyBlock(propertyBlock);

            yield return null;
        }

        // Final values
        propertyBlock.SetColor("_MainBandColor", targetMainColor);
        propertyBlock.SetColor("_SecondaryBandColor", targetSecondaryColor);
        propertyBlock.SetFloat("_BandWidth", bandWidth * 2f); // Final highlight band width

        frameRenderer.SetPropertyBlock(propertyBlock);

        Debug.Log($"Highlighted frame {frameIndex} with enhanced band width, keeping colors: main: {targetMainColor}, secondary: {targetSecondaryColor}");
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

        // Get the original material colors and band width
        Color originalMainColor = frameRenderer.material.GetColor("_MainBandColor");
        Color originalSecondaryColor = frameRenderer.material.GetColor("_SecondaryBandColor");
        float originalBandWidth = frameRenderer.material.GetFloat("_BandWidth");

        // Current highlighted values
        Color currentMainColor = originalMainColor; // Keep same colors
        Color currentSecondaryColor = originalSecondaryColor; // Keep same colors
        float currentBandWidth = bandWidth * 2f; // Current highlighted band width

        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        frameRenderer.GetPropertyBlock(propertyBlock);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            // Colors stay the same, only animate band width back to original
            Color lerpedMainColor = originalMainColor; // No color change
            Color lerpedSecondaryColor = originalSecondaryColor; // No color change
            float lerpedBandWidth = Mathf.Lerp(currentBandWidth, originalBandWidth, t);

            propertyBlock.SetColor("_MainBandColor", lerpedMainColor);
            propertyBlock.SetColor("_SecondaryBandColor", lerpedSecondaryColor);
            propertyBlock.SetFloat("_BandWidth", lerpedBandWidth);

            frameRenderer.SetPropertyBlock(propertyBlock);

            yield return null;
        }

        // Clear property block to return to original material
        frameRenderer.SetPropertyBlock(null);

        Debug.Log($"Faded frame {frameIndex} back to original band width, colors unchanged");
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
}