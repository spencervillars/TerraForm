using UnityEngine;
using System.Collections;

public class ObjectManager {

    private static ObjectManager singleton;
    private static Object singletonLock = new Object();

    public GameObject playerObject;
    public Camera mainCamera;

    private ObjectManager()
    {
        playerObject = GameObject.Find("PlayerObject");
        mainCamera = GameObject.FindObjectOfType<Camera>();
    }

    public static ObjectManager GetObjectManager()
    {
        lock(singletonLock)
        {
            if (singleton == null)
            {
                singleton = new ObjectManager();
            }
        }

        return singleton;
    }

    public Vector3 GetPlayerPosition()
    {
        return playerObject.transform.position;
    }

}
