using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VintageRobo_Controller : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Current State")]
    [SerializeField] private AnimationState currentState = AnimationState.Idle;

    [Header("Animation Trigger Names")]
    [SerializeField] private string idleTrigger = "Idle";
    [SerializeField] private string waveTrigger = "Wave";
    [SerializeField] private string callTrigger = "Call";
    [SerializeField] private string danceTrigger = "Dance";

    public enum AnimationState
    {
        Idle,
        Wave,
        Call,
        Dance
    }

    private void Awake()
    {
        // Auto-find Animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null)
        {
            
        }
    }

    private void Start()
    {
        SetState(currentState);
    }

    public void SetState(AnimationState newState)
    {
        if (animator == null)
        {
            
            return;
        }

        currentState = newState;
        string triggerName = GetTriggerForState(newState);
        
        if (!string.IsNullOrEmpty(triggerName))
        {
            
            animator.SetTrigger(triggerName);
        }
    }

    private string GetTriggerForState(AnimationState state)
    {
        switch (state)
        {
            case AnimationState.Idle:
                return idleTrigger;
            case AnimationState.Wave:
                return waveTrigger;
            case AnimationState.Call:
                return callTrigger;
            case AnimationState.Dance:
                return danceTrigger;
            default:
                return idleTrigger;
        }
    }

    // Public methods for state changes
    public void PlayIdle() => SetState(AnimationState.Idle);
    public void PlayWave() => SetState(AnimationState.Wave);
    public void PlayCall() => SetState(AnimationState.Call);
    public void PlayDance() => SetState(AnimationState.Dance);

    // Get current animation state info
    public AnimationState GetCurrentState() => currentState;

    public bool IsPlaying(AnimationState state) => currentState == state;

    // Editor helper methods
    #if UNITY_EDITOR
    [ContextMenu("Play Idle")]
    private void PreviewIdle() => PlayIdle();

    [ContextMenu("Play Wave")]
    private void PreviewWave() => PlayWave();

    [ContextMenu("Play Call")]
    private void PreviewCall() => PlayCall();

    [ContextMenu("Play Dance")]
    private void PreviewDance() => PlayDance();
    #endif
}
