using UnityEngine;
using System.Collections;

public class HorseController : MonoBehaviour {


    // TESTING ANIMATION STATE TRANSITIONS
    // 0 == IDLE
    // 1 == WALK
    // 2 == RUN
    Animator animator;

    private float timer;

    public float maxWalkSpeed;
    public float maxTurnSpeed;
    public float maxRunSpeed;

    // Debug flag to determine whether or not to draw debug text to the screen
    private bool animDebug;
    
    // Flag to determine whether the Horse is controlled by the player
    private bool playerControlled;

	// Use this for initialization
	void Start () {
        Debug.Log("Hello World!");
        animator = GetComponent<Animator>();
        animDebug = false;
        playerControlled = false;
        timer = 2.5f;
        maxWalkSpeed = 5;
        maxRunSpeed = 9.5f;
        maxTurnSpeed = 150;

        enablePlayerController();
	}
	
	// Update is called once per frame
	void Update () {
        // horseAgentAI();
        // animationDebugInput();
        // randomStateChange();

        // If the enablePlayerControl helper method is called, check player input
        if (playerControlled) {

            float turnSpeed = Input.GetAxis("Horizontal") * maxTurnSpeed;
            float walkSpeed;

            if (Input.GetKey(KeyCode.LeftShift)) {
                walkSpeed = Input.GetAxis("Vertical") * maxRunSpeed;
            } else {
                walkSpeed = Input.GetAxis("Vertical") * maxWalkSpeed;
            }

            //Debug.Log("Current walkSpeed: " + walkSpeed);

            transform.Translate(0, 0, walkSpeed * Time.deltaTime);
            transform.Rotate(0, turnSpeed * Time.deltaTime, 0);

            if (walkSpeed > 0 && walkSpeed <= 5) {
                animator.SetInteger("movement_state", 1);
            } else if (walkSpeed > 5) {
                animator.SetInteger("movement_state", 2);
            } else {
                animator.SetInteger("movement_state", 0);
            }
        }
    }

    void FixedUpdate() {

    }

    // Used to draw non-interactive UI elements to the screen 
    void OnGUI() {
        if (animDebug) {
            GUI.color = Color.red;
            GUI.Label(new Rect(10, 10, 500, 20), "Animation Input Debug ON");
            GUI.Label(new Rect(10, 30, 500, 20), "Press 1 for IDLE");
            GUI.Label(new Rect(10, 50, 500, 20), "Press 2 for WALK");
            GUI.Label(new Rect(10, 70, 500, 20), "Press 3 for RUN");
        }

        if (playerControlled) {
            GUI.color = Color.red;
            GUI.Label(new Rect(10, 10, 500, 20), "Player Input Debug ON");
            GUI.Label(new Rect(10, 30, 500, 20), "Press WASD for movement");
            GUI.Label(new Rect(10, 50, 500, 20), "Press SHIFT for sprint");
        }
    }

    // Used to give the Horse AI behavior
    private void horseAgentAI() {

    }

    // Used to manually control the movement of the Horse 
    private void enablePlayerController() {
        playerControlled = true;
    }

    // Triggers a random animation state change 
    private void randomStateChange() {
        timer -= Time.deltaTime;

        if (timer <= 0) {
            Debug.Log("timer is up! prev state: " + animator.GetInteger("movement_state"));
            int newState = (int)Mathf.Floor(Random.Range(0, 2.999f));
            Debug.Log("new timer state: " + newState);
            animator.SetInteger("movement_state", newState);
            timer = 2.5f;
        }
    }

    // Changes animation state based on user input
    // Add to Update() to implement
    private void animationDebugInput() {
        animDebug = true;

        if (Input.GetKeyDown("1"))
        {
            animator.SetInteger("movement_state", 0);
            Debug.Log("Set movement to state 0 (IDLE)");
        }

        if (Input.GetKeyDown("2"))
        {
            animator.SetInteger("movement_state", 1);
            Debug.Log("Set movement to state 1 (WALK)");
        }

        if (Input.GetKeyDown("3"))
        {
            animator.SetInteger("movement_state", 2);
            Debug.Log("Set movement to state 2 (RUN)");
        }
    }

    private void drawDebugControls() {

    }
}
