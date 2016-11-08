using UnityEngine;
using System.Collections;


// Script utilized to perform AI behavior on our Prey GameObjects
public class PreyAgent : MonoBehaviour {


    private NavMeshAgent agent;
    private Animator animator;

    private float maxWalkSpeed;
    private float maxRunSpeed;

    private Vector3 previousPosition;
    private float curSpeed;
    private bool selected;

    private double endurance;
    private int health;
    private string state;
    private float visionRadius;
    private float personalSpaceRadius;

    // Use this for initialization
    void Start() {
        agent = GetComponent<NavMeshAgent>();
        agent.autoBraking = false;
        endurance = 1.0;
        health = 100;
        animator = GetComponent<Animator>();
        previousPosition = transform.position;
        maxWalkSpeed = 5.0f;
        maxRunSpeed = 15.0f;
        visionRadius = 50;
		personalSpaceRadius = agent.radius * 2;
        state = "relaxed";
	}
	
	// Update is called once per frame
	void Update() {
        Vector3 curMove = transform.position - previousPosition;
        curSpeed = curMove.magnitude / Time.deltaTime;
        previousPosition = transform.position;

        updatePrey();

        if (curSpeed > 0 && curSpeed <= (maxWalkSpeed + 1)) {
            animator.SetInteger("movement_state", 1);
        } else if (curSpeed > maxWalkSpeed + 1) {
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

    // Used for debugging, draws a sphere in the scene view
    void OnDrawGizmos() {
        Gizmos.DrawWireSphere(this.transform.position, visionRadius);
    }

    // Allows the camera to let the HorseAgent know that it has been deselected
    public void deselect() {
        selected = false;
    }

    // Getter function to receive the current agent's velocity in the simulation
    public Vector3 getVelocity() {
        return agent.velocity;
    }

    private void updatePrey() {
        // create a detection radius and find all predators within it
        Collider[] hitColliders = Physics.OverlapSphere(this.transform.position, visionRadius);
        Vector3 normalizedCenter = new Vector3(0, 0, 0);
        // seperate our boids so they dont get too close
        Vector3 seperation = Vector3.zero;
        Vector3 nonFleeSeperation = Vector3.zero;
        // align our boids so we're moving in the same direction
        Vector3 alignment = Vector3.zero;
        Vector3 nonFleeAlignment = Vector3.zero;
        // steer to move towards the avg position of flockmates
        Vector3 cohesion = Vector3.zero;
        Vector3 nonFleeCohesion = Vector3.zero;
        int numOfPred = 0;
        int numOfFleeingNeighbors = 0;
		int numOfTooCloseFleeingNeighbors = 0;
		int numOfNonFleeingNeighbors = 0;
		int numOfTooCloseNonFleeNeighbors = 0;
        for (int i = 0; i < hitColliders.Length; i++) {
            GameObject curObject = hitColliders[i].gameObject;
            if (curObject.tag == "PredatorAgent") {
                // if the thing we collided with is a predator
                //Debug.Log("A Prey's vision has collided with a Predator.");
                // run away!
                numOfPred++;
                normalizedCenter += curObject.transform.position;
            }

            if (curObject.tag == "PreyAgent" && !curObject.Equals(this.gameObject)) {
                PreyAgent getScript = curObject.GetComponent<PreyAgent>();
				if (getScript.state == "fleeing") {
					numOfFleeingNeighbors++;
					alignment += getScript.getVelocity ();
					cohesion += getScript.transform.position;

					float calcDistFlee = Vector3.Distance (this.transform.position, getScript.transform.position);
					if (calcDistFlee <= personalSpaceRadius) {
						numOfTooCloseFleeingNeighbors++;
						seperation += ((this.transform.position - getScript.transform.position) / calcDistFlee);
					}
				} else {
					numOfNonFleeingNeighbors++;
					nonFleeAlignment += getScript.getVelocity ();
					nonFleeCohesion += getScript.transform.position;

					float calcDist = Vector3.Distance (this.transform.position, getScript.transform.position);
					if (calcDist <= personalSpaceRadius) {
						numOfTooCloseNonFleeNeighbors++;
						nonFleeSeperation += ((this.transform.position - getScript.transform.position) / calcDist);
					}
				}
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
        } else if (numOfFleeingNeighbors > 0) {
        // we don't see predators but we see fellow prey that are fleeing... 
        // use boids model for flock simulation
            state = "alarmed";
			agent.ResetPath ();
            int sumDetected = numOfFleeingNeighbors;
            alignment = (alignment / sumDetected);
			Debug.DrawRay(this.transform.position, alignment, Color.yellow);
			//alignment = Vector3.ClampMagnitude (alignment, maxRunSpeed);
            alignment.Normalize();

			cohesion = (cohesion / sumDetected) - this.transform.position;
			Debug.DrawRay(this.transform.position, nonFleeCohesion, Color.magenta);
			//cohesion = Vector3.ClampMagnitude (cohesion, maxRunSpeed);
            cohesion.Normalize();

			if (numOfTooCloseFleeingNeighbors > 0) {
	            seperation = (seperation / sumDetected);
				//seperation = Vector3.ClampMagnitude (seperation, maxRunSpeed);
				Debug.DrawRay(this.transform.position, seperation, Color.blue);
	            seperation.Normalize();
			}

			Vector3 newVelocity = agent.velocity + seperation + cohesion + alignment;
			newVelocity = Vector3.ClampMagnitude(newVelocity, maxRunSpeed);

			agent.velocity = (newVelocity);
            //agent.SetDestination(this.transform.position + (seperation + cohesion + alignment));
            //agent.speed = maxRunSpeed;
        } else if (numOfNonFleeingNeighbors > 0) {
            // we haven't seen a predator, so make sure our speed isn't too fast
            // then follow the flock
            state = "following herd";
			agent.ResetPath ();
            int sumDetected = numOfNonFleeingNeighbors;

            nonFleeAlignment = (nonFleeAlignment / sumDetected);
			//nonFleeAlignment = Vector3.ClampMagnitude (nonFleeAlignment, maxWalkSpeed);
			Debug.DrawRay (this.transform.position, nonFleeAlignment, Color.yellow);
            nonFleeAlignment.Normalize();

			nonFleeCohesion = (nonFleeCohesion / sumDetected) - this.transform.position;
			//nonFleeCohesion = Vector3.ClampMagnitude (nonFleeCohesion, maxWalkSpeed);
			Debug.DrawRay(this.transform.position, nonFleeCohesion, Color.magenta);
			nonFleeCohesion.Normalize();

			if (numOfTooCloseNonFleeNeighbors > 0) {
				nonFleeSeperation = (nonFleeSeperation / numOfTooCloseNonFleeNeighbors);
				//nonFleeSeperation = Vector3.ClampMagnitude (nonFleeSeperation, maxWalkSpeed);
				Debug.DrawRay (this.transform.position, nonFleeSeperation, Color.blue);
				nonFleeSeperation.Normalize();
			}

			Vector3 newVelocity = agent.velocity + nonFleeSeperation + nonFleeCohesion + nonFleeAlignment;
			newVelocity = newVelocity * maxWalkSpeed;
			newVelocity= Vector3.ClampMagnitude (newVelocity, maxWalkSpeed);

			agent.velocity = (newVelocity);
            //agent.SetDestination(this.transform.position + (nonFleeSeperation + nonFleeCohesion + nonFleeAlignment));
            //agent.speed = maxWalkSpeed;
        } else {
            state = "relaxed";
            agent.ResetPath();
            agent.speed = maxWalkSpeed;
        }

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

        if (endurance > 1) {
            endurance = 1;
        }

        agent.speed = (float) (agent.speed * endurance);
    }

}
