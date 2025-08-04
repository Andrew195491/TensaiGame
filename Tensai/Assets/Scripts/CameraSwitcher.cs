using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CameraSwitcher : MonoBehaviour, IPointerClickHandler
{
    public Camera mainCamera;
    public Camera minimapCamera;
    public RawImage minimapImage;

    public RenderTexture mainCameraTexture;
    public RenderTexture minimapTexture;

    private bool isMinimapFullView = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        ToggleCameraView();
    }

    private void ToggleCameraView()
    {
        isMinimapFullView = !isMinimapFullView;

        if (isMinimapFullView)
        {
            // Mostrar el minimapa en pantalla completa
            minimapCamera.targetTexture = null; // Vista libre
            mainCamera.targetTexture = mainCameraTexture;
            minimapImage.texture = mainCameraTexture;

            mainCamera.enabled = false;
            minimapCamera.enabled = true;
        }
        else
        {
            // Volver a vista normal con minimapa pequeño
            mainCamera.targetTexture = null;
            minimapCamera.targetTexture = minimapTexture;
            minimapImage.texture = minimapTexture;

            mainCamera.enabled = true;
            minimapCamera.enabled = true;
        }
    }
}
