using UnityEngine;
using System;

static class Config {
    public const float PREDATOR_SPREAD = 25.0f;
    public const float PREDATOR_DISTANCE = -25.0f;
    public const int PREDATOR_COUNT = 5;

    public const float PREY_SPREAD = 100.0F;
    public const float PREY_DISTANCE = 50.0f;
    public const int PREY_COUNT = 15;

    public const bool GEN_RANDOM_SEED = true;

    public const float HEIGHT_PLANE = -25.8267f; // The y-axis coordinate

}

public class SimulationController : MonoBehaviour {

    //public PredatorAgent[] predators;
    //public PreyAgent[] prey;
    public GameObject prey;
    public GameObject predator;

	// Use this for initialization
	void Start () {
        initGroup(prey, Config.PREY_COUNT, Config.PREY_SPREAD, Config.PREY_DISTANCE, Config.GEN_RANDOM_SEED);
        initGroup(predator, Config.PREDATOR_COUNT, Config.PREDATOR_SPREAD, Config.PREDATOR_DISTANCE, Config.GEN_RANDOM_SEED);     
    }

    private static GameObject[] initGroup(GameObject theObject, 
                                          int theCount, float theSpread,
                                          float theDistance, bool isRand) {
        GameObject[] objects;
        objects = new GameObject[theCount];
        float xPos;
        float zPos;

        
        for (int i = 0; i < theCount; i++) {
            xPos = UnityEngine.Random.Range(-1 * theSpread / 2, theSpread / 2);
            zPos = UnityEngine.Random.Range(-1 * theSpread / 2, theSpread / 2) + theDistance;
            objects[i] = (GameObject) Instantiate(theObject,
                                                 new Vector3(xPos, Config.HEIGHT_PLANE, zPos),
                                                 Quaternion.identity);
        }

        return objects;
    }
    
	
	// Update is called once per frame
	void Update () {
	
	}
}
