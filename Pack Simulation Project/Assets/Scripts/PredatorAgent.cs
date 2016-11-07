using UnityEngine;
using System.Collections;


// Script utilized to perform AI behavior on our Prey GameObjects
public class PredatorAgent : MonoBehaviour {


    private NavMeshAgent agent;
    private Animation animate;

    private float maxWalkSpeed;
    private float maxRunSpeed;

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
        maxWalkSpeed = 5;
        maxRunSpeed = 12.5f;
        visionRadius = 100;
        personalSpaceRadius = 500;
        killRange = 5;
    }

    // Update is called once per frame
    void Update() {
        Vector3 curMove = transform.position - previousPosition;
        curSpeed = curMove.magnitude / Time.deltaTime;
        previousPosition = transform.position;

        // if we're a predator
        updatePredator();

        if (curSpeed > 0 && curSpeed <= maxWalkSpeed) {
            animate.CrossFade("Allosaurus_Walk");
        } else if (curSpeed > maxWalkSpeed) {
            animate.CrossFade("Allosaurus_Run");
        } else {
            animate.CrossFade("Allosaurus_Idle");
        }
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
        calculateSimpleFollow();
        //calculateCurrentDestination();
    }

    // Calculates the Predator's new destination based on a modified Boids model. 
    private void calculateCurrentDestination() {
        // Create a detection radius and find all prey within it
        Collider[] hitColliders = Physics.OverlapSphere(this.transform.position, visionRadius);

        // store how many prey and friendly predators we detect
        int preyDetected = 0;
        int predatorsDetected = 0;

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
                }

                //alignment += getScript.getVelocity();
                //cohesion += getScript.transform.position;
                //seperation += (getScript.transform.position - this.transform.position);
            }

            // if we see a predator
            if (curObject.tag == "PredatorAgent") {
                PredatorAgent getScript = curObject.GetComponent<PredatorAgent>();
                //alignment += getScript.getVelocity();
                //cohesion += getScript.transform.position;

                float calcDist = Vector3.Distance(this.transform.position, getScript.transform.position);
                if (calcDist <= personalSpaceRadius) {
                    seperation += (getScript.transform.position - this.transform.position);
                }
                predatorsDetected++;
            }
        }

        int sumDetected = preyDetected + predatorsDetected;

        // if we've seen more than one prey chase the closest prey
        if (preyDetected > 0 && closestPrey != null) {
            // if we're in kill range to the closest prey, kill it
            if (closestDist <= killRange) {
                agent.SetDestination(closestPrey.transform.position);
                // TO-DO: add kill behavior
            // if we're not in kill range, chase the avg position of the pack 
            } else {
                PreyAgent getScript = closestPrey.GetComponent<PreyAgent>();
                //alignment = (alignment / sumDetected);
                //alignment.Normalize();

                //cohesion = (cohesion / sumDetected) - this.transform.position;
                //cohesion.Normalize();

                cohesion += closestPrey.transform.position;
                //cohesion.Normalize();

                seperation = (seperation / predatorsDetected) * -1;
                //seperation.Normalize();

                agent.SetDestination(cohesion + seperation);
                agent.speed = maxRunSpeed;
            }
        // else we haven't seen any prey, do nothing
        //
        // TO-DO: chase avg predator flock position? 
        } else {
            curTargetName = "NO TARGET SELECTED";
            agent.ResetPath();
        }
    }

    // Simple following AI for predators, to be used while developing more complex behavior
    // Predators calculate the most nearby Prey and follow it
    private void calculateSimpleFollow() {
        // create a detection radius and find all prey within it
        Collider[] hitColliders = Physics.OverlapSphere(this.transform.position, visionRadius);
        GameObject closestPrey = null;
        float closestDist = Mathf.Infinity;
        int preyDetected = 0;
        for (int i = 0; i < hitColliders.Length; i++) {
            GameObject curObject = hitColliders[i].gameObject;
            if (curObject.tag == "PreyAgent") {
                curTargetName = curObject.transform.name;
                preyDetected++;
                float curDist = Vector3.Distance(this.transform.position, curObject.transform.position);
                if (curDist < closestDist) {
                    closestPrey = curObject;
                }
            }
        }

        if (preyDetected < 1) {
            curTargetName = "NO TARGET SELECTED";
            agent.ResetPath();
            agent.speed = maxWalkSpeed;
        } else {
            curTargetName = closestPrey.name;
            agent.SetDestination(closestPrey.transform.position);
            agent.speed = maxRunSpeed;
        }
    }

}
