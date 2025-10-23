using UnityEngine; // Necesario para MonoBehaviour
using UnityEngine.UI; // Necesario para RawImage
using UnityEngine.EventSystems; // Necesario para IPointerClickHandler (iPointerClickHandler es una interfaz para detectar clics)

/// <summary>
/// Clase que permite alternar entre la vista principal y el minimapa en pantalla completa.
/// Implementa IPointerClickHandler para detectar clics del usuario en la UI.
/// </summary>
public class CameraSwitcher : MonoBehaviour, IPointerClickHandler
{
    // ============================================
    // SECCIÓN 1: DECLARACIÓN DE VARIABLES
    // ============================================
    
    // Cámara principal que muestra la vista normal del juego
    public Camera mainCamera;
    
    // Cámara del minimapa que muestra una vista aérea del escenario
    public Camera minimapCamera;
    
    // Imagen UI donde se renderiza el minimapa en la esquina de la pantalla
    public RawImage minimapImage;

    // Textura de renderizado para la cámara principal (cuando está minimizada)
    public RenderTexture mainCameraTexture;
    
    // Textura de renderizado para el minimapa (cuando está en modo pequeño)
    public RenderTexture minimapTexture;

    // Variable de estado que indica si el minimapa está en vista completa o no
    // false = Vista normal (cámara principal grande, minimapa pequeño)
    // true = Vista invertida (minimapa grande, cámara principal en RawImage)
    private bool isMinimapFullView = false;

    // ============================================
    // SECCIÓN 2: DETECCIÓN DE INTERACCIÓN
    // ============================================
    
    /// <summary>
    /// Método de la interfaz IPointerClickHandler.
    /// Se ejecuta automáticamente cuando el usuario hace clic en el GameObject
    /// que tiene este script (típicamente la imagen del minimapa).
    /// </summary>
    /// <param name="eventData">Datos del evento de clic</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Al hacer clic, alternamos entre las dos vistas
        ToggleCameraView();
    }

    // ============================================
    // SECCIÓN 3: LÓGICA DE CAMBIO DE VISTA
    // ============================================
    
    /// <summary>
    /// Alterna entre dos modos de visualización:
    /// Modo 1: Cámara principal en pantalla completa, minimapa pequeño en esquina
    /// Modo 2: Minimapa en pantalla completa, cámara principal en esquina
    /// </summary>
    private void ToggleCameraView()
    {
        // Invertimos el estado actual
        isMinimapFullView = !isMinimapFullView;

        if (isMinimapFullView)
        {
            // ==========================================
            // MODO: MINIMAPA EN PANTALLA COMPLETA
            // ==========================================
            
            // La cámara del minimapa renderiza directamente a la pantalla (sin RenderTexture)
            minimapCamera.targetTexture = null; // null = pantalla completa
            
            // La cámara principal ahora renderiza a una textura
            mainCamera.targetTexture = mainCameraTexture;
            
            // La RawImage del minimapa ahora muestra la cámara principal (intercambio)
            minimapImage.texture = mainCameraTexture;

            // Desactivamos la cámara principal para que no renderice en pantalla
            mainCamera.enabled = false;
            
            // La cámara del minimapa es ahora la vista principal
            minimapCamera.enabled = true;
        }
        else
        {
            // ==========================================
            // MODO: VISTA NORMAL (CÁMARA PRINCIPAL GRANDE)
            // ==========================================
            
            // La cámara principal renderiza directamente a la pantalla
            mainCamera.targetTexture = null; // null = pantalla completa
            
            // El minimapa renderiza a su textura específica
            minimapCamera.targetTexture = minimapTexture;
            
            // La RawImage muestra la textura del minimapa (vista aérea pequeña)
            minimapImage.texture = minimapTexture;

            // Ambas cámaras están activas:
            // - mainCamera dibuja la vista principal en pantalla
            mainCamera.enabled = true;
            // - minimapCamera dibuja en su RenderTexture (que se ve en la esquina)
            minimapCamera.enabled = true;
        }
    }
}

// ============================================
// EXPLICACIÓN DE RENDER TEXTURES
// ============================================
/*
 * RenderTexture: Es una textura especial donde una cámara puede dibujar su vista
 * 
 * Cuando targetTexture = null:
 *   → La cámara renderiza directamente en la pantalla (vista completa)
 * 
 * Cuando targetTexture = miTextura:
 *   → La cámara renderiza en esa textura (puede mostrarse en una RawImage pequeña)
 * 
 * Este script permite hacer "Picture-in-Picture" intercambiando qué cámara
 * renderiza a pantalla completa y cuál renderiza a la imagen pequeña.
 */