using UnityEngine;
using System.Collections;


// Script utilized to perform AI behavior on our Prey GameObjects
public class PredatorAgent : MonoBehaviour {


    private NavMeshAgent agent;
    private Animation animation;
    private int curDestination;

    private float maxWalkSpeed;
    private float maxRunSpeed;

    private Vector3 previousPosition;
    private float curSpeed;
    private bool selected;
    private string curTargetName;

    private float endurance;

    // Use this for initialization
    void Start() {
        agent = GetComponent<NavMeshAgent>();
        agent.autoBraking = false;
        Transform findChild = this.transform.Find("allosaurus_root");
        animation = findChild.GetComponent<Animation>();
        curTargetName = "NO TARGET SELECTED";
        previousPosition = transform.position;
        curDestination = 0;
        endurance = 1.0f;
        maxWalkSpeed = 5;
        maxRunSpeed = 10;
    }

    // Update is called once per frame
    void Update() {
        Vector3 curMove = transform.position - previousPosition;
        curSpeed = curMove.magnitude / Time.deltaTime;
        previousPosition = transform.position;

        // if we're a predator
        updatePredator();

        if (curSpeed > 0 && curSpeed <= maxWalkSpeed) {
            animation.CrossFade("Allosaurus_Walk");
        } else if (curSpeed > maxWalkSpeed) {
            animation.CrossFade("Allosaurus_Run");
        } else {
            animation.CrossFade("Allosaurus_Idle");
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

    private void updatePredator() {
        // create a detection radius and find all prey within it
        Collider[] hitColliders = Physics.OverlapSphere(this.transform.position, 100);
        int preyDetected = 0;
        for (int i = 0; i < hitColliders.Length; i++) {
            GameObject curObject = hitColliders[i].gameObject;
            if (curObject.tag == "PreyAgent") {
                PreyAgent getScript = curObject.GetComponent<PreyAgent>();
                curTargetName = curObject.transform.name;
                preyDetected++;
                Debug.Log("We've seen a prey");
                agent.SetDestination(curObject.transform.position);
                agent.speed = maxRunSpeed;
            }
        }

        if (preyDetected < 1) {
            curTargetName = "NO TARGET SELECTED";
        }
    }

}
