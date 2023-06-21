using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallTracker : MonoBehaviour
{
    [SerializeField] private Transform ballTransform;
    string currentField;
    void Start()
    {
        transform.position = new Vector3(ballTransform.position.x,-0.5f,ballTransform.position.z);
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(ballTransform.position.x,-0.5f,ballTransform.position.z);
    }

    private void OnTriggerEnter(Collider other){
        currentField = other.gameObject.tag;
    }
}