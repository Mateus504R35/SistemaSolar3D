using UnityEngine;

public class FreeCameraController : MonoBehaviour
{
    [Header("Movimentação")]
    [SerializeField] private float movementSpeed = 10f;
    [SerializeField] private float fastMovementMultiplier = 3f;
    [SerializeField] private float movementSmoothness = 8f;

    [Header("Rotação")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float rotationSmoothness = 15f;
    [SerializeField] private float minimumVerticalAngle = -85f;
    [SerializeField] private float maximumVerticalAngle = 85f;

    private Vector3 currentVelocity;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private float yaw;
    private float pitch;

    private void Start()
    {
        SaveInitialTransform();
        UpdateRotationValues();
        LockCursor();
    }

    private void Update()
    {
        HandleMovement();
        HandleMouseLook();
        HandleCommands();
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float forward = Input.GetAxisRaw("Vertical");

        float vertical = 0f;

        if (Input.GetKey(KeyCode.E))
        {
            vertical += 1f;
        }

        if (Input.GetKey(KeyCode.Q))
        {
            vertical -= 1f;
        }

        Vector3 localDirection = new Vector3(
            horizontal,
            vertical,
            forward
        );

        // Impede que a câmera fique mais rápida na diagonal.
        if (localDirection.sqrMagnitude > 1f)
        {
            localDirection.Normalize();
        }

        float currentSpeed = movementSpeed;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed *= fastMovementMultiplier;
        }

        // Converte a direção local da câmera para o espaço do mundo.
        Vector3 worldDirection =
            transform.right * localDirection.x +
            transform.up * localDirection.y +
            transform.forward * localDirection.z;

        Vector3 targetVelocity = worldDirection * currentSpeed;

        float interpolation =
            1f - Mathf.Exp(-movementSmoothness * Time.deltaTime);

        currentVelocity = Vector3.Lerp(
            currentVelocity,
            targetVelocity,
            interpolation
        );

        transform.position += currentVelocity * Time.deltaTime;
    }

    private void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        float mouseX =
            Input.GetAxis("Mouse X") * mouseSensitivity;

        float mouseY =
            Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;

        pitch = Mathf.Clamp(
            pitch,
            minimumVerticalAngle,
            maximumVerticalAngle
        );

        Quaternion targetRotation = Quaternion.Euler(
            pitch,
            yaw,
            0f
        );

        float interpolation =
            1f - Mathf.Exp(-rotationSmoothness * Time.deltaTime);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            interpolation
        );
    }

    private void HandleCommands()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCamera();
        }

        if (Input.GetMouseButtonDown(0) &&
            Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitApplication();
        }
    }

    private void ResetCamera()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        currentVelocity = Vector3.zero;

        UpdateRotationValues();
    }

    private void SaveInitialTransform()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    private void UpdateRotationValues()
    {
        Vector3 currentAngles = transform.eulerAngles;

        yaw = currentAngles.y;
        pitch = NormalizeAngle(currentAngles.x);
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ExitApplication()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}