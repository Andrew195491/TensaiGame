using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Alterna entre la vista principal y la del minimapa SOLO cuando se hace clic
/// DENTRO del RawImage del minimapa. Ignora cualquier clic en otras partes del Canvas.
/// </summary>
[RequireComponent(typeof(GraphicRaycaster))]
public class CameraSwitcher_U : MonoBehaviour, IPointerClickHandler
{
    [Header("Cámaras")]
    [Tooltip("Cámara principal del juego.")]
    public Camera mainCamera;

    [Tooltip("Cámara del minimapa.")]
    public Camera minimapCamera;

    [Header("Renderización UI")]
    [Tooltip("Imagen UI que muestra el minimapa (el área clicable).")]
    public RawImage minimapImage;

    [Header("Texturas de Renderizado")]
    [Tooltip("Textura usada por la cámara principal cuando no está en pantalla completa.")]
    public RenderTexture mainCameraTexture;

    [Tooltip("Textura usada por la cámara del minimapa cuando está en vista pequeña.")]
    public RenderTexture minimapTexture;

    private bool isMinimapFullView = false;

    void Awake()
    {
        if (minimapImage != null)
        {
            // MUY IMPORTANTE: solo el RawImage del minimapa debe ser raycasteable.
            minimapImage.raycastTarget = true;
        }
    }

    /// <summary>
    /// Recibe el clic del sistema de eventos. Solo alterna si el clic fue sobre el RawImage del minimapa.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!ClickedInsideMinimap(eventData)) return;
        ToggleCameraView();
    }

    /// <summary>
    /// Verifica con precisión si el clic cayó DENTRO del rect del minimapa.
    /// </summary>
    private bool ClickedInsideMinimap(PointerEventData eventData)
    {
        if (minimapImage == null) return false;

        // 1) El objeto bajo el puntero debe ser EXACTAMENTE el RawImage del minimapa
        if (eventData.pointerEnter != minimapImage.gameObject)
        {
            // Como a veces hay un "Mask" o "Border", hacemos una comprobación geométrica también:
            return RectTransformUtility.RectangleContainsScreenPoint(
                minimapImage.rectTransform,
                eventData.position,
                eventData.pressEventCamera
            );
        }

        // 2) Verificación geométrica extra por seguridad
        return RectTransformUtility.RectangleContainsScreenPoint(
            minimapImage.rectTransform,
            eventData.position,
            eventData.pressEventCamera
        );
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
            // Minimapa a pantalla completa
            if (minimapCamera) minimapCamera.targetTexture = null;           // pantalla
            if (mainCamera)    mainCamera.targetTexture    = mainCameraTexture; // a textura
            if (minimapImage)  minimapImage.texture        = mainCameraTexture;

            if (mainCamera)    mainCamera.enabled = false;
            if (minimapCamera) minimapCamera.enabled = true;
        }
        else
        {
            // Vista normal
            if (mainCamera)    mainCamera.targetTexture    = null;           // pantalla
            if (minimapCamera) minimapCamera.targetTexture = minimapTexture; // a textura
            if (minimapImage)  minimapImage.texture        = minimapTexture;

            if (mainCamera)    mainCamera.enabled = true;
            if (minimapCamera) minimapCamera.enabled = true;
        }
    }

    /// <summary>
    /// Permite forzar la vista deseada desde otros scripts si lo necesitas.
    /// </summary>
    public void SetFullView(bool minimapFull)
    {
        if (isMinimapFullView == minimapFull) return;
        ToggleCameraView();
    }
}
