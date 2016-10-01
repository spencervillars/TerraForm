using UnityEngine;
using System.Collections;

public class script : MonoBehaviour {

	// Use this for initialization
	void Start () {
        NoiseGenerator.Initialize();
	}
	
	// Update is called once per frame
	void Update () {
        CellManager.GetCellManager().Tick();
	}
}
