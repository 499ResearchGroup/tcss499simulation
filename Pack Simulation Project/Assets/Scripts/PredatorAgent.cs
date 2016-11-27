using UnityEngine;
using System.Collections;


// Script utilized to perform AI behavior on our Prey GameObjects
public class PredatorAgent : MonoBehaviour {


    private NavMeshAgent agent;
    private Animation animate;

    private float maxWalkSpeed;
    private float maxRunSpeed;


    // the following three fields are used for animation controlling
    // 0 = running, walking
    // 1 = attacking
    private int predatorState;
    // time in seconds to execute a full attack
    private float attackTime;
    // a time stamp for when the attack was initiated
    private float attackTimeStampInitiated;

    private Vector3 previousPosition;
    private float curSpeed;
    private bool selected;
    private string curTargetName;

    private float endurance;
    private float visionRadius;
    private float focusRadius;
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
        focusRadius = 35;
        killRange = 10.5f;
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

    private void updatePredator() {
        //calculateCurrentDestination();
        calculateForces();
        updateEndurance();
        checkAttackStatus();
    }


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
                }
                //attraction += (curObject.transform.position - this.transform.position) / (distToPrey);
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
            attraction = (closestPrey.transform.position - this.transform.position);
            alignment *= 0.1f;
            seperation *= 0.1f;
            cohesion *= 0.05f;
            attraction *= 50;
            checkIfInKillRange(closestDist, closestPrey);
            agent.velocity = Vector3.ClampMagnitude(agent.velocity + alignment + cohesion + seperation + attraction, maxRunSpeed * endurance);
        } else {
            agent.velocity = Vector3.ClampMagnitude(agent.velocity + alignment + cohesion + seperation, maxWalkSpeed * endurance);
        }
    }

    private void checkIfInKillRange(float closestDist, GameObject prey) {
        if (closestDist <= killRange) {
            initiateAttack(prey);
        }
    }

    private void initiateAttack(GameObject closestPrey) {
        PreyAgent getPreyScript = closestPrey.GetComponent<PreyAgent>();

        // if the current time is greater than (initiated + attackTime), we can attack
        if (Time.time >= (attackTimeStampInitiated + attackTime)) {
            predatorState = 1;
            attackTimeStampInitiated = Time.time;
			getPreyScript.bitePrey();
        } 
    }

    private void checkAttackStatus() {
        // an attack is available, but we aren't attacking, reset our predatorState to 0
        if (Time.time >= (attackTimeStampInitiated + attackTime)) {
            predatorState = 0;
        }
        // else we must be attacking, don't interrupt 
    }

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

    // DEPRECATED VERSION OF PREDATOR BEHAVIOR: USE FOR REFERENCE ONLY
    private void calculateCurrentDestination() {
        // Create a detection radius and find all prey within it
        Collider[] hitColliders = Physics.OverlapSphere(this.transform.position, visionRadius);

        // store how many prey and friendly predators we detect
        int preyDetected = 0;
        int predatorsDetected = 0;
        int tooCloseNeighbors = 0;

        // seperate our boids so they dont get too close
        Vector3 seperation = Vector3.zero;
        Vector3 predOnlySeperation = Vector3.zero;
        // align our boids so we're moving in the same direction
        Vector3 alignment = Vector3.zero;
        Vector3 predOnlyAlignment = Vector3.zero;
        // steer to move towards the avg position of flockmates
        Vector3 cohesion = Vector3.zero;
        Vector3 predOnlyCohesion = Vector3.zero;

        float weightSum = 0.0f;

        // store the closest prey for calculating which prey to chase and the closest distance
        GameObject closestPrey = null;
        float closestDist = Mathf.Infinity;

        for (int i = 0; i < hitColliders.Length; i++) {
            GameObject curObject = hitColliders[i].gameObject;

            // if we see a prey
            if (curObject.tag == "PreyAgent") {
                PreyAgent getScript = curObject.GetComponent<PreyAgent>();
                preyDetected++;
                float calcDist = Vector3.Distance(this.transform.position, getScript.transform.position);
                if (calcDist < closestDist) {
                    closestPrey = curObject;
                    closestDist = calcDist;
                }

                alignment += getScript.getVelocity();
                //cohesion += getScript.transform.position * (1 / calcDist * calcDist);
                //weightSum += (1 / calcDist * calcDist);
            }

            // if we see a predator
            if (curObject.tag == "PredatorAgent") {
                PredatorAgent getScript = curObject.GetComponent<PredatorAgent>();
                predOnlyAlignment += getScript.getVelocity();
                predOnlyCohesion += getScript.transform.position;

                float calcDist = Vector3.Distance(this.transform.position, getScript.transform.position);
                if (calcDist <= personalSpaceRadius) {
                    predOnlySeperation += (this.transform.position - getScript.transform.position);
                    seperation += (this.transform.position - getScript.transform.position);
                    tooCloseNeighbors++;
                }
                predatorsDetected++;
            }
        }

        // if we've seen a prey chase the closest prey
        if (preyDetected > 0 && closestPrey != null) {
            // if we're close to a prey, zero in on it
            if (closestDist <= focusRadius) {
                //Debug.Log("Closest Dist: " + closestDist + ", FocusRadius: " + focusRadius);
                agent.SetDestination(closestPrey.transform.position);
                agent.speed = maxRunSpeed;
                // if we're in kill range to the closest prey, kill it
                if (closestDist <= killRange) {
                    initiateAttack(closestPrey);
                }
                // if we're not in focus range, chase the avg position of the pack 
            } else {
                agent.ResetPath();
                alignment = (alignment / preyDetected);
                Debug.DrawRay(this.transform.position, alignment, Color.yellow);
                alignment.Normalize();
                alignment *= 0.25f;

                //Debug.Log("Weight sum: " + weightSum);
                // cohesion = (cohesion / preyDetected) - this.transform.position);
                //cohesion = (cohesion / (weightSum)) - this.transform.position;
                cohesion = closestPrey.transform.position - this.transform.position;
                Debug.DrawRay(this.transform.position, cohesion, Color.magenta);
                cohesion.Normalize();
                cohesion *= 0.9f;
                //Debug.Log("Cohesion vector: " + cohesion);

                if (tooCloseNeighbors > 0) {
                    seperation = (seperation / predatorsDetected);
                    Debug.DrawRay(this.transform.position, seperation, Color.blue);
                    seperation.Normalize();
                    seperation *= 0.5f;
                }

                Vector3 newVelocity = agent.velocity + cohesion + alignment + seperation;
                newVelocity = Vector3.ClampMagnitude(newVelocity, (float)maxRunSpeed * endurance);

                agent.velocity = (newVelocity);
            }
            // else we haven't seen any prey, attempt to flock with fellow predators
        } else {
            curTargetName = "NO TARGET SELECTED";
            agent.ResetPath();
            if (predatorsDetected > 0) {
                cohesion = (cohesion / predatorsDetected) - this.transform.position;
                cohesion.Normalize();
                cohesion *= 0.5f;
                alignment = (alignment / predatorsDetected);
                alignment.Normalize();
                alignment *= 0.1f;
                if (tooCloseNeighbors > 0) {
                    seperation = (seperation / predatorsDetected);
                    seperation.Normalize();
                    seperation *= 0.9f;
                    Debug.DrawRay(this.transform.position, seperation, Color.blue);
                }
                agent.velocity = Vector3.ClampMagnitude(agent.velocity + cohesion + alignment + seperation, (float)maxWalkSpeed * endurance);
            } else {
                agent.velocity = agent.velocity;
            }
        }
    }
}