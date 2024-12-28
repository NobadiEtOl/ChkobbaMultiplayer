using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrintWebGLVersion : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        print(SystemInfo.graphicsDeviceType);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
