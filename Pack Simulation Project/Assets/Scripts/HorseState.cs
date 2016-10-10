using UnityEngine;
using System.Collections;

public class HorseState : MonoBehaviour {


    // TESTING ANIMATION STATE TRANSITIONS
    // 0 == IDLE
    // 1 == WALK
    // 2 == RUN
    Animator animator;
    Rigidbody rb;

    private float timer;

    public float walkSpeed;
    public float runSpeed;

    // Debug flag to determine whether or not to draw debug text to the screen
    private bool animDebug;

	// Use this for initialization
	void Start () {
        Debug.Log("Hello World!");
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        animDebug = false;
        timer = 2.5f;
        walkSpeed = 25;
        runSpeed = 100;
	}
	
	// Update is called once per frame
	void Update () {
        // horseAgentAI();
        animationDebugInput();
        // randomStateChange();
	}

    // Used to draw non-interactive UI elements to the screen 
    void OnGUI() {
        if (animDebug) {
            GUI.Label(new Rect(10, 10, 500, 20), "Animation Input Debug ON");
            GUI.Label(new Rect(10, 30, 500, 20), "Press 1 for IDLE");
            GUI.Label(new Rect(10, 50, 500, 20), "Press 2 for WALK");
            GUI.Label(new Rect(10, 70, 500, 20), "Press 3 for RUN");
        }
    }

    private void horseAgentAI() {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
        rb.velocity = movement * 500;
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
