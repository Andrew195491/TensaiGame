using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ReplacementUI : MonoBehaviour
{
    [Header("Componentes del Canvas")]
    public GameObject selectionCanvas;
    public TextMeshProUGUI infoText;
    public Button cancelButton;

    [Header("Slots de Selección (Los GameObjects con Button, no el parent)")]
    public Button storageCard1Button;
    public Button storageCard2Button;
    public Button storageCard3Button;

    [Header("Panel de Confirmación")]
    public GameObject confirmationCard_Canvas;
    public TextMeshProUGUI notificationText;
    public Button yesButton;
    public Button noButton;

    private List<Button> selectionButtons = new List<Button>();
    private Carta nuevaCartaPendiente;
    private int indiceSeleccionado = -1;
    private System.Action callbackOnComplete;

    void Awake()
    {
        // Guardamos los botones en una lista
        selectionButtons.Add(storageCard1Button);
        selectionButtons.Add(storageCard2Button);
        selectionButtons.Add(storageCard3Button);

        // Ocultar los paneles al inicio
        if (selectionCanvas != null)
            selectionCanvas.SetActive(false);
        
        if (confirmationCard_Canvas != null)
            confirmationCard_Canvas.SetActive(false);

        // Configurar el botón de cancelar
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(CancelarReemplazo);
        }

        // Configurar botones de confirmación
        if (yesButton != null)
        {
            yesButton.onClick.RemoveAllListeners();
            yesButton.onClick.AddListener(ConfirmarReemplazoFinal);
        }

        if (noButton != null)
        {
            noButton.onClick.RemoveAllListeners();
            noButton.onClick.AddListener(CancelarConfirmacion);
        }
    }

    /// <summary>
    /// Muestra el panel para que el jugador elija qué carta reemplazar.
    /// </summary>
    public void MostrarPanelReemplazo(List<Carta> cartasActuales, Carta nuevaCarta, System.Action onComplete = null)
    {
        this.nuevaCartaPendiente = nuevaCarta;
        this.callbackOnComplete = onComplete;
        selectionCanvas.SetActive(true);

        // Bloquear el dado mientras se toma la decisión
        if (CartaManager.instancia != null && CartaManager.instancia.dadoController != null)
        {
            CartaManager.instancia.dadoController.BloquearDado(true);
        }

        string tipoIcono = nuevaCarta.accion.Contains("Avanza") || nuevaCarta.accion == "RepiteTurno" ? "✨" : "⚡";
        infoText.text = $"Tu almacenamiento está lleno.\n\nNueva carta: {tipoIcono} <b>{ObtenerResumenCarta(nuevaCarta)}</b>\n\nElige una carta para descartar:";

        // Configurar cada botón con la información de la carta actual
        for (int i = 0; i < cartasActuales.Count && i < selectionButtons.Count; i++)
        {
            Button boton = selectionButtons[i];
            
            if (boton == null)
            {
                Debug.LogError($"El botón de storage card {i + 1} no está asignado en el Inspector!");
                continue;
            }

            // Activar el botón y su parent
            boton.gameObject.SetActive(true);
            if (boton.transform.parent != null)
                boton.transform.parent.gameObject.SetActive(true);

            // Actualizar texto del botón
            TextMeshProUGUI txt = boton.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                string iconoCarta = cartasActuales[i].accion.Contains("Avanza") || cartasActuales[i].accion == "RepiteTurno" ? "✨" : "⚡";
                txt.text = $"{iconoCarta} {ObtenerResumenCarta(cartasActuales[i])}";
            }

            // Configurar listener del botón - ahora muestra confirmación primero
            int index = i; // Captura de índice para el listener
            boton.onClick.RemoveAllListeners();
            boton.onClick.AddListener(() => MostrarConfirmacion(index, cartasActuales[index]));
        }

        // Ocultar botones que no se usan
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

    /// <summary>
    /// Muestra el panel de confirmación antes de reemplazar la carta.
    /// </summary>
    private void MostrarConfirmacion(int index, Carta cartaAReemplazar)
    {
        indiceSeleccionado = index;
        
        // Ocultar el panel de selección
        selectionCanvas.SetActive(false);
        
        // Mostrar el panel de confirmación
        confirmationCard_Canvas.SetActive(true);
        
        string iconoVieja = cartaAReemplazar.accion.Contains("Avanza") || cartaAReemplazar.accion == "RepiteTurno" ? "✨" : "⚡";
        string iconoNueva = nuevaCartaPendiente.accion.Contains("Avanza") || nuevaCartaPendiente.accion == "RepiteTurno" ? "✨" : "⚡";
        
        notificationText.text = $"¿Estás seguro de que quieres reemplazar:\n\n{iconoVieja} <b>{ObtenerResumenCarta(cartaAReemplazar)}</b>\n\nPor:\n\n{iconoNueva} <b>{ObtenerResumenCarta(nuevaCartaPendiente)}</b>?";
        
        Debug.Log($"Mostrando confirmación para reemplazar carta en índice {index}");
    }

    /// <summary>
    /// Se llama cuando el jugador confirma el reemplazo (botón "Yes").
    /// </summary>
    private void ConfirmarReemplazoFinal()
    {
        if (CartaManager.instancia != null && indiceSeleccionado >= 0)
        {
            CartaManager.instancia.ReemplazarCartaEnStorage(indiceSeleccionado, nuevaCartaPendiente);
            Debug.Log($"Carta en posición {indiceSeleccionado} reemplazada por: {nuevaCartaPendiente.pregunta}");
        }
        
        CerrarPaneles();
    }

    /// <summary>
    /// Se llama cuando el jugador cancela la confirmación (botón "No").
    /// Vuelve al panel de selección.
    /// </summary>
    private void CancelarConfirmacion()
    {
        // Ocultar confirmación
        confirmationCard_Canvas.SetActive(false);
        
        // Volver a mostrar el panel de selección
        selectionCanvas.SetActive(true);
        
        indiceSeleccionado = -1;
        
        Debug.Log("Confirmación cancelada. Volviendo a la selección de cartas.");
    }

    /// <summary>
    /// Se llama al pulsar el botón "Cancelar" en el panel principal. 
    /// Descarta la nueva carta completamente.
    /// </summary>
    private void CancelarReemplazo()
    {
        Debug.Log($"Reemplazo cancelado. Carta descartada: {nuevaCartaPendiente?.pregunta ?? "desconocida"}");
        CerrarPaneles();
    }

    /// <summary>
    /// Cierra todos los paneles y limpia el estado.
    /// </summary>
    private void CerrarPaneles()
    {
        selectionCanvas.SetActive(false);
        confirmationCard_Canvas.SetActive(false);
        
        nuevaCartaPendiente = null;
        indiceSeleccionado = -1;

        // Desbloquear el dado
        if (CartaManager.instancia != null && CartaManager.instancia.dadoController != null)
        {
            CartaManager.instancia.dadoController.BloquearDado(false);
        }

        // Ejecutar callback si existe
        callbackOnComplete?.Invoke();
        callbackOnComplete = null;

        Debug.Log("Paneles de reemplazo cerrados");
    }

    /// <summary>
    /// Helper para mostrar un resumen de la carta.
    /// </summary>
    private string ObtenerResumenCarta(Carta carta)
    {
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
            _ => "Especial"
        };
    }
}