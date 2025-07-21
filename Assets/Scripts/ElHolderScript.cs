using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UIElements;

public class ElHolderScript : MonoBehaviour
{
    public static ElHolderScript LocalInstance; // Make it static and public
    List<Animator> animatorHands = new List<Animator>();
    [SerializeField] public List<GameObject> frameObjects = new List<GameObject>();
    private int currentActivePlayer = -1; // Track currently active player
    [SerializeField] public List<GameObject> powerTransformObjects = new List<GameObject>();
    [SerializeField] public List<GameObject> powerDictionary = new List<GameObject>();
    [SerializeField] private Transform centerTransform; // Center position for showcasing powers
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
            Animator animator = child.GetComponent<Animator>();
            if (animator != null)
            {
                animatorHands.Add(animator);
            }
        }
    }

    // Convert logical player number to hand index based on game mode
    private int GetHandIndex(int playerNumber)
    {
        // Get player count from DeckController to determine game mode
        int playerCount = DeckController.LocalInstance?.playerCount ?? 4;

        if (playerCount == 2) // 1v1 mode
        {
            // In 1v1: player 0 -> hand 0, player 1 -> hand 2 (across from each other)
            return playerNumber == 0 ? 0 : 2;
        }
        else // 2v2 mode (4 players)
        {
            // In 2v2: direct mapping player 0 -> hand 0, player 1 -> hand 1, etc.
            return playerNumber;
        }
    }

    public void UpdateCurrentPlayer(int newPlayerNumber)
    {
        // Reset previous player's frame background color if there was one
        if (currentActivePlayer != -1)
        {
            int currentHandIndex = GetHandIndex(currentActivePlayer);

            // Stop current player's turn animation
            if (currentHandIndex >= 0 && currentHandIndex < animatorHands.Count)
            {
                // Force immediate stop
                animatorHands[currentHandIndex].SetBool("turnLoop", false);
                animatorHands[currentHandIndex].Play("Idle", 0, 0f);
            }

            // Reset current player's frame background color (using player number)
            ResetFrameBackgroundColor(currentActivePlayer);
        }

        // Set new player's frame to white and start their turn animation
        int newHandIndex = GetHandIndex(newPlayerNumber);
        if (newHandIndex >= 0 && newHandIndex < animatorHands.Count)
        {
            // Set new player's frame background to white (using player number)
            SetFrameBackgroundToWhite(newPlayerNumber);

            // Start new player's turn animation immediately
            animatorHands[newHandIndex].SetBool("turnLoop", true);
            // Force immediate transition to turn state
            animatorHands[newHandIndex].Play("ElPlayingAnimationClip", 0, 0f);
        }

        // Update current player
        currentActivePlayer = newPlayerNumber;
    }


    void Update()
    {

    }

    [ContextMenu("TokenActionTrue")]
    public void TokenActionTrue()
    {
        TokenActionTrue(0); // Default to player 0 for context menu
    }

    public void TokenActionTrue(int playerNumber)
    {
        if (playerNumber >= 0 && playerNumber < animatorHands.Count)
        {
            animatorHands[playerNumber].SetBool("tokenAction", true);
            // Force immediate transition to token animation
            animatorHands[playerNumber].Play("ElTokenAnimationClip", 0, 0f); // Replace with actual token state name
        }
    }


    public void OnTokenAnimationEnd(int playerNumber)
    {
        TokenActionFalse(playerNumber);
    }

    [ContextMenu("TokenActionFalse")]
    public void TokenActionFalse()
    {
        TokenActionFalse(0); // Default to player 0 for context menu
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
        TurnActionTrue(0); // Default to player 0 for context menu
    }

    public void TurnActionTrue(int playerNumber)
    {
        if (playerNumber >= 0 && playerNumber < animatorHands.Count)
        {
            animatorHands[playerNumber].SetBool("turnLoop", true);
            // Force immediate transition to turn animation
            animatorHands[playerNumber].Play("ElPlayingAnimationClip", 0, 0f); // Replace with actual turn state name
        }
    }


    public void OnTurnAnimationEnd(int playerNumber)
    {
        TurnActionFalse(playerNumber);
    }

    [ContextMenu("TurnLoopFalse")]
    public void TurnActionFalse()
    {
        TurnActionFalse(0); // Default to player 0 for context menu
    }

    public void TurnActionFalse(int playerNumber)
    {
        if (playerNumber >= 0 && playerNumber < animatorHands.Count)
        {
            animatorHands[playerNumber].SetBool("turnLoop", false);
            // Force immediate transition to idle state
            animatorHands[playerNumber].Play("Idle", 0, 0f); // Replace "Idle" with your actual idle state name
        }
    }


    // Helper method to control all hands at once (if needed)
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
        currentActivePlayer = -1; // Reset current active player
    }

    public void SetHandMode(int playerCount)
    {
        if (playerCount == 2)
        {
            // In 1v1 mode, deactivate hands at index 1 and 3
            if (animatorHands.Count > 1)
                animatorHands[1].gameObject.SetActive(false);
            if (animatorHands.Count > 3)
                animatorHands[3].gameObject.SetActive(false);
        }
        else if (playerCount == 4)
        {
            // In 2v2 mode, make sure all hands are active
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
        powerName = powerName.Replace(" ", ""); // Remove spaces for matching

        // Bounds check for hand index
        if (handIndex < 0 || handIndex >= powerTransformObjects.Count)
        {
            Debug.LogWarning($"Invalid hand index {handIndex} for player {playerNumber}");
            return;
        }

        // Check if the hand is active (important for 1v1 mode)
        if (!powerTransformObjects[handIndex].activeInHierarchy)
        {
            Debug.LogWarning($"Hand {handIndex} is inactive, skipping power showcase");
            return;
        }

        // Find the correct power prefab by name
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

        // Get the starting transform
        Transform startTransform = powerTransformObjects[handIndex].transform;

        // Create the power sprite instance
        GameObject powerInstance = Instantiate(powerPrefab);
        powerInstance.transform.SetParent(centerTransform.parent, true);
        SpriteRenderer sr = powerInstance.GetComponent<SpriteRenderer>();
        GameObject smokeObject = powerInstance.transform.GetChild(0).gameObject;
        smokeObject.SetActive(false); // Hide smoke initially

        // Set initial position and scale
        powerInstance.transform.position = startTransform.position;
        powerInstance.transform.localScale = Vector3.one * 100f; // Start small

        // Get center position
        Vector3 centerPosition = centerTransform.position;
        //centerPosition.z = powerInstance.transform.position.z; // Keep same Z

        // Animation duration
        float moveDuration = 0.1f; // 70% of 1.5f
        float fadeDuration = 0.2f; // 30% of 1.5f

        Vector3 endScale = new Vector3(200, 200, 200); // End bigger

        // Create a sequence for the animations 
        Sequence powerSequence = DOTween.Sequence();

        powerSequence.AppendInterval(0.35f);

        // Move to center and scale up simultaneously
        powerSequence.Append(powerInstance.transform.DOMove(centerPosition, moveDuration));
        powerSequence.Join(powerInstance.transform.DOScale(endScale, moveDuration));

        powerSequence.AppendInterval(0);

        powerSequence.Append(powerInstance.transform.DORotate(powerInstance.transform.rotation.eulerAngles + new Vector3(0, 0, 15), 0.025f).SetLoops(2, LoopType.Yoyo));
        powerSequence.Append(powerInstance.transform.DORotate(powerInstance.transform.rotation.eulerAngles + new Vector3(0, 0, -12), 0.025f).SetLoops(2, LoopType.Yoyo));
        powerSequence.Join(powerInstance.transform.DOMove(centerPosition + new Vector3(0, 0, 170), 0.15f).SetLoops(2, LoopType.Yoyo));
        powerSequence.Append(powerInstance.transform.DORotate(powerInstance.transform.rotation.eulerAngles + new Vector3(0, 0, 13), 0.025f).SetLoops(2, LoopType.Yoyo));
        powerSequence.Append(powerInstance.transform.DORotate(powerInstance.transform.rotation.eulerAngles + new Vector3(0, 0, -16), 0.025f).SetLoops(2, LoopType.Yoyo));
        powerSequence.Join(powerInstance.transform.DOMove(centerPosition + new Vector3(0, 0, 100), 0.15f).SetLoops(2, LoopType.Yoyo));
        //powerSequence.Append(powerInstance.transform.DORotate(powerInstance.transform.rotation.eulerAngles + new Vector3(0, 0, 17), 0.025f).SetLoops(2, LoopType.Yoyo));
        //powerSequence.Append(powerInstance.transform.DORotate(powerInstance.transform.rotation.eulerAngles + new Vector3(0, 0, -17), 0.025f).SetLoops(2, LoopType.Yoyo));
        //powerSequence.Join(powerInstance.transform.DOMove(centerPosition + new Vector3(0, 0, 50), 0.15f).SetLoops(2, LoopType.Yoyo));

        yield return powerSequence.WaitForCompletion();

        Sequence fadeSequence = DOTween.Sequence();

        // Fade out
        if (sr != null)
        {
            fadeSequence.Append(sr.DOFade(0f, fadeDuration).SetEase(Ease.InQuad));
        }

        smokeObject.SetActive(true);
        Debug.LogError($"Activating power instance: {powerInstance.name}");

        // Get the child's animator and play animation if it exists
        Animator childAnimator = smokeObject.GetComponent<Animator>();
        if (childAnimator != null)
        {
            // Play the child's animation and wait for it to complete
            childAnimator.Play(0); // Play first animation state

            // Get animation length to wait for completion
            AnimatorStateInfo stateInfo = childAnimator.GetCurrentAnimatorStateInfo(0);
            float animationLength = stateInfo.length;

            fadeSequence.AppendInterval(animationLength);
        }
        else
        {
            // If no animator, just wait a bit for any other effects
            fadeSequence.AppendInterval(0.5f);
        }

        // Wait for the sequence to complete
        yield return fadeSequence.WaitForCompletion();

        // Destroy the power instance
        Debug.LogError($"Destroying power instance: {powerInstance.name}");
        Destroy(powerInstance);
    }
    
    public void PlayTokenAnimationOnce(int playerNumber)
    {
        int handIndex = GetHandIndex(playerNumber);
        
        // Bounds check for hand index
        if (handIndex < 0 || handIndex >= animatorHands.Count)
        {
            Debug.LogWarning($"Invalid hand index {handIndex} for player {playerNumber}");
            return;
        }
        
        // Check if the hand is active (important for 1v1 mode)
        if (!animatorHands[handIndex].gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"Hand {handIndex} is inactive, skipping token animation");
            return;
        }
        
        // Play token animation once
        StartCoroutine(TokenAnimationOnceCoroutine(handIndex));
    }

    private IEnumerator TokenAnimationOnceCoroutine(int handIndex)
    {
        // Force immediate start of token animation
        animatorHands[handIndex].SetBool("tokenAction", true);
        animatorHands[handIndex].Play("ElTokenAnimationClip", 0, 0f); // Replace with actual token state name

        AnimatorStateInfo stateInfo = animatorHands[handIndex].GetCurrentAnimatorStateInfo(0);
        float animationDuration = stateInfo.length;

        Debug.Log($"Token animation duration: {animationDuration} seconds");

        yield return new WaitForSeconds(animationDuration);

        // Force immediate transition back to turn animation
        animatorHands[handIndex].SetBool("tokenAction", false);

        // Check if this hand belongs to the current active player
        int playerNumber = GetPlayerNumberFromHandIndex(handIndex);
        if (playerNumber == currentActivePlayer)
        {
            animatorHands[handIndex].SetBool("turnLoop", true);
            animatorHands[handIndex].Play("ElPlayingAnimationClip", 0, 0f); // Force immediate turn animation
        }
        else
        {
            animatorHands[handIndex].Play("ElDrumRollAnimationClip", 0, 0f); // Force to idle
        }
    }

    private int GetPlayerNumberFromHandIndex(int handIndex)
    {
        int playerCount = DeckController.LocalInstance?.playerCount ?? 4;

        if (playerCount == 2) // 1v1 mode
        {
            // In 1v1: hand 0 -> player 0, hand 2 -> player 1
            return handIndex == 0 ? 0 : 1;
        }
        else // 2v2 mode (4 players)
        {
            // In 2v2: direct mapping hand index -> player number
            return handIndex;
        }
    }

    public void SetFrameBackgroundColor(int frameIndex, Color backgroundColor)
    {
        // Use the hand index directly (since frameIndex should already be mapped through GetHandIndex)
        // Bounds check for frame index
        if (frameIndex < 0 || frameIndex >= frameObjects.Count)
        {
            Debug.LogWarning($"Invalid frame index {frameIndex}. Available frames: {frameObjects.Count}");
            return;
        }

        // Check if the frame object exists and is active
        if (frameObjects[frameIndex] == null || !frameObjects[frameIndex].activeInHierarchy)
        {
            Debug.LogWarning($"Frame {frameIndex} is null or inactive");
            return;
        }

        // Get the renderer component
        Renderer frameRenderer = frameObjects[frameIndex].GetComponent<Renderer>();
        if (frameRenderer == null)
        {
            Debug.LogWarning($"Frame {frameIndex} does not have a Renderer component");
            return;
        }

        // Create a new MaterialPropertyBlock to modify properties without affecting other objects
        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        // Get existing property block (in case there are other custom properties)
        frameRenderer.GetPropertyBlock(propertyBlock);

        // Set the background color (assuming your shader uses "_MainColor" property)
        propertyBlock.SetColor("_MainColor", backgroundColor);

        // Apply the property block to the renderer
        frameRenderer.SetPropertyBlock(propertyBlock);

        Debug.Log($"Set frame {frameIndex} background color to {backgroundColor}");
    }


    // Convenience function to set background color to white
    // Modified function to work with player numbers instead of direct frame indices
    public void SetFrameBackgroundToWhite(int playerNumber)
    {
        int frameIndex = GetHandIndex(playerNumber);
        SetFrameBackgroundColor(frameIndex, Color.white);
    }


    // Function to reset frame background color to original
    // Modified function to work with player numbers instead of direct frame indices  
    public void ResetFrameBackgroundColor(int playerNumber)
    {
        int frameIndex = GetHandIndex(playerNumber);

        // Bounds check for frame index
        if (frameIndex < 0 || frameIndex >= frameObjects.Count)
        {
            Debug.LogWarning($"Invalid frame index {frameIndex}. Available frames: {frameObjects.Count}");
            return;
        }

        // Check if the frame object exists and is active
        if (frameObjects[frameIndex] == null)
        {
            Debug.LogWarning($"Frame {frameIndex} is null");
            return;
        }

        // Get the renderer component
        Renderer frameRenderer = frameObjects[frameIndex].GetComponent<Renderer>();
        if (frameRenderer == null)
        {
            Debug.LogWarning($"Frame {frameIndex} does not have a Renderer component");
            return;
        }

        // Clear the property block to use original material properties
        frameRenderer.SetPropertyBlock(null);

        Debug.Log($"Reset frame {frameIndex} to original background color");
    }


    // Function to set background color for current active player's frame
    [ContextMenu("Set Active Player Frame to White")]
    public void SetActivePlayerFrameToWhite()
    {
        if (currentActivePlayer >= 0)
        {
            int handIndex = GetHandIndex(currentActivePlayer);
            SetFrameBackgroundToWhite(handIndex);
        }
        else
        {
            Debug.LogWarning("No active player set");
        }
    }

}