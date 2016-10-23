using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {

    //Private variable to store the offset distance between the player and camera
    //private Vector3 offset;

    // Reference to our Horse GameObject
    private Transform target;

    // Speed of the camera for WASD camera movement
    private float WASDCameraSpeed;

    // The distance in the x-z plane to the target
    private float distance = 10.0f;
    // the height we want the camera to be above the target
    private float height = 5.0f;

    private float rotationDamping;
    private float heightDamping;

    // Minimum and maximum horizontal rotation
    public float minX = -360.0f;
    public float maxX = 360.0f;

    // Minimum and maximum vertical rotation
    public float minY = -45.0f;
    public float maxY = 45.0f;

    // Adjusts sensitivity of mouse input for camera mouse look
    public float sensX = 100.0f;
    public float sensY = 100.0f;

    float rotationY = 0.0f;
    float rotationX = 0.0f;

    // Use this for initialization
    void Start() {
        //Calculate and store the offset value by getting the distance between the player's position and camera's position.
        target = null;
        WASDCameraSpeed = 10f;
        heightDamping = 0f;
        rotationDamping = 0f;
        //offset = new Vector3(0, 10f, 5f);
    }

    // LateUpdate is called after Update each frame
    void LateUpdate() {
        if (target == null) {
            checkWASDCamera();
        } else {
            smoothCamera();
        }

        checkMouseInput();

        // If space bar is pressed detach from the current focucs
        if (target != null && Input.GetKey(KeyCode.Space)) {
            HorseAgent targetScript = target.gameObject.GetComponent<HorseAgent>();
            targetScript.deselect();
            target = null;
        }
    }

    public void changeObjectFocus(Transform agentPos) {
        target = agentPos;
    }

    private void checkWASDCamera() {
        if (Input.GetKey(KeyCode.D)) {
            transform.Translate(new Vector3(WASDCameraSpeed * Time.deltaTime, 0, 0));
        }
        if (Input.GetKey(KeyCode.A)) {
            transform.Translate(new Vector3(-WASDCameraSpeed * Time.deltaTime, 0, 0));
        }
        if (Input.GetKey(KeyCode.S)) {
            transform.Translate(new Vector3(0, 0, -WASDCameraSpeed * Time.deltaTime));
        }
        if (Input.GetKey(KeyCode.W)) {
            transform.Translate(new Vector3(0, 0, WASDCameraSpeed * Time.deltaTime));
        }
    }

    private void checkMouseInput() {
        // If the right mouse button is held down, receive mouse input to rotate the camera
        if (Input.GetMouseButton(1)) {
            rotationX += Input.GetAxis("Mouse X") * sensX * Time.deltaTime;
            rotationY += Input.GetAxis("Mouse Y") * sensY * Time.deltaTime;
            rotationY = Mathf.Clamp(rotationY, minY, maxY);
            transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
        }
    }


    // Code copied from standard assets SmoothFollow camera
    private void smoothCamera() {
        // Calculate the current rotation angles
        var wantedRotationAngle = target.eulerAngles.y;
        var wantedHeight = target.position.y + height;

        var currentRotationAngle = transform.eulerAngles.y;
        var currentHeight = transform.position.y;

        // Damp the rotation around the y-axis
        currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle, rotationDamping * Time.deltaTime);

        // Damp the height
        currentHeight = Mathf.Lerp(currentHeight, wantedHeight, heightDamping * Time.deltaTime);

        // Convert the angle into a rotation
        var currentRotation = Quaternion.Euler(0, currentRotationAngle, 0);

        // Set the position of the camera on the x-z plane to:
        // distance meters behind the target
        transform.position = target.position;
        transform.position -= currentRotation * Vector3.forward * distance;

        // Set the height of the camera
        transform.position = new Vector3(transform.position.x, currentHeight, transform.position.z);

        // Always look at the target
        transform.LookAt(target);
    }
}
