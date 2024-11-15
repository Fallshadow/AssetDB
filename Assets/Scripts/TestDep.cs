using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestDep : MonoBehaviour
{
    public GameObject test;
    // Start is called before the first frame update
    void Start()
    {
        GameObject.Instantiate(test);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
