using UnityEngine;

/// <summary>
/// Aplica rotação local ao corpo celeste em torno de seu próprio eixo.
/// </summary>
public class SelfRotation : MonoBehaviour
{
    [Header("Rotação própria")]

    [SerializeField]
    private Vector3 rotationAxis = Vector3.up;

    [SerializeField]
    private float rotationSpeed = 30f;

    [SerializeField]
    private bool rotationEnabled = true;

    private void Update()
    {
        if (!rotationEnabled || rotationAxis.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float angle = rotationSpeed * Time.deltaTime;

        transform.Rotate(
            rotationAxis.normalized,
            angle,
            Space.Self
        );
    }

    public void SetRotationEnabled(bool enabled)
    {
        rotationEnabled = enabled;
    }
}