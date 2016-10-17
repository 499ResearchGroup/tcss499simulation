using UnityEngine;
using System.Collections;

public class HorseController : MonoBehaviour {

    Animator animator;
    private Rigidbody rb;

    // Parameters to determine maximum speed for player control
    public float maxWalkSpeed;
    public float maxTurnSpeed;
    public float maxRunSpeed;
    
    // Flag to determine whether the Horse is controlled by the player
    private bool playerControlled;

    private NavMeshAgent agent;

    private bool selected; 

	// Use this for initialization
	void Start () {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        rb.mass = 550;
        selected = false;


        // Used for player control
        playerControlled = false;
        maxWalkSpeed = 10;
        maxRunSpeed = 19;
        maxTurnSpeed = 150;

        //enablePlayerController();
	}
	
	// Update is called once per frame
	void Update () {

    }

    // FixedUpdate is called in sync with the physics engine 
    void FixedUpdate() {
        // If the enablePlayerController helper method is called, check player input
        if (playerControlled) {
            performPlayerInput();
        }
    }

    // Used to draw non-interactive UI elements to the screen 
    void OnGUI() {
        if (selected) {
            GUI.color = Color.red;
            GUI.Label(new Rect(10, 10, 500, 20), "Agent Information Goes Here");
        }

        if (playerControlled) {
            GUI.color = Color.red;
            GUI.Label(new Rect(10, 10, 500, 20), "Player Input Debug ON");
            GUI.Label(new Rect(10, 30, 500, 20), "Press WASD for movement");
            GUI.Label(new Rect(10, 50, 500, 20), "Press SHIFT for sprint");
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

    // Allows the camera to let the HorseController know that it has been deselected
    public void deselect() {
        selected = false;
    }

    // Used to manually control the movement of the Horse 
    private void enablePlayerController() {
        playerControlled = true;
    }

    // Performs player input checking when playerControl is enabled.
    private void performPlayerInput() {

        float moveHorizontal = Input.GetAxis("Horizontal") * maxTurnSpeed;
        float moveVertical;

        // If we're holding down our sprint key, sprint
        if (Input.GetKey(KeyCode.LeftShift)) {
            moveVertical = Input.GetAxis("Vertical") * maxRunSpeed;
        } else {
            moveVertical = Input.GetAxis("Vertical") * maxWalkSpeed;
        }

        // Old movement method translating the absolute position of the parent game object for our Horse
        // Better to use Rigidbody.addforce
        transform.Translate(0, 0, moveVertical * Time.deltaTime);
        transform.Rotate(0, moveHorizontal * Time.deltaTime, 0);

        // New testing method using addtorque and addforce
        //Vector3 turning = new Vector3(0.0f, moveHorizontal * -1.0f, 0.0f);
        //Vector3 movement = new Vector3(0.0f, 0.0f, moveVertical);

        //rb.AddTorque(turning);
        //rb.AddForce(movement);

        if (moveVertical > 0 && moveVertical <= maxWalkSpeed) {
            animator.SetInteger("movement_state", 1);
        } else if (moveVertical > maxWalkSpeed) {
            animator.SetInteger("movement_state", 2);
        } else {
            animator.SetInteger("movement_state", 0);
        }
    }
}
