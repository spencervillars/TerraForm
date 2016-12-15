using UnityEngine;
using System.Collections;

public class FollowPlayer : MonoBehaviour {

    //
    // Taken from the Unity Tutorial website
    // https://unity3d.com/learn/tutorials/projects/roll-ball-tutorial/moving-camera?playlist=17141
    //

    GameObject player;

    void Start()
    {
        player = GameObject.Find("PlayerObject");
    }

    void LateUpdate()
    {
        transform.position = player.transform.position + new Vector3(0,13f,0);
        transform.rotation = player.transform.rotation;
    }
}
