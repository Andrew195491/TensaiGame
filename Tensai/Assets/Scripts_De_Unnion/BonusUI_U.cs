using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI de cartas bonus almacenadas (sólo info siempre; usar sólo en tu turno y antes de tirar).
/// </summary>
public class BonusUI_U : MonoBehaviour
{
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
    public MovePlayer_U jugador;

    // === Estado interno ===
    private readonly List<GameObject> storageSlots = new List<GameObject>();
    private int cartaSeleccionadaIndex = -1;

    /// <summary>
    /// Candado de uso: TRUE solo en tu turno y antes de tirar el dado.
    /// </summary>
    private bool puedeUsarAhora = false;

    void Awake()
    {
        storageSlots.Add(storageCard1);
        storageSlots.Add(storageCard2);
        storageSlots.Add(storageCard3);

        foreach (var slot in storageSlots) slot.SetActive(false);
        if (cardExplaining) cardExplaining.SetActive(false);

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

    /// <summary>
    /// Llamar desde GameManager:
    /// - true  => al empezar el turno del jugador, ANTES de habilitar el dado.
    /// - false => en cuanto el jugador tira el dado, o durante turnos de bots.
    /// </summary>
    public void SetUsoHabilitado(bool habilitar)
    {
        puedeUsarAhora = habilitar;

        // Si el panel está abierto, refleja el estado en el botón "Usar"
        if (cardExplaining && cardExplaining.activeSelf && btnUsarCarta != null)
            btnUsarCarta.interactable = puedeUsarAhora && cartaSeleccionadaIndex >= 0;
    }

    /// <summary>
    /// Refresca los paneles visibles según la cantidad de cartas almacenadas.
    /// </summary>
    public void ActualizarUI(List<Carta_U> cartas)
    {
        foreach (var slot in storageSlots) slot.SetActive(false);

        if (cardExplaining && cardExplaining.activeSelf && cartaSeleccionadaIndex >= cartas.Count)
            CerrarPanelExplicacion();

        for (int i = 0; i < cartas.Count && i < storageSlots.Count; i++)
        {
            GameObject slot = storageSlots[i];
            slot.SetActive(true);

            TextMeshProUGUI txt = slot.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                // usa iconos ASCII seguros para evitar warnings de fuentes
                string tipoIcono = (cartas[i].accion.Contains("Avanza") || cartas[i].accion == "RepiteTurno") ? "[+]" : "[!]";
                txt.text = $"{tipoIcono} {ObtenerResumenCarta(cartas[i])}";
            }

            Button boton = slot.GetComponent<Button>() ?? slot.AddComponent<Button>();
            int index = i;
            boton.onClick.RemoveAllListeners();
            boton.onClick.AddListener(() => MostrarInfoCarta(index, cartas[index]));

            var trigger = slot.GetComponent<EventTrigger>();
            if (trigger != null) Destroy(trigger);
        }
    }

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

    private void MostrarInfoCarta(int index, Carta_U carta)
    {
        cartaSeleccionadaIndex = index;

        if (cardExplaining) cardExplaining.SetActive(true);

        string tipoIcono = (carta.accion.Contains("Avanza") || carta.accion == "RepiteTurno") ? "[+]" : "[!]";
        if (cardExplainingText)
            cardExplainingText.text = $"{tipoIcono} {carta.pregunta}\n\n" +
                                      (puedeUsarAhora ? "Pulsa 'Usar' para activar esta carta." : "Sólo puedes usar cartas al inicio de tu turno, antes de tirar.");

        // Botón usar solo activo si se puede usar ahora
        if (btnUsarCarta) btnUsarCarta.interactable = puedeUsarAhora;
    }

    private void UsarCartaSeleccionada()
    {
        // Enforce del candado por si alguien llama por código
        if (!puedeUsarAhora)
        {
            Debug.Log("[BonusUI] No puedes usar cartas ahora. Solo al inicio de tu turno y antes de tirar.");
            return;
        }

        if (cartaSeleccionadaIndex < 0)
        {
            Debug.LogWarning("[BonusUI] No hay carta seleccionada.");
            return;
        }

        if (CartaManager_U.instancia != null && jugador != null)
        {
            CartaManager_U.instancia.UsarCartaDelStorage(cartaSeleccionadaIndex, jugador);
            Debug.Log($"[BonusUI] Usando carta en posición {cartaSeleccionadaIndex}");
        }

        CerrarPanelExplicacion();

        // Opcional: tras usar una carta, puedes seguir usando otras en el mismo turno
        // Si NO quieres permitirlo, descomenta:
        // SetUsoHabilitado(false);
    }

    private void CerrarPanelExplicacion()
    {
        if (cardExplaining) cardExplaining.SetActive(false);
        cartaSeleccionadaIndex = -1;
        // Mantén el estado de puedeUsarAhora como esté (lo controla GameManager)
    }
}
