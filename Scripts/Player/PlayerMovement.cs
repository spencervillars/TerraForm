using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour {

    GameObject mainCamera;
    Rigidbody rb;

	// Use this for initialization
	void Start () {
        mainCamera = GameObject.Find("Main Camera");
        //rb = gameObject.GetComponent<Rigidbody>();
    }

    void Update()
    {
        float timeFactor = Time.deltaTime;
        timeFactor = Mathf.Min(timeFactor, 1f / 30f);

        var x = Input.GetAxis("Horizontal") * timeFactor * 200.0f;
        var z = Input.GetAxis("Vertical") * timeFactor * 100;
        transform.Translate(x,0,z);
    }


}
