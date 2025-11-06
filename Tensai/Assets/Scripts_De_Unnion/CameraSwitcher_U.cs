using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Permite alternar entre la vista principal del juego y el minimapa en pantalla completa.
/// Al hacer clic sobre el minimapa, las cámaras intercambian sus vistas.
/// </summary>
public class CameraSwitcher_U : MonoBehaviour, IPointerClickHandler
{
    [Header("Cámaras")]
    [Tooltip("Cámara principal del juego.")]
    public Camera mainCamera;

    [Tooltip("Cámara del minimapa.")]
    public Camera minimapCamera;

    [Header("Renderización UI")]
    [Tooltip("Imagen UI que muestra el minimapa.")]
    public RawImage minimapImage;

    [Header("Texturas de Renderizado")]
    [Tooltip("Textura usada por la cámara principal cuando no está en pantalla completa.")]
    public RenderTexture mainCameraTexture;

    [Tooltip("Textura usada por la cámara del minimapa cuando está en vista pequeña.")]
    public RenderTexture minimapTexture;

    // Indica si el minimapa está mostrando la vista completa
    private bool isMinimapFullView = false;

    /// <summary>
    /// Detecta el clic en la UI y alterna la vista entre las cámaras.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        ToggleCameraView();
    }

    /// <summary>
    /// Alterna entre:
    /// - Vista normal: cámara principal grande, minimapa pequeño.
    /// - Vista invertida: minimapa grande, cámara principal pequeña.
    /// </summary>
    private void ToggleCameraView()
    {
        isMinimapFullView = !isMinimapFullView;

        if (isMinimapFullView)
        {
            // ============================
            // MODO: MINIMAPA PANTALLA COMPLETA
            // ============================
            minimapCamera.targetTexture = null;           // Renderiza a pantalla completa
            mainCamera.targetTexture = mainCameraTexture; // Cámara principal pasa a textura
            minimapImage.texture = mainCameraTexture;     // Muestra la cámara principal en la UI

            mainCamera.enabled = false;
            minimapCamera.enabled = true;
        }
        else
        {
            // ============================
            // MODO: VISTA NORMAL
            // ============================
            mainCamera.targetTexture = null;              // Renderiza a pantalla completa
            minimapCamera.targetTexture = minimapTexture; // Minimapa en su textura
            minimapImage.texture = minimapTexture;        // Muestra el minimapa en la UI

            mainCamera.enabled = true;
            minimapCamera.enabled = true;
        }
    }
}
