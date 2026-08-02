using UnityEngine;

/// <summary>
/// Constrói e aplica explicitamente a matriz View da câmera:
/// V = S_z * R^-1 * T^-1
///
/// T^-1 leva o mundo para a origem da câmera.
/// R^-1 desfaz a rotação mundial da câmera.
/// S_z converte o eixo +Z de frente dos Transforms do Unity para
/// a convenção de câmera em que a frente corresponde a -Z.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(1000)]
public class ExplicitViewMatrix : MonoBehaviour
{
    [Header("Aplicação")]
    [SerializeField]
    private bool applyMatrixToCamera = true;

    public Matrix4x4 ViewMatrix { get; private set; } = Matrix4x4.identity;

    private Camera controlledCamera;

    private void Awake()
    {
        controlledCamera = GetComponent<Camera>();
        ApplyViewMatrix();
    }

    private void OnEnable()
    {
        if (controlledCamera == null)
        {
            controlledCamera = GetComponent<Camera>();
        }

        ApplyViewMatrix();
    }

    private void LateUpdate()
    {
        // O FreeCameraController altera posição e rotação em Update.
        // Aqui usamos os valores finais do frame para reconstruir a View.
        ApplyViewMatrix();
    }

    private void OnValidate()
    {
        if (controlledCamera == null)
        {
            controlledCamera = GetComponent<Camera>();
        }

        ApplyViewMatrix();
    }

    public void ApplyViewMatrix()
    {
        if (controlledCamera == null)
        {
            return;
        }

        Vector3 cameraPosition = transform.position;
        Quaternion cameraRotation = transform.rotation;

        Matrix4x4 inverseTranslationMatrix =
            Matrix4x4.Translate(-cameraPosition);

        Matrix4x4 inverseRotationMatrix =
            Matrix4x4.Rotate(Quaternion.Inverse(cameraRotation));

        Matrix4x4 cameraConventionMatrix =
            Matrix4x4.Scale(new Vector3(1f, 1f, -1f));

        ViewMatrix =
            cameraConventionMatrix *
            inverseRotationMatrix *
            inverseTranslationMatrix;

        if (applyMatrixToCamera)
        {
            controlledCamera.worldToCameraMatrix = ViewMatrix;
        }
        else
        {
            controlledCamera.ResetWorldToCameraMatrix();
        }
    }

    /// <summary>
    /// Transforma um ponto do mundo para o espaço da câmera.
    /// </summary>
    public Vector3 TransformWorldPointToCamera(Vector3 worldPoint)
    {
        return ViewMatrix.MultiplyPoint3x4(worldPoint);
    }

    [ContextMenu("Imprimir matriz View")]
    private void PrintViewMatrix()
    {
        ApplyViewMatrix();

        Debug.Log(
            $"Matriz View explícita da câmera '{name}':\n" +
            FormatMatrix(ViewMatrix),
            this
        );
    }

    private void OnDisable()
    {
        if (controlledCamera != null)
        {
            controlledCamera.ResetWorldToCameraMatrix();
        }
    }

    private static string FormatMatrix(Matrix4x4 matrix)
    {
        return
            $"[{matrix.m00,10:F4} {matrix.m01,10:F4} {matrix.m02,10:F4} {matrix.m03,10:F4}]\n" +
            $"[{matrix.m10,10:F4} {matrix.m11,10:F4} {matrix.m12,10:F4} {matrix.m13,10:F4}]\n" +
            $"[{matrix.m20,10:F4} {matrix.m21,10:F4} {matrix.m22,10:F4} {matrix.m23,10:F4}]\n" +
            $"[{matrix.m30,10:F4} {matrix.m31,10:F4} {matrix.m32,10:F4} {matrix.m33,10:F4}]";
    }
}
