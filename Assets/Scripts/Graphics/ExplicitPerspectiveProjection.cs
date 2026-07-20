using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ExplicitPerspectiveProjection : MonoBehaviour
{
    [Header("Parâmetros da projeção perspectiva")]

    [SerializeField]
    [Range(20f, 120f)]
    private float verticalFieldOfView = 60f;

    [SerializeField]
    private float nearPlane = 0.1f;

    [SerializeField]
    private float farPlane = 500f;

    private Camera controlledCamera;

    public Matrix4x4 ProjectionMatrix =>
        controlledCamera.projectionMatrix;

    private void Awake()
    {
        controlledCamera = GetComponent<Camera>();
        ApplyProjectionMatrix();
    }

    private void LateUpdate()
    {
        ApplyProjectionMatrix();
    }

    private void OnValidate()
    {
        if (controlledCamera == null)
        {
            controlledCamera = GetComponent<Camera>();
        }

        ApplyProjectionMatrix();
    }

    private void ApplyProjectionMatrix()
    {
        if (controlledCamera == null)
        {
            return;
        }

        nearPlane = Mathf.Max(0.01f, nearPlane);
        farPlane = Mathf.Max(nearPlane + 0.01f, farPlane);

        float aspectRatio = controlledCamera.aspect;

        Matrix4x4 perspectiveMatrix =
            Matrix4x4.Perspective(
                verticalFieldOfView,
                aspectRatio,
                nearPlane,
                farPlane
            );

        controlledCamera.nearClipPlane = nearPlane;
        controlledCamera.farClipPlane = farPlane;

        controlledCamera.projectionMatrix =
            perspectiveMatrix;
    }

    private void OnDisable()
    {
        if (controlledCamera != null)
        {
            controlledCamera.ResetProjectionMatrix();
        }
    }
}