using UnityEngine;
using System.Collections;


// Justin was here!

// Script utilized to perform AI behavior on our Prey GameObjects
public class PreyAgent : MonoBehaviour {


    private NavMeshAgent agent;
    private Animator animator;

    private float maxWalkSpeed;
    private float maxRunSpeed;

    private Vector3 previousPosition;
    private float curSpeed;
    private bool selected;

    private float endurance;
    public float health;
    private string state;
    private float visionRadius;
    private float personalSpaceRadius;
	private float enduranceScalar;

    // Use this for initialization
    void Start() {
        agent = GetComponent<NavMeshAgent>();
        agent.autoBraking = false;
        endurance = 1.0f;
        health = 100;
        animator = GetComponent<Animator>();
        previousPosition = transform.position;
        maxWalkSpeed = Config.PREY_WALK_SPEED;
        //maxRunSpeed = Config.PREY_RUN_SPEED;
		maxRunSpeed = Random.Range(10.0f, 15.0f);
        visionRadius = Config.PREY_VISION_RADIUS;
		enduranceScalar = 0.999f;
		personalSpaceRadius = agent.radius * 2;
        state = "relaxed";
	}

    // Update is called once per frame
    void Update() {
        Vector3 curMove = transform.position - previousPosition;
        curSpeed = curMove.magnitude / Time.deltaTime;
        previousPosition = transform.position;

        if (health > 0 && animator.enabled) {
			updatePrey();
            if (curSpeed > 0 && curSpeed <= (maxWalkSpeed + 1)) {
                animator.SetInteger("movement_state", 1);
            } else if (curSpeed > maxWalkSpeed + 1) {
                animator.SetInteger("movement_state", 2);
            } else {
                animator.SetInteger("movement_state", 0);
            }
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

    // Used for debugging, draws a sphere in the scene view
    void OnDrawGizmos() {
        Gizmos.DrawWireSphere(this.transform.position, visionRadius);
    }

    // Allows the camera to let the HorseAgent know that it has been deselected
    public void deselect() {
        selected = false;
    }

    // Allows other agents to sap the endurance of the prey, simulating a bite
    public void bitePrey() {
		endurance *= enduranceScalar;
		enduranceScalar = Mathf.Pow (enduranceScalar, 3);
        health -= (10);
    }

    // Getter function to receive the current agent's velocity in the simulation
    public Vector3 getVelocity() {
        return agent.velocity;
    }

    private void updatePrey() {
        if (health < 25) {
            exhibitDisabledState();
        } else {
            calculateForces();
            updateEndurance();
        }
    }

    // 
    private void calculateForces() {
        // create a detection radius and find all relevant agents within it
        Collider[] hitColliders = Physics.OverlapSphere(this.transform.position, visionRadius);

        // force vectors 
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        Vector3 seperation = Vector3.zero;
        Vector3 repulsion = Vector3.zero;

        // counter used to track number of prey within radius
        int preyDetected = 1;
        int predatorsDetected = 0;

        // loop through all of the things we've collided with
        for (int i = 0; i < hitColliders.Length; i++) {
            GameObject curObject = hitColliders[i].gameObject;

            if (curObject.tag == "PredatorAgent") {
                // do something with vectors
				float dist = Vector3.Distance(this.transform.position, curObject.transform.position);
                predatorsDetected++;
				repulsion += (curObject.transform.position - this.transform.position) / (dist * dist * dist);
            }

            if (curObject.tag == "PreyAgent" && !curObject.Equals(this)) {
                preyDetected++;
                alignment += curObject.GetComponent<PreyAgent>().getVelocity();
                cohesion += curObject.transform.position;
                if (Vector3.Distance(this.transform.position, curObject.transform.position) <= personalSpaceRadius) {
                    seperation -= (curObject.transform.position - this.transform.position);
                }
            }
        }

        cohesion /= preyDetected;
        alignment /= preyDetected;

        if (predatorsDetected > 0) {
            repulsion /= predatorsDetected;
            repulsion *= -5000;
            agent.velocity = Vector3.ClampMagnitude(agent.velocity + alignment + cohesion + seperation + repulsion, maxRunSpeed * endurance);
        } else {
            agent.velocity = Vector3.ClampMagnitude(agent.velocity + alignment + cohesion + seperation, maxWalkSpeed * endurance);
        }
    }

    private void updateEndurance() {

        if (curSpeed >= maxWalkSpeed) {
            endurance -= 0.01f * Time.deltaTime;
        }

        if (curSpeed <= maxWalkSpeed) {
            endurance += 0.01f * Time.deltaTime;
        }

        if (endurance < 0) {
            endurance = 0;
        }

        if (endurance > 1) {
            endurance = 1;
        }

        agent.speed = (float) (agent.speed * endurance);
    }

    private void exhibitDisabledState() {
        agent.speed = 0;
		agent.velocity = Vector3.zero;
        agent.ResetPath();
		agent.enabled = false;
        transform.Rotate(new Vector3(transform.rotation.x, transform.rotation.y, 90));
        BoxCollider collide = this.GetComponent<BoxCollider>();
        animator.enabled = false;
        collide.enabled = false;
    }
}
