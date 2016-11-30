using UnityEngine;
using System.Collections;


// Script utilized for behavior on our Predator GameObjects
public class PredatorAgent : MonoBehaviour {


    private NavMeshAgent agent;
    private Animation animate;

    private float maxWalkSpeed;
    private float maxRunSpeed;


    // The following three fields are used for animation controlling.
    // 0 = running, walking
    // 1 = attacking
    private int predatorState;
    // Time in seconds to execute a full attack.
    private float attackTime;
    // A time stamp for when the attack was initiated.
    private float attackTimeStampInitiated;

    private Vector3 previousPosition;
    private float curSpeed;
    private bool selected;
    private string curTargetName;

    private float endurance;
    private float visionRadius;
    private float personalSpaceRadius;
    private float killRange;

    // Use this for initialization
    void Start() {
        agent = GetComponent<NavMeshAgent>();
        agent.autoBraking = false;
        Transform findChild = this.transform.Find("allosaurus_root");
        animate = findChild.GetComponent<Animation>();
        curTargetName = "NO TARGET SELECTED";
        previousPosition = transform.position;
        endurance = 1.0f;
        maxWalkSpeed = Config.PREDATOR_WALK_SPEED;
        maxRunSpeed = Config.PREDATOR_RUN_SPEED;
        visionRadius = Config.PREDATOR_VISION_RADIUS;
        personalSpaceRadius = agent.radius * 2;
        killRange = 8.5f;
        predatorState = 0;
        attackTime = 0.5f;
        attackTimeStampInitiated = Time.time;
    }

    // Update is called once per frame
    void Update() {
        Vector3 curMove = transform.position - previousPosition;
        curSpeed = curMove.magnitude / Time.deltaTime;
        previousPosition = transform.position;

        // if we're a predator
        updatePredator();

        if (predatorState == 1) {
            animate.CrossFade("Allosaurus_Attack01");
        } else {
            if (curSpeed > 0 && curSpeed <= maxWalkSpeed) {
                animate.CrossFade("Allosaurus_Walk");
            } else if (curSpeed > maxWalkSpeed) {
                animate.CrossFade("Allosaurus_Run");
            } else {
                animate.CrossFade("Allosaurus_Idle");
            }
        }
    }

    // Used for debugging, draws a sphere in the scene view
    void OnDrawGizmos() {
        Gizmos.DrawWireSphere(this.transform.position, visionRadius);
    }

    // Triggers when a mouse click collides with the BoxCollider on the Horse
    void OnMouseDown() {
        Debug.Log("I clicked on a Predator!");
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
            GUI.Label(new Rect(10, 40, 500, 20), "Current Target: " + curTargetName);
        }
    }

    // Allows the camera to let the agent know that it has been deselected
    public void deselect() {
        selected = false;
    }

    // Getter function to receive the agent's velocity in the simulation
    public Vector3 getVelocity() {
        return agent.velocity;
    }

	// The update predator function which contains all function calls necessary to update the state of the Predator.
    private void updatePredator() {
        //calculateCurrentDestination();
        calculateForces();
        updateEndurance();
        checkAttackStatus();
    }

	// Primary method for calculating new velocity for Predators. Utilizes a modified boids model with additional attraction vector
	// to push Predators dynamically towards the closest Prey.
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
        Vector3 attraction = Vector3.zero;

        // counter used to track number of prey within radius
        int preyDetected = 0;
        int predatorsDetected = 1;
        GameObject closestPrey = null;
        float closestDist = Mathf.Infinity;

        // loop through all of the things we've collided with
        for (int i = 0; i < hitColliders.Length; i++) {
            GameObject curObject = hitColliders[i].gameObject;

            if (curObject.tag == "PreyAgent") {
                preyDetected++;

                float distToPrey = Vector3.Distance(curObject.transform.position, this.transform.position);
                if (distToPrey < closestDist) {
                    closestPrey = curObject;
					closestDist = distToPrey;
                }
                attraction += (curObject.transform.position - this.transform.position) / (distToPrey * distToPrey * distToPrey);
            }

            if (curObject.tag == "PredatorAgent" && !curObject.Equals(this)) {
                predatorsDetected++;
                alignment += curObject.GetComponent<PredatorAgent>().getVelocity();
                cohesion += curObject.transform.position;
                if (Vector3.Distance(this.transform.position, curObject.transform.position) <= personalSpaceRadius) {
                    seperation -= (curObject.transform.position - this.transform.position);
                }
            }
        }

        cohesion /= predatorsDetected;
        alignment /= predatorsDetected;

        if (preyDetected > 0) {
            //attraction = (closestPrey.transform.position - this.transform.position);
            alignment *= 0.1f;
            seperation *= 2.5f;
            cohesion *= 0.1f;
            attraction *= 5000;
            checkIfInKillRange(closestDist, closestPrey);
            agent.velocity = Vector3.ClampMagnitude(agent.velocity + alignment + cohesion + seperation + attraction, maxRunSpeed * endurance);
        } else {
            agent.velocity = Vector3.ClampMagnitude(agent.velocity + alignment + cohesion + seperation, maxWalkSpeed * endurance);
        }
    }

	// Helper function to determine whether we're in kill range of a prey. 
	// Initiates an attack if inside of kill range, attacking the given prey. 
    private void checkIfInKillRange(float closestDist, GameObject prey) {
        if (closestDist <= killRange) {
            initiateAttack(prey);
        }
    }

	// Helper function to initiate an attack. 
	//
	// Attacks can happen once every "attackTime" seconds, where "attackTime" is in seconds.
    private void initiateAttack(GameObject closestPrey) {
        PreyAgent getPreyScript = closestPrey.GetComponent<PreyAgent>();
		//Debug.Log ("we're attempting to attack");
        // if the current time is greater than (initiated + attackTime), we can attack
        if (Time.time >= (attackTimeStampInitiated + attackTime)) {
			//Debug.Log ("we're initiating an attack");
            predatorState = 1;
            attackTimeStampInitiated = Time.time;
			getPreyScript.bitePrey();
        } 
    }

	// Helper function to determine whether our Predator can attack again. Switches the predatorState to "0" (not-attacking) if 
	// so. 
    private void checkAttackStatus() {
        // an attack is available, but we aren't attacking, reset our predatorState to 0
        if (Time.time >= (attackTimeStampInitiated + attackTime)) {
            predatorState = 0;
        }
        // else we must be attacking, don't interrupt 
    }

	// Updates the endurance on our Predator, then updates the speed with the new endurance value. 
    private void updateEndurance() {

        if (curSpeed >= maxWalkSpeed) {
            endurance -= 0.005f * Time.deltaTime;
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

        agent.speed = (float)(agent.speed * endurance);
    }
}