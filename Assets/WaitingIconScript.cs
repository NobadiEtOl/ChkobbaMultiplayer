using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitingIconScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Rotate the icon continuously
        transform.Rotate(0, 0, 100 * Time.deltaTime);
    }
}
