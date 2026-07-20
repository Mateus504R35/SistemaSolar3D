using UnityEngine;

/// <summary>
/// Rotaciona o pivô orbital, fazendo os objetos filhos
/// se moverem ao redor do centro da órbita.
/// </summary>
public class OrbitMotion : MonoBehaviour
{
    [Header("Configuração da órbita")]

    [SerializeField]
    private Vector3 orbitAxis = Vector3.up;

    [SerializeField]
    private float orbitSpeed = 10f;

    [SerializeField]
    private bool orbitEnabled = true;

    private void Update()
    {
        if (!orbitEnabled || orbitAxis.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float angle = orbitSpeed * Time.deltaTime;

        transform.Rotate(
            orbitAxis.normalized,
            angle,
            Space.Self
        );
    }

    public void SetOrbitEnabled(bool enabled)
    {
        orbitEnabled = enabled;
    }

    public void SetOrbitSpeed(float newSpeed)
    {
        orbitSpeed = newSpeed;
    }
}