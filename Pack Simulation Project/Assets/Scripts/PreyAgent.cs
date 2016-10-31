using UnityEngine;
using System.Collections;


// Script utilized to perform AI behavior on our Prey GameObjects
public class PreyAgent : MonoBehaviour {


    private NavMeshAgent agent;
    private Animator animator;
    private int curDestination;

    private float maxWalkSpeed;
    private float maxRunSpeed;

    private Vector3 previousPosition;
    private float curSpeed;
    private bool selected;

    private double endurance;
    private int health;
    private string state;
    private float visionRadius;

    // Use this for initialization
    void Start() {
        agent = GetComponent<NavMeshAgent>();
        agent.autoBraking = false;
        endurance = 1.0;
        health = 100;
        animator = GetComponent<Animator>();
        previousPosition = transform.position;
        curDestination = 0;
        maxWalkSpeed = 5;
        maxRunSpeed = 15;
        visionRadius = 50;
        state = "relaxed";
	}
	
	// Update is called once per frame
	void Update() {
        Vector3 curMove = transform.position - previousPosition;
        curSpeed = curMove.magnitude / Time.deltaTime;
        previousPosition = transform.position;

        updatePrey();

        if (curSpeed > 0 && curSpeed <= maxWalkSpeed) {
            animator.SetInteger("movement_state", 1);
        } else if (curSpeed > maxWalkSpeed) {
            animator.SetInteger("movement_state", 2);
        } else {
            animator.SetInteger("movement_state", 0);
        }

    }

    // Triggers when a mouse click collides with the BoxCollider on the Horse
    void OnMouseDown() {
        Debug.Log("I clicked on a Prey!");
        GameObject getCameraObject = GameObject.FindGameObjectWithTag("MainCamera");
        CameraController camera = getCameraObject.GetComponent<CameraController>();
        camera.changeObjectFocus(this.transform);
        selected = true;
    }

    // Used to draw non-interactive UI elements to the screen 
    void OnGUI() {
        if (selected) {
            GUI.color = Color.red;
            GUI.Label(new Rect(10, 10, 500, 20), "Agent Name: " + this.transform.name);
            GUI.Label(new Rect(10, 20, 500, 20), "Speed: " + curSpeed);
            GUI.Label(new Rect(10, 30, 500, 20), "Endurance: " + endurance);
            GUI.Label(new Rect(10, 40, 500, 20), "Health: " + health);
            GUI.Label(new Rect(10, 50, 500, 20), "State: " + state);
        }
    }

    // Allows the camera to let the HorseAgent know that it has been deselected
    public void deselect() {
        selected = false;
    }

    private void updatePrey() {
        // create a detection radius and find all predators within it
        Collider[] hitColliders = Physics.OverlapSphere(this.transform.position, visionRadius);
        Vector3 normalizedCenter = new Vector3(0, 0, 0);
        int numOfPred = 0;
        for (int i = 0; i < hitColliders.Length; i++) {
            GameObject curObject = hitColliders[i].gameObject;
            if (curObject.tag == "PredatorAgent") {
                PredatorAgent getScript = curObject.GetComponent<PredatorAgent>();

                // if the thing we collided with is a predator
                //Debug.Log("A Prey's vision has collided with a Predator.");
                // run away!
                numOfPred++;
                normalizedCenter += curObject.transform.position;
            }

            if (curObject.tag == "Wall") {
                normalizedCenter += curObject.transform.position;
                numOfPred++;
            }
        }

        // we've detected a predator! run!
        if (numOfPred > 0) {
            state = "fleeing";
            normalizedCenter = (normalizedCenter / numOfPred);
            Vector3 moveAway = Vector3.MoveTowards(this.transform.position, normalizedCenter, -1 * Time.deltaTime * maxRunSpeed);
            NavMeshHit hit;
            NavMesh.SamplePosition(moveAway, out hit, 5, NavMesh.AllAreas);

            agent.SetDestination(hit.position);
            agent.speed = maxRunSpeed;
        } else {
        // we haven't seen a predator, so make sure our speed isn't too fast
        // then find a random point within a fixed area that is on the navmesh to move to
            agent.speed = maxWalkSpeed;
            state = "relaxed";
        }

        // waypoint debug code, now being used to see the vision radius
        GameObject waypoint = GameObject.Find("CURRENT_WAYPOINT_DEBUG_PREY");
        waypoint.transform.position = this.transform.position;
        WayPointDebugScript wpScript = waypoint.GetComponent<WayPointDebugScript>();
        wpScript.updateRadius(visionRadius);


        updateEndurance();
    }

    private void updateEndurance() {

        if (curSpeed >= maxWalkSpeed) {
            endurance -= 0.01 * Time.deltaTime;
        }

        if (curSpeed <= maxWalkSpeed) {
            endurance += 0.01 * Time.deltaTime;
        }

        if (endurance < 0) {
            endurance = 0;
        }

        agent.speed = (float) (agent.speed * endurance);
    }

}
