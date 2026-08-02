using UnityEngine;

/// <summary>
/// Constrói explicitamente a matriz Model do objeto:
/// M_local = T * R * S
/// M_world = M_parent * M_local
///
/// A recursão inclui todos os pais. Portanto, em um planeta,
/// a rotação do pivô OrbitaPlaneta também participa da matriz final.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class ExplicitModelMatrix : MonoBehaviour
{
    [Header("Depuração")]
    [SerializeField] private bool drawLocalAxes = false;
    [SerializeField, Min(0.01f)] private float axisLength = 2f;

    public Matrix4x4 ModelMatrix { get; private set; } = Matrix4x4.identity;

    private void OnEnable()
    {
        RecalculateModelMatrix();
    }

    private void LateUpdate()
    {
        // LateUpdate garante que órbita e rotação própria já tenham sido
        // atualizadas pelos outros scripts no frame atual.
        RecalculateModelMatrix();
    }

    private void OnValidate()
    {
        axisLength = Mathf.Max(0.01f, axisLength);
        RecalculateModelMatrix();
    }

    public void RecalculateModelMatrix()
    {
        ModelMatrix = BuildWorldMatrixExplicitly(transform);
    }

    /// <summary>
    /// Constrói a matriz mundial sem ler transform.localToWorldMatrix.
    /// </summary>
    private static Matrix4x4 BuildWorldMatrixExplicitly(Transform current)
    {
        Matrix4x4 translationMatrix =
            Matrix4x4.Translate(current.localPosition);

        Matrix4x4 rotationMatrix =
            Matrix4x4.Rotate(current.localRotation);

        Matrix4x4 scaleMatrix =
            Matrix4x4.Scale(current.localScale);

        // Como os vetores são multiplicados à direita, a ordem efetiva é:
        // escala -> rotação -> translação.
        Matrix4x4 localMatrix =
            translationMatrix * rotationMatrix * scaleMatrix;

        if (current.parent == null)
        {
            return localMatrix;
        }

        Matrix4x4 parentWorldMatrix =
            BuildWorldMatrixExplicitly(current.parent);

        return parentWorldMatrix * localMatrix;
    }

    /// <summary>
    /// Transforma um ponto do espaço local para o espaço do mundo usando
    /// a matriz Model construída por este script.
    /// </summary>
    public Vector3 TransformPointToWorld(Vector3 localPoint)
    {
        return ModelMatrix.MultiplyPoint3x4(localPoint);
    }

    /// <summary>
    /// Transforma uma direção. A translação não afeta direções.
    /// </summary>
    public Vector3 TransformDirectionToWorld(Vector3 localDirection)
    {
        return ModelMatrix.MultiplyVector(localDirection);
    }

    [ContextMenu("Imprimir matriz Model")]
    private void PrintModelMatrix()
    {
        RecalculateModelMatrix();

        float difference = MaximumAbsoluteDifference(
            ModelMatrix,
            transform.localToWorldMatrix
        );

        Debug.Log(
            $"Matriz Model explícita de '{name}':\n" +
            FormatMatrix(ModelMatrix) +
            $"\nMaior diferença para localToWorldMatrix do Unity: {difference:E6}",
            this
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawLocalAxes)
        {
            return;
        }

        RecalculateModelMatrix();

        Vector3 origin = ModelMatrix.MultiplyPoint3x4(Vector3.zero);
        Vector3 x = ModelMatrix.MultiplyPoint3x4(Vector3.right * axisLength);
        Vector3 y = ModelMatrix.MultiplyPoint3x4(Vector3.up * axisLength);
        Vector3 z = ModelMatrix.MultiplyPoint3x4(Vector3.forward * axisLength);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, x);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(origin, y);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(origin, z);
    }

    private static float MaximumAbsoluteDifference(Matrix4x4 a, Matrix4x4 b)
    {
        float maximum = 0f;

        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 4; column++)
            {
                maximum = Mathf.Max(
                    maximum,
                    Mathf.Abs(a[row, column] - b[row, column])
                );
            }
        }

        return maximum;
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
