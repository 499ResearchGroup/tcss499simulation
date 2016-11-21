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
        personalSpaceRadius = 500;
        focusRadius = 25;
        killRange = 7.5f;
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
        calculateCurrentDestination();
        updateEndurance();
        checkAttackStatus();
    }

    // Calculates the Predator's new destination based on a modified Boids model. 
    private void calculateCurrentDestination() {
        // Create a detection radius and find all prey within it
        Collider[] hitColliders = Physics.OverlapSphere(this.transform.position, visionRadius);

        // store how many prey and friendly predators we detect
        int preyDetected = 0;
        int predatorsDetected = 0;
        int tooCloseNeighbors = 0;

        // seperate our boids so they dont get too close
        Vector3 seperation = Vector3.zero;
        // align our boids so we're moving in the same direction
        Vector3 alignment = Vector3.zero;
        // steer to move towards the avg position of flockmates
        Vector3 cohesion = Vector3.zero;

        // store the closest prey for calculating which prey to chase and the closest distance
        GameObject closestPrey = null;
        float closestDist = Mathf.Infinity;

        for (int i = 0; i < hitColliders.Length; i++) {
            GameObject curObject = hitColliders[i].gameObject;

            // if we see a prey
            if (curObject.tag == "PreyAgent") {
                PreyAgent getScript = curObject.GetComponent<PreyAgent>();
                preyDetected++;
                float curDist = Vector3.Distance(this.transform.position, curObject.transform.position);
                if (curDist < closestDist) {
                    closestPrey = curObject;
                    closestDist = curDist;
                }

                float calcDist = Vector3.Distance(this.transform.position, getScript.transform.position);
                if (calcDist <= personalSpaceRadius) {
                    seperation += (this.transform.position - getScript.transform.position) / calcDist;
                    tooCloseNeighbors++;
                }

                alignment += getScript.getVelocity();
                cohesion += getScript.transform.position;
            }

            // if we see a predator
            if (curObject.tag == "PredatorAgent") {
                PredatorAgent getScript = curObject.GetComponent<PredatorAgent>();
                alignment += getScript.getVelocity();
                cohesion += getScript.transform.position;

                float calcDist = Vector3.Distance(this.transform.position, getScript.transform.position);
                if (calcDist <= personalSpaceRadius) {
                    seperation += (this.transform.position - getScript.transform.position) / calcDist;
                    tooCloseNeighbors++;
                }
                predatorsDetected++;
            }
        }

        int sumDetected = preyDetected + predatorsDetected;

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
                alignment = (alignment / sumDetected);
                Debug.DrawRay(this.transform.position, alignment, Color.yellow);
                alignment.Normalize();
                alignment *= 0.5f;

                cohesion = (cohesion / sumDetected) - this.transform.position;
                Debug.DrawRay(this.transform.position, cohesion, Color.magenta);
                cohesion.Normalize();
                cohesion *= 0.1f;

                if (tooCloseNeighbors > 0) {
                    seperation = (seperation / sumDetected);
                    Debug.DrawRay(this.transform.position, seperation, Color.blue);
                    seperation.Normalize();
                    seperation *= 0.5f;
                }

                Vector3 newVelocity = agent.velocity + seperation + cohesion + alignment;
                newVelocity = Vector3.ClampMagnitude(newVelocity, (float) maxRunSpeed * endurance);

                agent.velocity = (newVelocity);
            }
        // else we haven't seen any prey, maneuver in a search radius
        //
        // TO-DO: chase avg predator flock position? 
        } else {
            curTargetName = "NO TARGET SELECTED";
            agent.ResetPath();
        }
    }

    private void initiateAttack(GameObject closestPrey) {
        PreyAgent getPreyScript = closestPrey.GetComponent<PreyAgent>();
        getPreyScript.sapEndurance(0.10f);

        // if the current time is greater than (initiated + attackTime), we can attack
        if (Time.time >= (attackTimeStampInitiated + attackTime)) {
            predatorState = 1;
            attackTimeStampInitiated = Time.time;
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
}