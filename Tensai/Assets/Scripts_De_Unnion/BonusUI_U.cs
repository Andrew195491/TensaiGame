using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Clase que gestiona la interfaz de usuario para las cartas bonus almacenadas.
/// Muestra hasta 3 cartas y permite ver información detallada y usarlas.
/// ACTUALIZADO: Compatible con Carta_U y CartaManager_U
/// </summary>
public class BonusUI_U : MonoBehaviour
{
    // ============================================
    // SECCIÓN 1: DECLARACIÓN DE VARIABLES
    // ============================================
    [Header("Slots de almacenamiento")]
    public GameObject storageCard1;
    public GameObject storageCard2;
    public GameObject storageCard3;

    [Header("Panel de explicación")]
    public GameObject cardExplaining;
    public TextMeshProUGUI cardExplainingText;
    public Button btnUsarCarta;
    public Button btnCerrarPanel;

    [Header("Referencias del jugador")]
    public MovePlayer_U jugador; // CAMBIADO: Ahora usa MovePlayer_U

    private List<GameObject> storageSlots = new List<GameObject>();
    private int cartaSeleccionadaIndex = -1;

    // ============================================
    // SECCIÓN 2: INICIALIZACIÓN
    // ============================================
    void Awake()
    {
        storageSlots.Add(storageCard1);
        storageSlots.Add(storageCard2);
        storageSlots.Add(storageCard3);

        foreach (var slot in storageSlots)
            slot.SetActive(false);

        cardExplaining.SetActive(false);

        if (btnUsarCarta != null)
        {
            btnUsarCarta.onClick.RemoveAllListeners();
            btnUsarCarta.onClick.AddListener(UsarCartaSeleccionada);
        }

        if (btnCerrarPanel != null)
        {
            btnCerrarPanel.onClick.RemoveAllListeners();
            btnCerrarPanel.onClick.AddListener(CerrarPanelExplicacion);
        }
    }

    // ============================================
    // SECCIÓN 3: ACTUALIZACIÓN DE LA INTERFAZ
    // ============================================
    /// <summary>
    /// Refresca los paneles visibles según la cantidad de cartas almacenadas.
    /// ACTUALIZADO: Ahora recibe List<Carta_U> en lugar de List<Carta>
    /// </summary>
    public void ActualizarUI(List<Carta_U> cartas)
    {
        foreach (var slot in storageSlots)
            slot.SetActive(false);

        if (cardExplaining.activeSelf && cartaSeleccionadaIndex >= cartas.Count)
        {
            CerrarPanelExplicacion();
        }

        for (int i = 0; i < cartas.Count && i < storageSlots.Count; i++)
        {
            GameObject slot = storageSlots[i];
            slot.SetActive(true);

            TextMeshProUGUI txt = slot.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                string tipoIcono = cartas[i].accion.Contains("Avanza") || cartas[i].accion == "RepiteTurno" ? "✨" : "⚡";
                txt.text = $"{tipoIcono} {ObtenerResumenCarta(cartas[i])}";
            }

            Button boton = slot.GetComponent<Button>();
            if (boton == null)
                boton = slot.AddComponent<Button>();

            int index = i;
            boton.onClick.RemoveAllListeners();
            boton.onClick.AddListener(() => MostrarInfoCarta(index, cartas[index]));

            EventTrigger trigger = slot.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                Destroy(trigger);
            }
        }
    }

    // ============================================
    // SECCIÓN 4: FUNCIONES AUXILIARES
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

    // ============================================
    // SECCIÓN 5: MANEJO DEL PANEL DE INFORMACIÓN
    // ============================================
    /// <summary>
    /// Muestra el panel emergente con información detallada de la carta.
    /// ACTUALIZADO: Ahora recibe Carta_U en lugar de Carta
    /// </summary>
    private void MostrarInfoCarta(int index, Carta_U carta)
    {
        cartaSeleccionadaIndex = index;
        cardExplaining.SetActive(true);

        string tipoIcono = carta.accion.Contains("Avanza") || carta.accion == "RepiteTurno" ? "✨" : "⚡";
        cardExplainingText.text = $"{tipoIcono} {carta.pregunta}\n\nHaz clic en 'Usar' para activar esta carta";

        Debug.Log($"Mostrando información de carta en posición {index}");
    }

    /// <summary>
    /// Usa la carta que está actualmente seleccionada.
    /// ACTUALIZADO: Ahora usa CartaManager_U
    /// </summary>
    private void UsarCartaSeleccionada()
    {
        if (cartaSeleccionadaIndex < 0)
        {
            Debug.LogWarning("No hay carta seleccionada");
            return;
        }

        // CAMBIADO: Usa CartaManager_U.instancia en lugar de CartaManager.instancia
        if (CartaManager_U.instancia != null && jugador != null)
        {
            CartaManager_U.instancia.UsarCartaDelStorage(cartaSeleccionadaIndex, jugador);
            Debug.Log($"Usando carta en posición {cartaSeleccionadaIndex}");
        }

        CerrarPanelExplicacion();
    }

    /// <summary>
    /// Cierra el panel de explicación y reinicia la selección.
    /// </summary>
    private void CerrarPanelExplicacion()
    {
        cardExplaining.SetActive(false);
        cartaSeleccionadaIndex = -1;
        Debug.Log("Panel de explicación cerrado");
    }
}