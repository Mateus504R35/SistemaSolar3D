using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FreeCameraController : MonoBehaviour
{
    [Header("Movimentação")]
    [SerializeField] private float movementSpeed = 8f;
    [SerializeField] private float fastMovementMultiplier = 4f;
    [SerializeField] private float precisionMovementMultiplier = 0.2f;

    [Tooltip("Menor velocidade selecionável pela roda do mouse.")]
    [SerializeField] private float minimumMovementSpeed = 0.25f;

    [Tooltip("Maior velocidade selecionável pela roda do mouse.")]
    [SerializeField] private float maximumMovementSpeed = 100f;

    [Tooltip("Quanto a roda do mouse altera a velocidade.")]
    [SerializeField] private float scrollSpeedFactor = 1.25f;

    [Tooltip("Faz Q/E utilizarem o eixo Y global, independentemente da rotação da câmera.")]
    [SerializeField] private bool useWorldUpForVerticalMovement = true;

    [Header("Limites de movimentação")]
    [Tooltip("Ativa a caixa invisível que limita a movimentação da câmera.")]
    [SerializeField] private bool useMovementLimits = true;

    [Tooltip("Centro da região em que a câmera pode se movimentar.")]
    [SerializeField] private Vector3 movementBoundsCenter = Vector3.zero;

    [Tooltip("Tamanho total da região permitida nos eixos X, Y e Z.")]
    [SerializeField]
    private Vector3 movementBoundsSize =
        new Vector3(900f, 400f, 900f);

    [Header("Rotação")]
    [SerializeField] private float mouseSensitivity = 1.5f;
    [SerializeField] private float rotationSmoothness = 20f;
    [SerializeField] private float minimumVerticalAngle = -85f;
    [SerializeField] private float maximumVerticalAngle = 85f;

    [Header("Acompanhamento de planetas")]
    [Tooltip("Adicione Mercúrio, Vênus, Terra etc. nessa ordem.")]
    [SerializeField] private Transform[] followTargets;

    [Tooltip("Olha para o planeta automaticamente quando ele é selecionado.")]
    [SerializeField] private bool lookAtTargetWhenSelected = true;

    private static readonly KeyCode[] targetKeys =
    {
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4,
        KeyCode.Alpha5,
        KeyCode.Alpha6,
        KeyCode.Alpha7,
        KeyCode.Alpha8,
        KeyCode.Alpha9
    };

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private float yaw;
    private float pitch;

    private int selectedTargetIndex = -1;
    private bool followingTarget;
    private Vector3 lastTargetPosition;

    [Header("Posicionamento ao selecionar planeta")]
    [Tooltip("Arraste o objeto Sol para este campo.")]
    [SerializeField] private Transform solarSystemCenter;

    [Tooltip("Teletransporta a câmera ao selecionar um planeta.")]
    [SerializeField] private bool teleportWhenSelected = true;

    [Tooltip("Aumenta a distância para enquadrar o planeta.")]
    [SerializeField] private float framingMultiplier = 1.5f;

    [Tooltip("Distância mínima entre a câmera e o planeta.")]
    [SerializeField] private float minimumTargetDistance = 2f;

    [Tooltip("Elevação da câmera proporcional ao tamanho do planeta.")]
    [SerializeField] private float verticalOffsetMultiplier = 0.15f;

    private Camera controlledCamera;
    private CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        controlledCamera = GetComponent<Camera>();

        // Garante que a posição inicial já esteja dentro da área permitida.
        if (useMovementLimits)
        {
            SetCameraTransformIgnoringCollision(
                ClampPositionToMovementBounds(transform.position),
                transform.rotation
            );
        }

        SaveInitialTransform();
        UpdateRotationValues();
        LockCursor();
    }

    private void Update()
    {
        HandleCommands();
        HandleSpeedAdjustment();
        HandleTargetSelection();
    }

    private void LateUpdate()
    {
        // Atualiza a representação física dos planetas depois que as órbitas
        // alteraram seus Transforms. É útil porque os planetas se movem.
        Physics.SyncTransforms();

        // LateUpdate ocorre depois da movimentação dos planetas,
        // diminuindo tremores durante o acompanhamento.
        HandleTargetFollowing();
        HandleMovement();
        HandleMouseLook();

        // Proteção adicional caso outro script altere a posição da câmera.
        EnforceMovementLimits();
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

        Vector3 verticalAxis = useWorldUpForVerticalMovement
            ? Vector3.up
            : transform.up;

        Vector3 movementDirection =
            transform.right * horizontal +
            transform.forward * forward +
            verticalAxis * vertical;

        // Garante a mesma velocidade para todas as direções
        // e evita aumento de velocidade na diagonal.
        movementDirection = Vector3.ClampMagnitude(
            movementDirection,
            1f
        );

        float currentSpeed = movementSpeed;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed *= fastMovementMultiplier;
        }

        if (Input.GetKey(KeyCode.LeftControl))
        {
            currentSpeed *= precisionMovementMultiplier;
        }

        Vector3 displacement =
            movementDirection * currentSpeed * Time.deltaTime;

        MoveCameraWithinBounds(displacement);
    }

    private void MoveCameraWithinBounds(Vector3 desiredDisplacement)
    {
        if (desiredDisplacement.sqrMagnitude <= 0.0000001f)
        {
            return;
        }

        Vector3 allowedDisplacement = desiredDisplacement;

        if (useMovementLimits)
        {
            Vector3 desiredPosition =
                transform.position + desiredDisplacement;

            Vector3 clampedPosition =
                ClampPositionToMovementBounds(desiredPosition);

            allowedDisplacement =
                clampedPosition - transform.position;
        }

        // Move() mantém as colisões do CharacterController com os planetas.
        characterController.Move(allowedDisplacement);
    }

    private Vector3 ClampPositionToMovementBounds(
        Vector3 desiredPosition)
    {
        Bounds movementBounds = new Bounds(
            movementBoundsCenter,
            movementBoundsSize
        );

        return new Vector3(
            Mathf.Clamp(
                desiredPosition.x,
                movementBounds.min.x,
                movementBounds.max.x
            ),
            Mathf.Clamp(
                desiredPosition.y,
                movementBounds.min.y,
                movementBounds.max.y
            ),
            Mathf.Clamp(
                desiredPosition.z,
                movementBounds.min.z,
                movementBounds.max.z
            )
        );
    }

    private void EnforceMovementLimits()
    {
        if (!useMovementLimits)
        {
            return;
        }

        Vector3 clampedPosition =
            ClampPositionToMovementBounds(transform.position);

        if ((clampedPosition - transform.position).sqrMagnitude <=
            0.000001f)
        {
            return;
        }

        SetCameraTransformIgnoringCollision(
            clampedPosition,
            transform.rotation
        );
    }

    private void HandleSpeedAdjustment()
    {
        float scroll = Input.mouseScrollDelta.y;

        if (Mathf.Abs(scroll) < 0.01f)
        {
            return;
        }

        movementSpeed *= Mathf.Pow(
            scrollSpeedFactor,
            scroll
        );

        movementSpeed = Mathf.Clamp(
            movementSpeed,
            minimumMovementSpeed,
            maximumMovementSpeed
        );
    }

    private void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        float mouseX =
            Input.GetAxisRaw("Mouse X") * mouseSensitivity;

        float mouseY =
            Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

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

        if (rotationSmoothness <= 0f)
        {
            transform.rotation = targetRotation;
            return;
        }

        float interpolation =
            1f - Mathf.Exp(
                -rotationSmoothness * Time.deltaTime
            );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            interpolation
        );
    }

    private void HandleTargetSelection()
    {
        if (followTargets == null)
        {
            return;
        }

        int maximumTargets = Mathf.Min(
            followTargets.Length,
            targetKeys.Length
        );

        for (int index = 0; index < maximumTargets; index++)
        {
            if (Input.GetKeyDown(targetKeys[index]))
            {
                SelectTarget(index);
                return;
            }
        }
    }

    private void SelectTarget(int index)
    {
        if (followTargets == null ||
            index < 0 ||
            index >= followTargets.Length ||
            followTargets[index] == null)
        {
            return;
        }

        selectedTargetIndex = index;

        Transform target = GetSelectedTarget();

        if (teleportWhenSelected)
        {
            TeleportToSelectedTarget();
        }
        else if (lookAtTargetWhenSelected)
        {
            LookAtSelectedTarget();
        }

        followingTarget = true;
        lastTargetPosition = target.position;

        Debug.Log($"Acompanhando: {target.name}");
    }

    private void HandleTargetFollowing()
    {
        if (!followingTarget)
        {
            return;
        }

        Transform target = GetSelectedTarget();

        if (target == null)
        {
            followingTarget = false;
            return;
        }

        // A câmera recebe o deslocamento realizado pelo planeta,
        // respeitando tanto as colisões quanto os limites da cena.
        Vector3 targetDisplacement =
            target.position - lastTargetPosition;

        MoveCameraWithinBounds(targetDisplacement);
        lastTargetPosition = target.position;
    }

    private void ToggleTargetFollowing()
    {
        Transform target = GetSelectedTarget();

        if (target == null)
        {
            Debug.LogWarning(
                "Nenhum planeta foi selecionado."
            );

            return;
        }

        followingTarget = !followingTarget;
        lastTargetPosition = target.position;

        Debug.Log(
            followingTarget
                ? $"Acompanhamento ativado: {target.name}"
                : $"Acompanhamento desativado: {target.name}"
        );
    }

    private void LookAtSelectedTarget()
    {
        Transform target = GetSelectedTarget();

        if (target == null)
        {
            return;
        }

        Vector3 direction =
            target.position - transform.position;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(
            direction.normalized,
            Vector3.up
        );

        UpdateRotationValues();
    }

    private Transform GetSelectedTarget()
    {
        if (followTargets == null ||
            selectedTargetIndex < 0 ||
            selectedTargetIndex >= followTargets.Length)
        {
            return null;
        }

        return followTargets[selectedTargetIndex];
    }

    private void HandleCommands()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCamera();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleTargetFollowing();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            LookAtSelectedTarget();
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
        SetCameraTransformIgnoringCollision(
            initialPosition,
            initialRotation
        );

        followingTarget = false;
        selectedTargetIndex = -1;

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

    private void TeleportToSelectedTarget()
    {
        Transform target = GetSelectedTarget();

        if (target == null)
        {
            return;
        }

        Bounds targetBounds = CalculateTargetBounds(target);

        Vector3 focusPoint = targetBounds.center;

        float targetRadius = Mathf.Max(
            targetBounds.extents.x,
            targetBounds.extents.y,
            targetBounds.extents.z
        );

        targetRadius = Mathf.Max(targetRadius, 0.5f);

        float verticalFieldOfView = controlledCamera != null
            ? controlledCamera.fieldOfView
            : 60f;

        float halfFieldOfViewRadians =
            verticalFieldOfView * 0.5f * Mathf.Deg2Rad;

        // Calcula a distância necessária para enquadrar o planeta.
        float targetDistance =
            targetRadius / Mathf.Tan(halfFieldOfViewRadians);

        targetDistance *= framingMultiplier;

        targetDistance = Mathf.Max(
            targetDistance,
            minimumTargetDistance
        );

        // Mantém a cápsula da câmera completamente fora da superfície.
        targetDistance +=
            characterController.radius +
            characterController.skinWidth;

        Vector3 cameraDirection;

        if (solarSystemCenter != null)
        {
            // Coloca a câmera no lado externo da órbita,
            // olhando em direção ao planeta e ao centro do sistema.
            cameraDirection =
                focusPoint - solarSystemCenter.position;

            if (cameraDirection.sqrMagnitude < 0.0001f)
            {
                cameraDirection = Vector3.back;
            }

            cameraDirection.Normalize();
        }
        else
        {
            // Direção alternativa caso o Sol não tenha sido configurado.
            cameraDirection = -Vector3.forward;
        }

        float verticalOffset =
            targetRadius * verticalOffsetMultiplier;

        Vector3 newCameraPosition =
            focusPoint +
            cameraDirection * targetDistance +
            Vector3.up * verticalOffset;

        Vector3 lookDirection =
            focusPoint - newCameraPosition;

        Quaternion newCameraRotation =
            Quaternion.LookRotation(
                lookDirection.normalized,
                Vector3.up
            );

        SetCameraTransformIgnoringCollision(
            newCameraPosition,
            newCameraRotation
        );

        UpdateRotationValues();

        // Impede um deslocamento inesperado no primeiro frame.
        lastTargetPosition = target.position;
    }

    private void SetCameraTransformIgnoringCollision(
        Vector3 newPosition,
        Quaternion newRotation)
    {
        if (useMovementLimits)
        {
            newPosition =
                ClampPositionToMovementBounds(newPosition);
        }

        // O CharacterController não deve permanecer ativo durante
        // teletransportes ou resets, pois isso pode causar reposicionamentos.
        bool wasEnabled = characterController.enabled;

        if (wasEnabled)
        {
            characterController.enabled = false;
        }

        transform.SetPositionAndRotation(
            newPosition,
            newRotation
        );

        if (wasEnabled)
        {
            characterController.enabled = true;
        }

        Physics.SyncTransforms();
    }

    private Bounds CalculateTargetBounds(Transform target)
    {
        Renderer[] renderers =
            target.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            return new Bounds(
                target.position,
                Vector3.one
            );
        }

        Bounds combinedBounds = renderers[0].bounds;

        for (int index = 1; index < renderers.Length; index++)
        {
            combinedBounds.Encapsulate(
                renderers[index].bounds
            );
        }

        return combinedBounds;
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

    private void OnValidate()
    {
        movementBoundsSize.x = Mathf.Max(
            0.1f,
            Mathf.Abs(movementBoundsSize.x)
        );

        movementBoundsSize.y = Mathf.Max(
            0.1f,
            Mathf.Abs(movementBoundsSize.y)
        );

        movementBoundsSize.z = Mathf.Max(
            0.1f,
            Mathf.Abs(movementBoundsSize.z)
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (!useMovementLimits)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(
            movementBoundsCenter,
            movementBoundsSize
        );
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public bool IsFollowingTarget
    {
        get { return followingTarget; }
    }

    public Transform SelectedTarget
    {
        get { return GetSelectedTarget(); }
    }

    public string SelectedTargetName
    {
        get
        {
            Transform target = GetSelectedTarget();

            return target != null
                ? target.name
                : "Nenhum";
        }
    }

    public string CurrentModeName
    {
        get
        {
            string navigationMode = followingTarget
                ? "Acompanhamento"
                : "Livre";

            string movementMode = "Normal";

            if (Input.GetKey(KeyCode.LeftControl))
            {
                movementMode = "Precisão";
            }
            else if (Input.GetKey(KeyCode.LeftShift))
            {
                movementMode = "Rápido";
            }

            return $"{navigationMode} | {movementMode}";
        }
    }

}