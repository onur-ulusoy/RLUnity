using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro; 


public class FirstPersonController : MonoBehaviour
{
    public float speed = 5.0f;
    public float jumpForce = 5.0f;
    public float mouseSensitivity = 100.0f;
    public float flySpeed = 7.0f;

    public Transform cameraTransform;

    private Rigidbody rb;
    private bool isGrounded;
    private bool isFlying = false;
    private float xRotation = 0f;
    private float lastSpaceTime = -1f;
    private float doubleSpaceThreshold = 0.3f; // Time in seconds to consider it a double press

    private Camera mainCamera;
    private bool controlsEnabled = true; // Flag to toggle controls

    public bool boxInteraction = false; // Flag for box interaction mode
    public TMP_InputField initialXInput;
    public TMP_InputField initialYInput;
    public TMP_InputField initialZInput;
    public TMP_InputField destinationXInput;
    public TMP_InputField destinationYInput;
    public TMP_InputField destinationZInput;

    public SimController simController; // Reference to SimController

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        mainCamera = Camera.main; // Assign the main camera

        // Lock the cursor to the center of the screen and make it invisible
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Toggle controls with ESC key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            controlsEnabled = !controlsEnabled;
            Cursor.lockState = controlsEnabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !controlsEnabled;
        }

        if (!controlsEnabled)
        {
            if (boxInteraction && Input.GetMouseButtonDown(0)) // Left mouse click
            {
                HandleBoxInteraction();
            }
            return; // Disable controls when controlsEnabled is false
        }

        if (IsPointerOverUIObject())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Time.time - lastSpaceTime < doubleSpaceThreshold)
            {
                ToggleFlyMode();
            }
            lastSpaceTime = Time.time;
        }

        if (isFlying)
        {
            Fly();
        }
        else
        {
            MovePlayer();
            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                Jump();
            }
        }

        MouseLook();
    }

    void ToggleFlyMode()
    {
        isFlying = !isFlying;
        rb.useGravity = !isFlying;
        rb.velocity = Vector3.zero; // Stop all movement when toggling fly mode
    }

    void Fly()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        float y = 0;

        if (Input.GetKey(KeyCode.Space)) // Ascend
        {
            y = 1;
        }
        else if (Input.GetKey(KeyCode.LeftShift)) // Descend
        {
            y = -1;
        }

        Vector3 move = transform.right * x + transform.up * y + transform.forward * z;
        transform.position += move * flySpeed * Time.deltaTime;
    }

    void MovePlayer()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        transform.position += move * speed * Time.deltaTime;
    }

    void MouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        isGrounded = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isFlying && collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    // Alternative method to check if the pointer is over a UI element
    private bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    void HandleBoxInteraction()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (hitObject.CompareTag("Box"))
            {
                Vector3Int position = FindPositionInMap(simController.positionToCubeMap, hitObject);
                if (position != Vector3Int.zero) // Check if a valid position was found
                {
                    // Fill in the initial position input fields
                    initialXInput.text = position.x.ToString();
                    initialYInput.text = position.y.ToString();
                    initialZInput.text = position.z.ToString();
                }
            }
            else if (hitObject.CompareTag("EmptyUnit"))
            {
                Vector3Int position = FindPositionInMap(simController.positionToEmptyMap, hitObject);
                if (position != Vector3Int.zero) // Check if a valid position was found
                {
                    // Fill in the destination position input fields
                    destinationXInput.text = position.x.ToString();
                    destinationYInput.text = position.y.ToString();
                    destinationZInput.text = position.z.ToString();
                }
            }
        }
    }

    Vector3Int FindPositionInMap(Dictionary<Vector3Int, GameObject> map, GameObject targetObject)
    {
        foreach (KeyValuePair<Vector3Int, GameObject> entry in map)
        {
            if (entry.Value == targetObject)
            {
                return entry.Key;
            }
        }
        return Vector3Int.zero; // Return a default value if not found
    }
}
