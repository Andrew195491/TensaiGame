// ============================================
// ReplacementUI_U.cs (Unificado y Corregido)
// ============================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Interfaz unificada para gestionar el reemplazo de cartas cuando el inventario está lleno.
/// 
/// Combina el flujo original de selección y confirmación, con un diseño más visual y limpio.
/// 
/// FLUJO DE USO:
/// 1️⃣ Mostrar panel de selección con las 3 cartas actuales.
/// 2️⃣ Elegir una carta para reemplazar.
/// 3️⃣ Confirmar o cancelar el reemplazo.
/// 4️⃣ Se actualiza el inventario y se desbloquea el dado.
/// 
/// ACTUALIZADO: Compatible con Carta_U
/// </summary>
public class ReplacementUI_U : MonoBehaviour
{
    // ============================================
    // SECCIÓN 1: ELEMENTOS PRINCIPALES DE LA UI
    // ============================================

    [Header("Panel de Selección")]
    public GameObject selectionCanvas;
    public TextMeshProUGUI infoText;
    public Button cancelButton;
    public Button storageCard1Button;
    public Button storageCard2Button;
    public Button storageCard3Button;

    [Header("Panel de Confirmación")]
    public GameObject confirmationCanvas;
    public TextMeshProUGUI notificationText;
    public Button yesButton;
    public Button noButton;

    [Header("Estilos Visuales")]
    public Color colorConfirm = new Color(0.2f, 0.8f, 0.2f);
    public Color colorCancel = new Color(0.9f, 0.25f, 0.25f);
    public Color colorNeutral = Color.white;

    // ============================================
    // SECCIÓN 2: VARIABLES DE ESTADO
    // ============================================

    private List<Button> selectionButtons = new();
    private Carta_U nuevaCartaPendiente; // CAMBIADO: Carta → Carta_U
    private int indiceSeleccionado = -1;
    private System.Action callbackOnComplete;

    // ============================================
    // SECCIÓN 3: INICIALIZACIÓN
    // ============================================

    void Awake()
    {
        selectionButtons.Add(storageCard1Button);
        selectionButtons.Add(storageCard2Button);
        selectionButtons.Add(storageCard3Button);

        if (selectionCanvas) selectionCanvas.SetActive(false);
        if (confirmationCanvas) confirmationCanvas.SetActive(false);

        if (cancelButton)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(CancelarReemplazo);
        }

        if (yesButton)
        {
            yesButton.onClick.RemoveAllListeners();
            yesButton.onClick.AddListener(ConfirmarReemplazoFinal);
        }

        if (noButton)
        {
            noButton.onClick.RemoveAllListeners();
            noButton.onClick.AddListener(CancelarConfirmacion);
        }
    }

    // ============================================
    // SECCIÓN 4: MOSTRAR PANEL DE SELECCIÓN
    // ============================================

    /// <summary>
    /// Muestra el panel de reemplazo con las cartas actuales del inventario.
    /// ACTUALIZADO: Ahora recibe List<Carta_U> en lugar de List<Carta>
    /// </summary>
    public void MostrarPanelReemplazo(List<Carta_U> cartasActuales, Carta_U nuevaCarta, System.Action onComplete = null)
    {
        nuevaCartaPendiente = nuevaCarta;
        callbackOnComplete = onComplete;

        if (!selectionCanvas) return;
        selectionCanvas.SetActive(true);

        // Bloquear el dado mientras el jugador decide
        if (CartaManager_U.instancia && CartaManager_U.instancia.dadoController)
            CartaManager_U.instancia.dadoController.BloquearDado(true);

        // Mostrar descripción de la nueva carta
        string icono = nuevaCarta.accion.Contains("Avanza") || nuevaCarta.accion == "RepiteTurno" ? "✨" : "⚡";
        infoText.text = $"Tu inventario está lleno.\n\nNueva carta: {icono} <b>{ObtenerResumenCarta(nuevaCarta)}</b>\n\nSelecciona una carta para reemplazar:";

        // Configurar botones con las cartas actuales
        for (int i = 0; i < selectionButtons.Count; i++)
        {
            Button btn = selectionButtons[i];
            if (i < cartasActuales.Count)
            {
                Carta_U carta = cartasActuales[i]; // CAMBIADO: Carta → Carta_U
                btn.gameObject.SetActive(true);
                btn.interactable = true;
                TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (txt)
                {
                    string iconoCarta = carta.accion.Contains("Avanza") || carta.accion == "RepiteTurno" ? "✨" : "⚡";
                    txt.text = $"{iconoCarta} {ObtenerResumenCarta(carta)}";
                }
                int index = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => MostrarConfirmacion(index, carta));
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }
    }

    // ============================================
    // SECCIÓN 5: MOSTRAR PANEL DE CONFIRMACIÓN
    // ============================================

    /// <summary>
    /// Muestra el panel de confirmación para reemplazar una carta específica.
    /// ACTUALIZADO: Ahora recibe Carta_U en lugar de Carta
    /// </summary>
    private void MostrarConfirmacion(int index, Carta_U cartaAReemplazar)
    {
        indiceSeleccionado = index;
        if (selectionCanvas) selectionCanvas.SetActive(false);
        if (confirmationCanvas) confirmationCanvas.SetActive(true);

        string iconoVieja = cartaAReemplazar.accion.Contains("Avanza") || cartaAReemplazar.accion == "RepiteTurno" ? "✨" : "⚡";
        string iconoNueva = nuevaCartaPendiente.accion.Contains("Avanza") || nuevaCartaPendiente.accion == "RepiteTurno" ? "✨" : "⚡";

        notificationText.text =
            $"¿Reemplazar:\n\n{iconoVieja} <b>{ObtenerResumenCarta(cartaAReemplazar)}</b>\n\nPor:\n\n{iconoNueva} <b>{ObtenerResumenCarta(nuevaCartaPendiente)}</b>?";
    }

    // ============================================
    // SECCIÓN 6: CONFIRMAR REEMPLAZO
    // ============================================

    private void ConfirmarReemplazoFinal()
    {
        if (CartaManager_U.instancia && indiceSeleccionado >= 0)
        {
            CartaManager_U.instancia.ReemplazarCartaEnStorage(indiceSeleccionado, nuevaCartaPendiente);
            Debug.Log($"✅ Carta en slot {indiceSeleccionado} reemplazada por '{nuevaCartaPendiente.pregunta}'");
        }
        CerrarPaneles();
    }

    // ============================================
    // SECCIÓN 7: CANCELAR CONFIRMACIÓN / PROCESO
    // ============================================

    private void CancelarConfirmacion()
    {
        if (confirmationCanvas) confirmationCanvas.SetActive(false);
        if (selectionCanvas) selectionCanvas.SetActive(true);
        indiceSeleccionado = -1;
        Debug.Log("↩️ Confirmación cancelada, volviendo al panel de selección.");
    }

    private void CancelarReemplazo()
    {
        Debug.Log($"❌ Reemplazo cancelado. Carta '{nuevaCartaPendiente?.pregunta ?? "desconocida"}' descartada.");
        CerrarPaneles();
    }

    // ============================================
    // SECCIÓN 8: CIERRE Y LIMPIEZA
    // ============================================

    private void CerrarPaneles()
    {
        if (selectionCanvas) selectionCanvas.SetActive(false);
        if (confirmationCanvas) confirmationCanvas.SetActive(false);

        nuevaCartaPendiente = null;
        indiceSeleccionado = -1;

        if (CartaManager_U.instancia && CartaManager_U.instancia.dadoController)
            CartaManager_U.instancia.dadoController.BloquearDado(false);

        callbackOnComplete?.Invoke();
        callbackOnComplete = null;

        Debug.Log("🧹 Paneles de reemplazo cerrados y estado limpiado.");
    }

    // ============================================
    // SECCIÓN 9: FUNCIÓN AUXILIAR
    // ============================================

    /// <summary>
    /// Convierte el tipo de acción de la carta en un texto corto y legible.
    /// ACTUALIZADO: Ahora recibe Carta_U en lugar de Carta
    /// </summary>
    private string ObtenerResumenCarta(Carta_U carta)
    {
        return carta.accion switch
        {
            "Avanza1" => "Avanza +1",
            "Avanza2" => "Avanza +2",
            "Avanza3" => "Avanza +3",
            "RepiteTurno" => "Repite turno",
            "Intercambia" => "Intercambia carta",
            "Inmunidad" => "Inmunidad temporal",
            "DobleDado" => "Doble dado",
            "TeletransporteAdelante" => "Teletransporte +",
            "ElegirDado" => "Elegir resultado",
            "RobarCarta" => "Robar carta",
            "Retrocede1" => "Retrocede -1",
            "Retrocede2" => "Retrocede -2",
            "Retrocede3" => "Retrocede -3",
            "PierdeTurno" => "Pierde turno",
            "IrSalida" => "Ir a salida",
            _ => "Especial"
        };
    }
}

// ============================================
// NOTAS DE DISEÑO Y FLUJO
// ============================================
/*
 * 🔹 Paso 1: Llamada desde CartaManager_U.IntentarAgregarCarta()
 *     → Si el inventario está lleno, se abre ReplacementUI_U.MostrarPanelReemplazo()
 *
 * 🔹 Paso 2: El jugador selecciona una carta a reemplazar
 *     → Se muestra panel de confirmación con comparación visual
 *
 * 🔹 Paso 3A: Confirma el reemplazo ("Sí")
 *     → Reemplaza la carta, cierra paneles, desbloquea el dado
 *
 * 🔹 Paso 3B: Cancela ("No")
 *     → Regresa al panel anterior para elegir otra carta
 *
 * 🔹 Paso 3C: Cancela todo ("Cancelar")
 *     → Descarta la nueva carta y mantiene el inventario igual
 *
 * ✨ CAMBIOS REALIZADOS:
 * - Carta → Carta_U en todos los parámetros y variables
 * - CartaManager → CartaManager_U en referencias a la instancia
 * - Mantenida toda la lógica y funcionalidad original
 */