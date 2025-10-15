using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class BonusUI : MonoBehaviour
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
    public MovePlayer jugador;

    private List<GameObject> storageSlots = new List<GameObject>();
    private int cartaSeleccionadaIndex = -1;

    void Awake()
    {
        // Guardamos los slots en una lista para fácil manejo
        storageSlots.Add(storageCard1);
        storageSlots.Add(storageCard2);
        storageSlots.Add(storageCard3);

        // Ocultamos todo al inicio
        foreach (var slot in storageSlots)
            slot.SetActive(false);

        cardExplaining.SetActive(false);

        // Configurar botones del panel de explicación
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
    /// Refresca los paneles visibles en pantalla según la cantidad de cartas almacenadas
    /// </summary>
    public void ActualizarUI(List<Carta> cartas)
    {
        // Apagar todos los slots primero
        foreach (var slot in storageSlots)
            slot.SetActive(false);

        // Si el panel está abierto y la carta ya no existe, cerrarlo
        if (cardExplaining.activeSelf && cartaSeleccionadaIndex >= cartas.Count)
        {
            CerrarPanelExplicacion();
        }

        // Activar solo los necesarios
        for (int i = 0; i < cartas.Count && i < storageSlots.Count; i++)
        {
            GameObject slot = storageSlots[i];
            slot.SetActive(true);

            // Buscar un TMP_Text dentro del slot (ej. nombre/resumen de carta)
            TextMeshProUGUI txt = slot.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                // Mostrar tipo de carta y efecto
                string tipoIcono = cartas[i].accion.Contains("Avanza") || cartas[i].accion == "RepiteTurno" ? "✨" : "⚡";
                txt.text = $"{tipoIcono} {ObtenerResumenCarta(cartas[i])}";
            }

            // Configurar botón para MOSTRAR INFO de la carta
            Button boton = slot.GetComponent<Button>();
            if (boton == null) 
                boton = slot.AddComponent<Button>();

            int index = i; // Capturar índice para el closure
            boton.onClick.RemoveAllListeners();
            boton.onClick.AddListener(() => MostrarInfoCarta(index, cartas[index]));

            // Limpiar EventTriggers anteriores
            EventTrigger trigger = slot.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                Destroy(trigger);
            }
        }
    }

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

    /// <summary>
    /// Muestra el panel de información de la carta al hacer clic
    /// </summary>
    private void MostrarInfoCarta(int index, Carta carta)
    {
        cartaSeleccionadaIndex = index;
        cardExplaining.SetActive(true);
        
        string tipoIcono = carta.accion.Contains("Avanza") || carta.accion == "RepiteTurno" ? "✨" : "⚡";
        cardExplainingText.text = $"{tipoIcono} {carta.pregunta}\n\nHaz clic en 'Usar' para activar esta carta";
        
        Debug.Log($"Mostrando información de carta en posición {index}");
    }

    /// <summary>
    /// Usa la carta seleccionada actualmente
    /// </summary>
    private void UsarCartaSeleccionada()
    {
        if (cartaSeleccionadaIndex < 0)
        {
            Debug.LogWarning("No hay carta seleccionada");
            return;
        }

        if (CartaManager.instancia != null && jugador != null)
        {
            CartaManager.instancia.UsarCartaDelStorage(cartaSeleccionadaIndex, jugador);
            Debug.Log($"Usando carta en posición {cartaSeleccionadaIndex}");
        }

        CerrarPanelExplicacion();
    }

    /// <summary>
    /// Cierra el panel de explicación
    /// </summary>
    private void CerrarPanelExplicacion()
    {
        cardExplaining.SetActive(false);
        cartaSeleccionadaIndex = -1;
        Debug.Log("Panel de explicación cerrado");
    }
}