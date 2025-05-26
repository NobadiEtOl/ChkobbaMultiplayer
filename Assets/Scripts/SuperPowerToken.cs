using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuperPowerToken : MonoBehaviour
{
    public SuperPower power; // Assign this in the Inspector

    [ContextMenu("Activate Power")]
    public void OnTokenClicked()
    {
        power.ActivatePower();
    }
}
