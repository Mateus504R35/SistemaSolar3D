using TMPro;
using UnityEngine;

public class CameraHUD : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField]
    private FreeCameraController cameraController;

    [SerializeField]
    private TMP_Text coordinatesText;

    [SerializeField]
    private TMP_Text modeText;

    [SerializeField]
    private TMP_Text selectedPlanetText;

    [Header("Configuração")]
    [SerializeField]
    [Range(0, 3)]
    private int coordinateDecimalPlaces = 1;

    private void Awake()
    {
        if (cameraController == null)
        {
            cameraController =
                FindObjectOfType<FreeCameraController>();
        }
    }

    private void LateUpdate()
    {
        if (cameraController == null)
        {
            ShowUnavailableInformation();
            return;
        }

        UpdateCoordinates();
        UpdateMode();
        UpdateSelectedPlanet();
    }

    private void UpdateCoordinates()
    {
        if (coordinatesText == null)
        {
            return;
        }

        Vector3 position =
            cameraController.transform.position;

        string format =
            "F" + coordinateDecimalPlaces;

        string x =
            position.x.ToString(format);

        string y =
            position.y.ToString(format);

        string z =
            position.z.ToString(format);

        coordinatesText.text =
            $"Coordenadas: X {x} | Y {y} | Z {z}";
    }

    private void UpdateMode()
    {
        if (modeText == null)
        {
            return;
        }

        modeText.text =
            $"Modo: {cameraController.CurrentModeName}";
    }

    private void UpdateSelectedPlanet()
    {
        if (selectedPlanetText == null)
        {
            return;
        }

        selectedPlanetText.text =
            $"Planeta selecionado: " +
            cameraController.SelectedTargetName;
    }

    private void ShowUnavailableInformation()
    {
        if (coordinatesText != null)
        {
            coordinatesText.text =
                "Coordenadas: indisponíveis";
        }

        if (modeText != null)
        {
            modeText.text =
                "Modo: indisponível";
        }

        if (selectedPlanetText != null)
        {
            selectedPlanetText.text =
                "Planeta selecionado: nenhum";
        }
    }
}