using UnityEngine;
using System.Collections;

public class WayPointDebugScript : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void updateRadius(float radii) {
        // multiply the radii by 2 since the default radius is 0.5 to achieve the desired radius
        float calcScale = 2f * radii;
        this.gameObject.transform.localScale = new Vector3(calcScale, calcScale, calcScale);

    }
}
