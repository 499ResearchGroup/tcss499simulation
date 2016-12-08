using UnityEngine;
using System.Collections;


// Justin was here!

// Script utilized to perform AI behavior on our Prey GameObjects
public class PreyAgent : MonoBehaviour {


    private NavMeshAgent agent;
    private Animator animator;

    public float maxWalkSpeed;
    public float maxRunSpeed;

    private Vector3 previousPosition;
    private float curSpeed;
    private bool selected;

    public float endurance;
    public float health;
    private string preyMode;
    private bool isFleeing;
    private float visionRadius;
    private float personalSpaceRadius;
	private float enduranceScalar;

    private int postFleeTicks;

    public void Initialize()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.autoBraking = false;
        endurance = 1.0f;
        health = 100;
        animator = GetComponent<Animator>();
        previousPosition = transform.position;
        if (Config.PREY_USE_RANDOM_SPEEDS)
        {
            maxWalkSpeed = Config.PREY_WALK_SPEED;
            maxRunSpeed = Random.Range(Config.PREY_MIN_RAND_SPEED, Config.PREY_MAX_RAND_SPEED);
        }
        else {
            maxWalkSpeed = Config.PREY_WALK_SPEED;
            maxRunSpeed = Config.PREY_RUN_SPEED;
        }
        visionRadius = Config.PREY_VISION_RADIUS;
        enduranceScalar = 0.999f;
        personalSpaceRadius = agent.radius * 2;
        isFleeing = false;
        preyMode = "relaxed";
        postFleeTicks = 0;
    }

    // Use this for initialization
    void Start() {
        
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
        //Debug.Log("I clicked on a Prey!");
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
            GUI.Label(new Rect(10, 50, 500, 20), "State: " + preyMode);
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

    // Allows Predators to determine whether this Prey is fleeing or not. 
    // Returns the Prey's isFleeing value.
    public bool getFleeing() {
        return isFleeing;
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

    // Used to update the Prey per time step. 
    private void updatePrey() {
        if (health <= 0) {
            exhibitDisabledState();
        } else {
            calculateForces();
            updateEndurance();
        }
    }

    // Primary method for calculating new velocity for Prey. Utilizes a modified boids model with additional repulsion vector
    // to push Prey away from Predators dynamically.
    //
    // Sources used/credit to:
    // 	Craig Reynolds: http://www.red3d.com/cwr/boids/
    // 		Utilized Craig Reynolds' article on his development of the Boids model when initially learning about Boids for the first time. 
    // 	Conrad Parker: http://www.kfish.org/boids/pseudocode.html
    // 		Utilized Conrad Parker's article on Boids which provides psuedo-code and suggestions for goal setting when learning about Boids for
    //      the first time. 
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

        // get prey list and calculate boids to simulate infinite vision radius with other prey
        GameObject[] preyList = GameObject.Find("SimulationManager").GetComponent<SimulationController>().getPreyList();
        for (int i = 0; i < preyList.Length; i++) {
            if (preyList[i].GetComponent<PreyAgent>() != this) {
                preyDetected++;
                alignment += preyList[i].GetComponent<PreyAgent>().getVelocity();
                cohesion += preyList[i].transform.position;
                if (Vector3.Distance(this.transform.position, preyList[i].transform.position) <= personalSpaceRadius) {
                    seperation -= (preyList[i].transform.position - this.transform.position);
                }
            }
        }

        // loop through all of the things we've collided with
        for (int i = 0; i < hitColliders.Length; i++) {
            GameObject curObject = hitColliders[i].gameObject;

            if (curObject.tag == "PredatorAgent") {
                // do something with vectors
				float dist = Vector3.Distance(this.transform.position, curObject.transform.position);
                predatorsDetected++;
				repulsion += (curObject.transform.position - this.transform.position) / (dist * dist * dist);
            }
        }

        cohesion /= preyDetected;
        alignment /= preyDetected;

        if (predatorsDetected > 0) {
            repulsion /= predatorsDetected;
            repulsion *= -5000;
            isFleeing = true;
            preyMode = "fleeing";

            postFleeTicks = 0;

            float enduranceFactor = endurance * 1.33f;
            if (enduranceFactor > 1.0f) {
                enduranceFactor = 1.0f;
            }

            agent.velocity = Vector3.ClampMagnitude(agent.velocity + alignment + cohesion + seperation + repulsion, maxRunSpeed * enduranceFactor);
        } else {
            if (postFleeTicks * Time.deltaTime < 500) {
                postFleeTicks++;
            } else {
                isFleeing = false;
                preyMode = "flocking";
                agent.velocity = Vector3.ClampMagnitude(agent.velocity + alignment + cohesion + seperation, maxWalkSpeed);
            }
            
        }
    }

    // Updates the endurance for this Prey per time step.
    private void updateEndurance() {

        if (curSpeed > maxWalkSpeed) {
            //endurance -= 0.01f * Time.deltaTime;
            //endurance -= (1 - (endurance * 0.99995f)) * Time.deltaTime;
            endurance *= Mathf.Pow(0.985f, Time.deltaTime);
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

    // When called, exhibits the "disabled" state of a Prey.
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
