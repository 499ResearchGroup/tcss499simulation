using UnityEngine;
using System.Collections;


// Script utilized to perform AI behavior on our Horse GameObjects
public class HorseAgent : MonoBehaviour {


    private NavMeshAgent agent;
    private Animator animator;
    private int curDestination;

    private float maxWalkSpeed;
    private float maxRunSpeed;

    private Vector3 previousPosition;
    private float curSpeed;
    private bool selected;

    // 0 for prey
    // 1 for predator 
    public int animal_type; 

    // Use this for initialization
    void Start () {
        agent = GetComponent<NavMeshAgent>();
        agent.autoBraking = false;
        animator = GetComponent<Animator>();
        previousPosition = transform.position;
        curDestination = 0;
        maxWalkSpeed = 5;
        maxRunSpeed = 15;
	}
	
	// Update is called once per frame
	void Update () {
        Vector3 curMove = transform.position - previousPosition;
        curSpeed = curMove.magnitude / Time.deltaTime;
        previousPosition = transform.position;

        // if we're prey
        if (animal_type == 0) {
            updatePrey();
        } else {
        // if we're a predator
            updatePredator();
        }

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
        Debug.Log("I clicked on the Horse!");
        GameObject getCameraObject = GameObject.FindGameObjectWithTag("MainCamera");
        CameraController camera = getCameraObject.GetComponent<CameraController>();
        selected = true;
        camera.changeObjectFocus(this.transform);
    }

    // Used to draw non-interactive UI elements to the screen 
    void OnGUI() {
        if (selected) {
            GUI.color = Color.red;
            GUI.Label(new Rect(10, 10, 500, 20), "Agent Information Goes Here");
        }
    }

    // Allows the camera to let the HorseAgent know that it has been deselected
    public void deselect() {
        selected = false;
    }

    private void updatePredator() {
        // create a detection radius and find all prey within it
        Collider[] hitColliders = Physics.OverlapSphere(this.transform.position, 100);
        for (int i = 0; i < hitColliders.Length; i++) {
            GameObject curObject = hitColliders[i].gameObject;
            if (curObject.tag == "HorseAgent") {
                HorseAgent getScript = curObject.GetComponent<HorseAgent>();

                // if the thing we collided with is a prey
                if (getScript.animal_type == 0) {
                    //Debug.Log("A Predator's vision has collided with a Prey.");
                    // follow it!
                    agent.SetDestination(curObject.transform.position);
                }
            }
        }
    }

    private void updatePrey() {
        // create a detection radius and find all predators within it
        Collider[] hitColliders = Physics.OverlapSphere(this.transform.position, 100);
        Vector3 normalizedCenter = new Vector3(0, 0, 0);
        int numOfPred = 0;
        for (int i = 0; i < hitColliders.Length; i++) {
            GameObject curObject = hitColliders[i].gameObject;
            if (curObject.tag == "HorseAgent") {
                HorseAgent getScript = curObject.GetComponent<HorseAgent>();

                // if the thing we collided with is a predator
                if (getScript.animal_type == 1) {
                    //Debug.Log("A Prey's vision has collided with a Predator.");
                    // run away!
                    //transform.rotation = Quaternion.LookRotation(transform.position - curObject.transform.position);

                    //Vector3 runTo = transform.position + transform.forward;
                    numOfPred++;
                    normalizedCenter += curObject.transform.position;
                }
            }
        }

        normalizedCenter = (normalizedCenter / numOfPred);
        Vector3 moveAway = Vector3.MoveTowards(this.transform.position, normalizedCenter, -1 * Time.deltaTime * maxRunSpeed);
        NavMeshHit hit;
        NavMesh.SamplePosition(moveAway, out hit, 5, NavMesh.AllAreas);

        GameObject waypoint = GameObject.Find("CURRENT_WAYPOINT_DEBUG_PREY");
        waypoint.transform.position = moveAway;
        agent.SetDestination(hit.position);
    }

}
