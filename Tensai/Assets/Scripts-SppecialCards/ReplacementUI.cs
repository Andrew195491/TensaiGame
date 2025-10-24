using UnityEngine; // Necesario para MonoBehaviour
using UnityEngine.UI;  // Necesario para Button
using TMPro;  // Necesario para TextMeshProUGUI
using System.Collections.Generic;  // Necesario para List<T> (colecciones genéricas)

/// <summary>
/// Gestiona la interfaz de reemplazo de cartas cuando el inventario está lleno.
/// Implementa un sistema de confirmación de dos pasos:
/// 1. Panel de selección: Elige qué carta descartar
/// 2. Panel de confirmación: Confirma o cancela el reemplazo
/// 
/// Este sistema previene errores del jugador al reemplazar cartas accidentalmente.
/// </summary>
public class ReplacementUI : MonoBehaviour
{
    // ============================================
    // SECCIÓN 1: COMPONENTES DEL PANEL DE SELECCIÓN
    // ============================================
    
    [Header("Componentes del Canvas")]
    /// <summary>
    /// Canvas principal que contiene el panel de selección de cartas.
    /// Se muestra cuando el inventario está lleno y hay una carta nueva.
    /// </summary>
    public GameObject selectionCanvas;
    
    /// <summary>
    /// Texto informativo que explica la situación al jugador.
    /// Muestra la nueva carta y pide elegir cuál descartar.
    /// </summary>
    public TextMeshProUGUI infoText;
    
    /// <summary>
    /// Botón para cancelar todo el proceso de reemplazo.
    /// Descarta la nueva carta y mantiene el inventario actual.
    /// </summary>
    public Button cancelButton;

    [Header("Slots de Selección (Los GameObjects con Button, no el parent)")]
    /// <summary>
    /// Botones que representan las 3 cartas actuales del inventario.
    /// El jugador hace clic en uno para elegir qué carta reemplazar.
    /// IMPORTANTE: Deben ser los GameObjects con el componente Button, no sus padres.
    /// </summary>
    public Button storageCard1Button;
    public Button storageCard2Button;
    public Button storageCard3Button;

    // ============================================
    // SECCIÓN 2: COMPONENTES DEL PANEL DE CONFIRMACIÓN
    // ============================================
    
    [Header("Panel de Confirmación")]
    /// <summary>
    /// Canvas del panel de confirmación (segundo paso).
    /// Se muestra después de seleccionar una carta para reemplazar.
    /// </summary>
    public GameObject confirmationCard_Canvas;
    
    /// <summary>
    /// Texto que muestra qué carta se va a reemplazar y por cuál.
    /// Permite al jugador revisar su decisión antes de confirmar.
    /// </summary>
    public TextMeshProUGUI notificationText;
    
    /// <summary>
    /// Botón "Sí" - Confirma el reemplazo definitivamente.
    /// Ejecuta el reemplazo y cierra todos los paneles.
    /// </summary>
    public Button yesButton;
    
    /// <summary>
    /// Botón "No" - Cancela la confirmación.
    /// Regresa al panel de selección para elegir otra carta.
    /// </summary>
    public Button noButton;

    // ============================================
    // SECCIÓN 3: VARIABLES DE ESTADO
    // ============================================
    
    /// <summary>
    /// Lista interna que agrupa los 3 botones de selección.
    /// Facilita iterar sobre ellos para configurarlos dinámicamente.
    /// </summary>
    private List<Button> selectionButtons = new List<Button>();
    
    /// <summary>
    /// Almacena temporalmente la nueva carta que se quiere agregar.
    /// Se guarda mientras el jugador decide qué hacer.
    /// </summary>
    private Carta nuevaCartaPendiente;
    
    /// <summary>
    /// Índice de la carta seleccionada para reemplazar (0, 1 o 2).
    /// -1 indica que no hay ninguna seleccionada.
    /// </summary>
    private int indiceSeleccionado = -1;
    
    /// <summary>
    /// Callback opcional que se ejecuta al completar o cancelar el proceso.
    /// Típicamente usado para desbloquear el dado o continuar el flujo del juego.
    /// </summary>
    private System.Action callbackOnComplete;

    // ============================================
    // SECCIÓN 4: INICIALIZACIÓN
    // ============================================
    
    void Awake()
    {
        // Agrupar los botones en una lista para fácil acceso
        selectionButtons.Add(storageCard1Button);
        selectionButtons.Add(storageCard2Button);
        selectionButtons.Add(storageCard3Button);

        // ====== OCULTAR PANELES AL INICIO ======
        
        // Los paneles solo deben mostrarse cuando sea necesario
        if (selectionCanvas != null)
            selectionCanvas.SetActive(false);
        
        if (confirmationCard_Canvas != null)
            confirmationCard_Canvas.SetActive(false);

        // ====== CONFIGURAR BOTÓN DE CANCELAR ======
        
        if (cancelButton != null)
        {
            // Limpiar listeners anteriores
            cancelButton.onClick.RemoveAllListeners();
            // Asignar función de cancelación
            cancelButton.onClick.AddListener(CancelarReemplazo);
        }

        // ====== CONFIGURAR BOTONES DE CONFIRMACIÓN ======
        
        // Botón "Sí" - Confirmar reemplazo
        if (yesButton != null)
        {
            yesButton.onClick.RemoveAllListeners();
            yesButton.onClick.AddListener(ConfirmarReemplazoFinal);
        }

        // Botón "No" - Cancelar confirmación
        if (noButton != null)
        {
            noButton.onClick.RemoveAllListeners();
            noButton.onClick.AddListener(CancelarConfirmacion);
        }
    }

    // ============================================
    // SECCIÓN 5: MOSTRAR PANEL DE SELECCIÓN
    // ============================================
    
    /// <summary>
    /// Muestra el panel de selección para que el jugador elija qué carta reemplazar.
    /// Este es el punto de entrada principal del sistema de reemplazo.
    /// 
    /// FLUJO:
    /// 1. Guarda la nueva carta y callback
    /// 2. Bloquea el dado para prevenir tiradas durante la decisión
    /// 3. Muestra información de la nueva carta
    /// 4. Configura botones con las cartas actuales del inventario
    /// 5. Espera a que el jugador elija una carta
    /// </summary>
    /// <param name="cartasActuales">Lista de cartas actualmente en el inventario</param>
    /// <param name="nuevaCarta">Nueva carta que se quiere agregar</param>
    /// <param name="onComplete">Callback ejecutado al finalizar el proceso</param>
    public void MostrarPanelReemplazo(List<Carta> cartasActuales, Carta nuevaCarta, System.Action onComplete = null)
    {
        // Guardar referencias para uso posterior
        this.nuevaCartaPendiente = nuevaCarta;
        this.callbackOnComplete = onComplete;
        
        // Mostrar el panel de selección
        selectionCanvas.SetActive(true);

        // ====== BLOQUEAR EL DADO ======
        
        // Prevenir que el jugador tire el dado mientras toma una decisión
        if (CartaManager.instancia != null && CartaManager.instancia.dadoController != null)
        {
            CartaManager.instancia.dadoController.BloquearDado(true);
        }

        // ====== CONFIGURAR TEXTO INFORMATIVO ======
        
        // Determinar icono según el tipo de carta nueva
        // ✨ = beneficio (Avanza, RepiteTurno)
        // ⚡ = otros efectos
        string tipoIcono = nuevaCarta.accion.Contains("Avanza") || nuevaCarta.accion == "RepiteTurno" ? "✨" : "⚡";
        
        // Mostrar información al jugador
        infoText.text = $"Tu almacenamiento está lleno.\n\nNueva carta: {tipoIcono} <b>{ObtenerResumenCarta(nuevaCarta)}</b>\n\n Elige una carta para descartar:";

        // ====== CONFIGURAR BOTONES DE SELECCIÓN ======
        
        // Iterar sobre las cartas actuales del inventario
        for (int i = 0; i < cartasActuales.Count && i < selectionButtons.Count; i++)
        {
            Button boton = selectionButtons[i];
            
            // Validar que el botón está asignado
            if (boton == null)
            {
                Debug.LogError($"El botón de storage card {i + 1} no está asignado en el Inspector!");
                continue;
            }

            // Activar el botón y su contenedor padre (si existe)
            boton.gameObject.SetActive(true);
            if (boton.transform.parent != null)
                boton.transform.parent.gameObject.SetActive(true);

            // Actualizar el texto del botón con información de la carta
            TextMeshProUGUI txt = boton.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                // Determinar icono de la carta actual
                string iconoCarta = cartasActuales[i].accion.Contains("Avanza") || cartasActuales[i].accion == "RepiteTurno" ? "✨" : "⚡";
                // Mostrar icono + resumen de la carta
                txt.text = $"{iconoCarta} {ObtenerResumenCarta(cartasActuales[i])}";
            }

            // ====== CONFIGURAR LISTENER DEL BOTÓN ======
            
            // Captura del índice para el closure (evitar problema de closure en loops)
            int index = i;
            
            // Limpiar listeners anteriores
            boton.onClick.RemoveAllListeners();
            
            // Al hacer clic, mostrar panel de confirmación (no reemplazar directamente)
            boton.onClick.AddListener(() => MostrarConfirmacion(index, cartasActuales[index]));
        }

        // ====== OCULTAR BOTONES NO USADOS ======
        
        // Si hay menos de 3 cartas, ocultar botones sobrantes
        for (int i = cartasActuales.Count; i < selectionButtons.Count; i++)
        {
            if (selectionButtons[i] != null)
            {
                selectionButtons[i].gameObject.SetActive(false);
                if (selectionButtons[i].transform.parent != null)
                    selectionButtons[i].transform.parent.gameObject.SetActive(false);
            }
        }

        Debug.Log($"Panel de reemplazo mostrado. Cartas actuales: {cartasActuales.Count}");
    }

    // ============================================
    // SECCIÓN 6: PANEL DE CONFIRMACIÓN
    // ============================================
    
    /// <summary>
    /// Muestra el panel de confirmación antes de ejecutar el reemplazo.
    /// Este paso adicional previene errores del jugador.
    /// 
    /// FLUJO:
    /// 1. Oculta el panel de selección
    /// 2. Muestra el panel de confirmación
    /// 3. Presenta claramente qué carta se va a reemplazar y por cuál
    /// 4. Espera confirmación del jugador (Sí/No)
    /// </summary>
    /// <param name="index">Índice de la carta a reemplazar (0-2)</param>
    /// <param name="cartaAReemplazar">Carta que será descartada</param>
    private void MostrarConfirmacion(int index, Carta cartaAReemplazar)
    {
        // Guardar el índice seleccionado
        indiceSeleccionado = index;
        
        // ====== CAMBIAR PANELES ======
        
        // Ocultar el panel de selección
        selectionCanvas.SetActive(false);
        
        // Mostrar el panel de confirmación
        confirmationCard_Canvas.SetActive(true);
        
        // ====== CONFIGURAR MENSAJE DE CONFIRMACIÓN ======
        
        // Determinar iconos de ambas cartas
        string iconoVieja = cartaAReemplazar.accion.Contains("Avanza") || cartaAReemplazar.accion == "RepiteTurno" ? "✨" : "⚡";
        string iconoNueva = nuevaCartaPendiente.accion.Contains("Avanza") || nuevaCartaPendiente.accion == "RepiteTurno" ? "✨" : "⚡";
        
        // Mostrar comparación clara: Carta vieja → Carta nueva
        notificationText.text = $"¿Estás seguro de que quieres reemplazar:\n\n{iconoVieja} <b>{ObtenerResumenCarta(cartaAReemplazar)}</b>\n\nPor:\n\n{iconoNueva} <b>{ObtenerResumenCarta(nuevaCartaPendiente)}</b>?";
        
        Debug.Log($"Mostrando confirmación para reemplazar carta en índice {index}");
    }

    // ============================================
    // SECCIÓN 7: CONFIRMAR REEMPLAZO
    // ============================================
    
    /// <summary>
    /// Se ejecuta cuando el jugador presiona "Sí" en el panel de confirmación.
    /// Realiza el reemplazo definitivo de la carta en el inventario.
    /// </summary>
    private void ConfirmarReemplazoFinal()
    {
        // Verificar que CartaManager existe y hay un índice válido
        if (CartaManager.instancia != null && indiceSeleccionado >= 0)
        {
            // Ejecutar el reemplazo en el sistema central
            CartaManager.instancia.ReemplazarCartaEnStorage(indiceSeleccionado, nuevaCartaPendiente);
            Debug.Log($"Carta en posición {indiceSeleccionado} reemplazada por: {nuevaCartaPendiente.pregunta}");
        }
        
        // Cerrar todos los paneles y limpiar estado
        CerrarPaneles();
    }

    // ============================================
    // SECCIÓN 8: CANCELAR CONFIRMACIÓN
    // ============================================
    
    /// <summary>
    /// Se ejecuta cuando el jugador presiona "No" en el panel de confirmación.
    /// Regresa al panel de selección para que pueda elegir otra carta.
    /// No descarta la nueva carta, solo permite reconsiderar la elección.
    /// </summary>
    private void CancelarConfirmacion()
    {
        // Ocultar el panel de confirmación
        confirmationCard_Canvas.SetActive(false);
        
        // Volver a mostrar el panel de selección
        selectionCanvas.SetActive(true);
        
        // Reiniciar índice seleccionado
        indiceSeleccionado = -1;
        
        Debug.Log("Confirmación cancelada. Volviendo a la selección de cartas.");
    }

    // ============================================
    // SECCIÓN 9: CANCELAR TODO EL PROCESO
    // ============================================
    
    /// <summary>
    /// Se ejecuta cuando el jugador presiona "Cancelar" en el panel principal.
    /// Descarta completamente la nueva carta y mantiene el inventario actual sin cambios.
    /// Esta es la opción de "salida" si el jugador decide que no quiere la nueva carta.
    /// </summary>
    private void CancelarReemplazo()
    {
        Debug.Log($"Reemplazo cancelado. Carta descartada: {nuevaCartaPendiente?.pregunta ?? "desconocida"}");
        CerrarPaneles();
    }

    // ============================================
    // SECCIÓN 10: CERRAR Y LIMPIAR
    // ============================================
    
    /// <summary>
    /// Cierra todos los paneles, limpia el estado y ejecuta el callback de finalización.
    /// Este método centraliza la limpieza para evitar duplicación de código.
    /// 
    /// Se ejecuta en dos casos:
    /// 1. Después de confirmar el reemplazo (ConfirmarReemplazoFinal)
    /// 2. Al cancelar todo el proceso (CancelarReemplazo)
    /// </summary>
    private void CerrarPaneles()
    {
        // ====== OCULTAR PANELES ======
        
        selectionCanvas.SetActive(false);
        confirmationCard_Canvas.SetActive(false);
        
        // ====== LIMPIAR ESTADO ======
        
        // Eliminar referencia a la carta pendiente
        nuevaCartaPendiente = null;
        // Reiniciar índice de selección
        indiceSeleccionado = -1;

        // ====== DESBLOQUEAR EL DADO ======
        
        // Permitir que el jugador continúe jugando
        if (CartaManager.instancia != null && CartaManager.instancia.dadoController != null)
        {
            CartaManager.instancia.dadoController.BloquearDado(false);
        }

        // ====== EJECUTAR CALLBACK ======
        
        // Ejecutar callback si existe (ej: desbloquear otras funcionalidades)
        callbackOnComplete?.Invoke();
        // Limpiar referencia al callback
        callbackOnComplete = null;

        Debug.Log("Paneles de reemplazo cerrados");
    }

    // ============================================
    // SECCIÓN 11: FUNCIÓN AUXILIAR DE RESUMEN
    // ============================================
    
    /// <summary>
    /// Convierte el código de acción de una carta en un texto corto y legible.
    /// Idéntico al método en BonusUI para mantener consistencia visual.
    /// </summary>
    /// <param name="carta">Carta de la cual obtener el resumen</param>
    /// <returns>Texto descriptivo corto de la acción</returns>
    private string ObtenerResumenCarta(Carta carta)
    {
        // Switch expression para mapear acciones a textos
        return carta.accion switch
        {
            "Avanza1" => "Avanza +1",
            "Avanza2" => "Avanza +2",
            "Avanza3" => "Avanza +3",
            "RepiteTurno" => "Repite turno",
            "Intercambia" => "Intercambiar",
            "Inmunidad" => "Inmunidad",
            "DobleDado" => "Doble dado",
            "TeletransporteAdelante" => "Teletransporte+",
            "ElegirDado" => "Elegir dado",
            "RobarCarta" => "Robar carta",
            "Retrocede1" => "Retrocede -1",
            "Retrocede2" => "Retrocede -2",
            "Retrocede3" => "Retrocede -3",
            "PierdeTurno" => "Pierde turno",
            "IrSalida" => "A salida",
            _ => "Especial" // Caso por defecto
        };
    }
}

// ============================================
// FLUJO COMPLETO DEL SISTEMA
// ============================================
/*
 * ESCENARIO: El jugador tiene 3 cartas y obtiene una cuarta
 * 
 * 1. CartaManager.IntentarAgregarCarta() detecta que el inventario está lleno
 * 
 * 2. CartaManager llama a ReplacementUI.MostrarPanelReemplazo()
 *    - Se muestra el panel de selección
 *    - Se bloquea el dado
 *    - Se muestran las 3 cartas actuales como botones
 * 
 * 3. El jugador hace clic en una de las 3 cartas
 *    - Se oculta el panel de selección
 *    - Se muestra el panel de confirmación
 *    - Se presenta claramente: "¿Reemplazar X por Y?"
 * 
 * 4A. El jugador presiona "Sí":
 *     - CartaManager.ReemplazarCartaEnStorage() ejecuta el reemplazo
 *     - Se cierran todos los paneles
 *     - Se desbloquea el dado
 *     - El jugador continúa con su nueva carta
 * 
 * 4B. El jugador presiona "No":
 *     - Vuelve al panel de selección (paso 2)
 *     - Puede elegir otra carta diferente
 * 
 * 4C. El jugador presiona "Cancelar" (en cualquier momento):
 *     - La nueva carta se descarta completamente
 *     - Se mantiene el inventario actual sin cambios
 *     - Se cierran todos los paneles
 *     - Se desbloquea el dado
 * 
 * VENTAJAS DE ESTE SISTEMA:
 * 
 * ✓ Confirmación de dos pasos previene errores costosos
 * ✓ Información visual clara con iconos y resúmenes
 * ✓ Permite reconsiderar la decisión (botón "No")
 * ✓ Permite descartar la nueva carta si no la quiere (botón "Cancelar")
 * ✓ Bloquea el dado durante el proceso para evitar conflictos
 * ✓ Sistema robusto con limpieza adecuada de estado
 */