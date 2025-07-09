using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElScript : MonoBehaviour
{
    ElHolderScript elHolderScript;
    // Start is called before the first frame update
    void Start()
    {
        elHolderScript = transform.parent.GetComponent<ElHolderScript>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void TokenActionFalse()
    {
        elHolderScript.TokenActionFalse();
    }

    public void TurnActionFalse()
    {
        elHolderScript.TurnActionFalse();
    }
}
