using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElHolderScript : MonoBehaviour
{
    // Start is called before the first frame update
    Animator animatorRight;
    Animator animatorLeft;
    void Start()
    {
        List<GameObject> handObjects = new List<GameObject>();

        foreach (Transform child in gameObject.transform)
        {
            handObjects.Add(child.gameObject);
        }

        animatorRight = handObjects[0].GetComponent<Animator>();
        animatorLeft = handObjects[1].GetComponent<Animator>();

        if (animatorRight == null || animatorLeft == null)
        {
            Debug.LogError("Animator components not found on hand objects.");
        }
        else
        {
            Debug.Log("Animators initialized successfully.");
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    [ContextMenu("TokenActionTrue")]
    public void TokenActionTrue()
    {
        animatorRight.SetBool("tokenAction", true);
    }

    public void OnTokenAnimationEnd()
    {
        TokenActionFalse();
    }


    [ContextMenu("TokenActionFalse")]
    public void TokenActionFalse()
    {
        animatorRight.SetBool("tokenAction", false);
        AnimatorStateInfo leftState = animatorLeft.GetCurrentAnimatorStateInfo(0);
        string leftStateName = leftState.IsName("ElPlayingAnimationClip") ? "ElPlayingAnimationClip" : "ElDrumRollAnimationClip"; // or use leftState.shortNameHash
        float leftNormalizedTime = leftState.normalizedTime % 1f; // value between 0 and 1
        animatorRight.Play(leftStateName, 0, leftNormalizedTime);
    }

    [ContextMenu("TurnLoopTrue")]
    public void TurnActionTrue()
    {
        animatorRight.SetBool("turnLoop", true);
        animatorLeft.SetBool("turnLoop", true);
    }

    public void OnTurnAnimationEnd()
    {
        TurnActionFalse();
    }


    [ContextMenu("TurnLoopFalse")]
    public void TurnActionFalse()
    {
        animatorRight.SetBool("turnLoop", false);
        animatorLeft.SetBool("turnLoop", false);
    }
}
