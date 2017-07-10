using UnityEngine;
using System.Collections;

public class WallScript : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}

    void OnTriggerEnter(Collider other) {
        Debug.Log("Wall has detected a collision");
    }
}
