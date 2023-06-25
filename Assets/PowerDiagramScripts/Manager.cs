using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{

    MantleManager mantleManager;


    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;

        mantleManager = FindObjectOfType<MantleManager>();
    }

    

    private void Update() {
        mantleManager.DoUpdate();
    }
}
