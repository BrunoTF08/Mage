using UnityEngine;

[DefaultExecutionOrder(100)]
public class ShoulderCameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private Vector3 shoulderOffset = new Vector3(0.6f, 1.55f, 0f);
    [SerializeField] private float distance = 3.25f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 2.2f;
    [SerializeField] private float startingPitch = 12f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 65f;

    [Header("Follow")]
    [SerializeField] private float positionSharpness = 24f;
    [SerializeField] private float rotationSharpness = 28f;
    [SerializeField] private bool rotatePlayerWithCamera = true;
    [SerializeField] private bool lockCursor = true;

    private float yaw;
    private float pitch;
    private bool initialized;

    public Transform Player
    {
        get => player;
        set
        {
            player = value;
            initialized = false;
        }
    }

    private void Start()
    {
        if (player == null)
        {
            PlayerController playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                player = playerController.transform;
            }
        }

        InitializeAngles();
        ApplyCursorState();
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            return;
        }

        if (!initialized)
        {
            InitializeAngles();
        }

        HandleCursorToggle();
        ReadLookInput();
        FollowTarget(Time.deltaTime);
    }

    public void SnapToTarget()
    {
        if (player == null)
        {
            return;
        }

        if (!initialized)
        {
            InitializeAngles();
        }

        Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivotPosition = player.position + yawRotation * shoulderOffset;
        Vector3 cameraPosition = pivotPosition - cameraRotation * Vector3.forward * distance;

        transform.SetPositionAndRotation(cameraPosition, cameraRotation);
    }

    private void InitializeAngles()
    {
        if (player != null)
        {
            yaw = player.eulerAngles.y;
        }
        else
        {
            yaw = transform.eulerAngles.y;
        }

        pitch = Mathf.Clamp(startingPitch, minPitch, maxPitch);
        initialized = true;
    }

    private void ApplyCursorState()
    {
        if (!lockCursor)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void HandleCursorToggle()
    {
        if (!lockCursor)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            ApplyCursorState();
        }
    }

    private void ReadLookInput()
    {
        if (lockCursor && Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        yaw += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void FollowTarget(float deltaTime)
    {
        Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivotPosition = player.position + yawRotation * shoulderOffset;
        Vector3 cameraPosition = pivotPosition - cameraRotation * Vector3.forward * distance;

        float positionBlend = 1f - Mathf.Exp(-positionSharpness * deltaTime);
        float rotationBlend = 1f - Mathf.Exp(-rotationSharpness * deltaTime);

        transform.position = Vector3.Lerp(transform.position, cameraPosition, positionBlend);
        transform.rotation = Quaternion.Slerp(transform.rotation, cameraRotation, rotationBlend);

        if (rotatePlayerWithCamera)
        {
            Quaternion playerRotation = Quaternion.Euler(0f, yaw, 0f);
            player.rotation = Quaternion.Slerp(player.rotation, playerRotation, rotationBlend);
        }
    }
}
