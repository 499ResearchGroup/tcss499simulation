using UnityEngine;
using System.Collections;


// Script utilized to perform AI behavior on our Horse GameObjects
public class HorseAgent : MonoBehaviour {


    public Transform[] points;
    private NavMeshAgent agent;
    private Animator animator;
    private int curDestination;

    private float maxWalkSpeed;
    private float maxRunSpeed;

    private Vector3 previousPosition;
    private float curSpeed;

    // Use this for initialization
    void Start () {
        agent = GetComponent<NavMeshAgent>();
        agent.autoBraking = false;
        animator = GetComponent<Animator>();
        previousPosition = transform.position;
        curDestination = 0;
        maxWalkSpeed = 5;
        maxRunSpeed = 15;
        GoToNextPoint();
	}
	
	// Update is called once per frame
	void Update () {
        Vector3 curMove = transform.position - previousPosition;
        curSpeed = curMove.magnitude / Time.deltaTime;
        previousPosition = transform.position;

        if (agent.remainingDistance < 0.5f) {
            GoToNextPoint();
        }

        if (curSpeed > 0 && curSpeed <= maxWalkSpeed) {
            animator.SetInteger("movement_state", 1);
        } else if (curSpeed > maxWalkSpeed) {
            animator.SetInteger("movement_state", 2);
        } else {
            animator.SetInteger("movement_state", 0);
        }

    }

    private void GoToNextPoint() {
        if (points.Length == 0) {
            Debug.Log("No waypoints have been set up.");
            return;
        }

        agent.destination = points[curDestination].position;

        curDestination = (curDestination + 1) % points.Length;
    }
}
