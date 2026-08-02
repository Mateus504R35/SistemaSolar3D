using UnityEngine;

/// <summary>
/// Demonstra o pipeline completo:
/// ponto_clip = P * V * M * ponto_local
///
/// Use em um objeto que possua ExplicitModelMatrix e associe os três
/// componentes no Inspector.
/// </summary>
[DisallowMultipleComponent]
public class ExplicitMvpDebugger : MonoBehaviour
{
    [SerializeField] private ExplicitModelMatrix modelSource;
    [SerializeField] private ExplicitViewMatrix viewSource;
    [SerializeField] private ExplicitPerspectiveProjection projectionSource;
    [SerializeField] private Vector3 localPoint = Vector3.zero;

    public Matrix4x4 MvpMatrix
    {
        get
        {
            if (modelSource == null || viewSource == null || projectionSource == null)
            {
                return Matrix4x4.identity;
            }

            return
                projectionSource.ProjectionMatrix *
                viewSource.ViewMatrix *
                modelSource.ModelMatrix;
        }
    }

    private void Reset()
    {
        modelSource = GetComponent<ExplicitModelMatrix>();
    }

    [ContextMenu("Imprimir transformação MVP do ponto")]
    private void PrintMvpTransformation()
    {
        if (modelSource == null || viewSource == null || projectionSource == null)
        {
            Debug.LogError(
                "Associe Model Source, View Source e Projection Source no Inspector.",
                this
            );
            return;
        }

        modelSource.RecalculateModelMatrix();
        viewSource.ApplyViewMatrix();

        Vector4 local = new Vector4(
            localPoint.x,
            localPoint.y,
            localPoint.z,
            1f
        );

        Vector4 world = modelSource.ModelMatrix * local;
        Vector4 camera = viewSource.ViewMatrix * world;
        Vector4 clip = projectionSource.ProjectionMatrix * camera;

        Vector3 normalizedDeviceCoordinates = Vector3.zero;

        if (Mathf.Abs(clip.w) > 0.000001f)
        {
            normalizedDeviceCoordinates = new Vector3(
                clip.x / clip.w,
                clip.y / clip.w,
                clip.z / clip.w
            );
        }

        Debug.Log(
            $"Pipeline MVP de '{name}':\n" +
            $"Local:  {local}\n" +
            $"Mundo:  {world}\n" +
            $"Câmera: {camera}\n" +
            $"Clip:   {clip}\n" +
            $"NDC:    {normalizedDeviceCoordinates}",
            this
        );
    }
}
