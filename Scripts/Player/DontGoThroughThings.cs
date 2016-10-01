using UnityEngine;
using System.Collections;

public class DontGoThroughThings : MonoBehaviour
{
    private Rigidbody myRigidbody;

    //initialize values 
    void Start()
    {
        myRigidbody = GetComponent<Rigidbody>();
    }

    void LateUpdate()
    {
        RaycastHit hitInfo;
        Physics.Raycast(gameObject.transform.position + Vector3.up*1000, Vector3.down, out hitInfo, 5000, -1);

        Vector3 position = gameObject.transform.position;
        position.y = hitInfo.point.y;

        gameObject.transform.position = position;

    }
}