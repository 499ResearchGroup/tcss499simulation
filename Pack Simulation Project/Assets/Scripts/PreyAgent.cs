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
    private float health;
    private string state;
    private float visionRadius;
    private float personalSpaceRadius;

    // Use this for initialization
    void Start() {
        agent = GetComponent<NavMeshAgent>();
        agent.autoBraking = false;
        endurance = 1.0f;
        health = 100;
        animator = GetComponent<Animator>();
        previousPosition = transform.position;
        maxWalkSpeed = Config.PREY_WALK_SPEED;
        maxRunSpeed = Config.PREY_RUN_SPEED;
        visionRadius = Config.PREY_VISION_RADIUS;
		personalSpaceRadius = agent.radius * 2;
        state = "relaxed";
	}

    // Update is called once per frame
    void Update() {
        Vector3 curMove = transform.position - previousPosition;
        curSpeed = curMove.magnitude / Time.deltaTime;
        previousPosition = transform.position;

        updatePrey();

        if (health > 0 && animator.enabled) {
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

    // Allows other agents to sap the endurance of the prey 
    public void sapEndurance(float reduction) {
        endurance -= reduction * Time.deltaTime;
        health -= (10 * Time.deltaTime);
    }

    // Getter function to receive the current agent's velocity in the simulation
    public Vector3 getVelocity() {
        return agent.velocity;
    }

    private void updatePrey() {
        if (health < 25) {
            exhibitDisabledState();
        } else {
            calculateCurrentDestination();
            updateEndurance();
        }
    }

    private void calculateCurrentDestination() {
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
                    alignment += getScript.getVelocity();
                    cohesion += getScript.transform.position;

                    float calcDistFlee = Vector3.Distance(this.transform.position, getScript.transform.position);
                    if (calcDistFlee <= personalSpaceRadius) {
                        numOfTooCloseFleeingNeighbors++;
                        seperation += ((this.transform.position - getScript.transform.position) / calcDistFlee);
                    }
                } else {
                    numOfNonFleeingNeighbors++;
                    nonFleeAlignment += getScript.getVelocity();
                    nonFleeCohesion += getScript.transform.position;

                    float calcDist = Vector3.Distance(this.transform.position, getScript.transform.position);
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
            agent.ResetPath();
            int sumDetected = numOfFleeingNeighbors;
            alignment = (alignment / sumDetected);
            Debug.DrawRay(this.transform.position, alignment, Color.yellow);
            alignment.Normalize();
            alignment *= 0.5f;

            cohesion = (cohesion / sumDetected) - this.transform.position;
            Debug.DrawRay(this.transform.position, nonFleeCohesion, Color.magenta);
            cohesion.Normalize();
            cohesion *= 0.1f;

            if (numOfTooCloseFleeingNeighbors > 0) {
                seperation = (seperation / sumDetected);
                Debug.DrawRay(this.transform.position, seperation, Color.blue);
                seperation.Normalize();
                seperation *= 0.5f;
            }

            Vector3 newVelocity = agent.velocity + seperation + cohesion + alignment;
            newVelocity = Vector3.ClampMagnitude(newVelocity, (maxRunSpeed * endurance));

            agent.velocity = (newVelocity);
        } else if (numOfNonFleeingNeighbors > 0) {
            // we haven't seen a predator, so make sure our speed isn't too fast
            // then follow the flock
            state = "following herd";
            agent.ResetPath();
            int sumDetected = numOfNonFleeingNeighbors;

            nonFleeAlignment = (nonFleeAlignment / sumDetected);
            Debug.DrawRay(this.transform.position, nonFleeAlignment, Color.yellow);
            nonFleeAlignment.Normalize();
            nonFleeAlignment *= 0.3f;

            nonFleeCohesion = (nonFleeCohesion / sumDetected) - this.transform.position;
            Debug.DrawRay(this.transform.position, nonFleeCohesion, Color.magenta);
            nonFleeCohesion.Normalize();
            nonFleeCohesion *= 0.1f;

            if (numOfTooCloseNonFleeNeighbors > 0) {
                nonFleeSeperation = (nonFleeSeperation / numOfTooCloseNonFleeNeighbors);
                Debug.DrawRay(this.transform.position, nonFleeSeperation, Color.blue);
                nonFleeSeperation.Normalize();
                nonFleeSeperation *= 1.0f;
            }

            Vector3 newVelocity = agent.velocity + nonFleeSeperation + nonFleeCohesion + nonFleeAlignment;
            newVelocity = newVelocity * maxWalkSpeed;
            newVelocity = Vector3.ClampMagnitude(newVelocity, (maxWalkSpeed * endurance));

            agent.velocity = (newVelocity);
        } else {
            // we haven't seen anything, including fellow prey
            // sample a random point in a small fixed circle and walk to it
            state = "relaxed";
            agent.ResetPath();
            agent.speed = maxWalkSpeed;
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
        agent.ResetPath();
        transform.Rotate(new Vector3(transform.rotation.x, transform.rotation.y, 90));
        BoxCollider collide = this.GetComponent<BoxCollider>();
        animator.enabled = false;
        collide.enabled = false;
    }

}
