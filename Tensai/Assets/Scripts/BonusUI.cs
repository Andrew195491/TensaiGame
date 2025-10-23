using UnityEngine; // Necesario para MonoBehaviour (MonoBehaviour es la clase base para scripts en Unity)
using UnityEngine.UI; // Necesario para Button
using UnityEngine.EventSystems; // Necesario para EventTrigger
using TMPro; // Necesario para TextMeshProUGUI
using System.Collections.Generic; // Necesario para List<T>

/// <summary>
/// Clase que gestiona la interfaz de usuario para las cartas bonus almacenadas.
/// Muestra hasta 3 cartas y permite ver información detallada y usarlas.
/// </summary>
public class BonusUI : MonoBehaviour
{
    // ============================================
    // SECCIÓN 1: DECLARACIÓN DE VARIABLES
    // ============================================
    
    [Header("Slots de almacenamiento")]
    // Estos son los 3 espacios visuales donde se muestran las cartas guardadas
    public GameObject storageCard1;
    public GameObject storageCard2;
    public GameObject storageCard3;

    [Header("Panel de explicación")]
    // Panel emergente que muestra información detallada de una carta
    public GameObject cardExplaining;
    // Texto que muestra la descripción de la carta seleccionada
    public TextMeshProUGUI cardExplainingText;
    // Botón para activar/usar la carta seleccionada
    public Button btnUsarCarta;
    // Botón para cerrar el panel sin usar la carta
    public Button btnCerrarPanel;

    [Header("Referencias del jugador")]
    // Referencia al script del jugador para aplicar efectos de cartas
    public MovePlayer jugador;

    // Lista interna que agrupa los 3 slots para facilitar su manejo
    private List<GameObject> storageSlots = new List<GameObject>();
    // Índice de la carta actualmente seleccionada (-1 = ninguna)
    private int cartaSeleccionadaIndex = -1;

    // ============================================
    // SECCIÓN 2: INICIALIZACIÓN
    // ============================================
    
    void Awake()
    {
        // Agrupamos los 3 slots en una lista para iterar fácilmente sobre ellos
        storageSlots.Add(storageCard1);
        storageSlots.Add(storageCard2);
        storageSlots.Add(storageCard3);

        // Ocultamos todos los slots al inicio (se mostrarán cuando haya cartas)
        foreach (var slot in storageSlots)
            slot.SetActive(false);

        // El panel de explicación también comienza oculto
        cardExplaining.SetActive(false);

        // Configuramos el botón "Usar Carta"
        if (btnUsarCarta != null)
        {
            // Limpiamos listeners previos para evitar duplicados
            btnUsarCarta.onClick.RemoveAllListeners();
            // Asignamos la función que se ejecuta al hacer clic
            btnUsarCarta.onClick.AddListener(UsarCartaSeleccionada);
        }

        // Configuramos el botón "Cerrar Panel"
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
    /// Se llama cada vez que el inventario de cartas cambia.
    /// </summary>
    public void ActualizarUI(List<Carta> cartas)
    {
        // Primero ocultamos todos los slots
        foreach (var slot in storageSlots)
            slot.SetActive(false);

        // Si el panel está abierto y la carta ya no existe (fue usada), lo cerramos
        if (cardExplaining.activeSelf && cartaSeleccionadaIndex >= cartas.Count)
        {
            CerrarPanelExplicacion();
        }

        // Activamos solo los slots necesarios según cuántas cartas hay
        for (int i = 0; i < cartas.Count && i < storageSlots.Count; i++)
        {
            GameObject slot = storageSlots[i];
            slot.SetActive(true); // Mostramos el slot

            // Buscamos el componente de texto dentro del slot
            TextMeshProUGUI txt = slot.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                // Asignamos un icono según el tipo de carta
                // ✨ para cartas positivas (Avanza, RepiteTurno)
                // ⚡ para otras cartas
                string tipoIcono = cartas[i].accion.Contains("Avanza") || cartas[i].accion == "RepiteTurno" ? "✨" : "⚡";
                // Mostramos el icono y un resumen de la carta
                txt.text = $"{tipoIcono} {ObtenerResumenCarta(cartas[i])}";
            }

            // Configuramos el botón del slot para mostrar información
            Button boton = slot.GetComponent<Button>();
            if (boton == null) 
                boton = slot.AddComponent<Button>(); // Si no existe, lo creamos

            // Capturamos el índice en una variable local (closure)
            int index = i;
            // Limpiamos listeners anteriores
            boton.onClick.RemoveAllListeners();
            // Al hacer clic, mostramos la información de esta carta
            boton.onClick.AddListener(() => MostrarInfoCarta(index, cartas[index]));

            // Eliminamos EventTriggers anteriores para evitar conflictos
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
    /// Usado para mostrar un resumen en los slots de almacenamiento.
    /// </summary>
    private string ObtenerResumenCarta(Carta carta)
    {
        // Switch expression para mapear acciones a textos descriptivos
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
            _ => "Especial" // Caso por defecto para acciones no reconocidas
        };
    }

    // ============================================
    // SECCIÓN 5: MANEJO DEL PANEL DE INFORMACIÓN
    // ============================================
    
    /// <summary>
    /// Muestra el panel emergente con información detallada de la carta.
    /// Se ejecuta cuando el jugador hace clic en un slot de carta.
    /// </summary>
    private void MostrarInfoCarta(int index, Carta carta)
    {
        // Guardamos qué carta está seleccionada actualmente
        cartaSeleccionadaIndex = index;
        // Mostramos el panel
        cardExplaining.SetActive(true);
        
        // Determinamos el icono según el tipo de carta
        string tipoIcono = carta.accion.Contains("Avanza") || carta.accion == "RepiteTurno" ? "✨" : "⚡";
        // Mostramos la pregunta/descripción de la carta e instrucciones
        cardExplainingText.text = $"{tipoIcono} {carta.pregunta}\n\nHaz clic en 'Usar' para activar esta carta";
        
        Debug.Log($"Mostrando información de carta en posición {index}");
    }

    /// <summary>
    /// Usa la carta que está actualmente seleccionada.
    /// Se ejecuta cuando el jugador presiona el botón "Usar".
    /// </summary>
    private void UsarCartaSeleccionada()
    {
        // Verificamos que haya una carta seleccionada
        if (cartaSeleccionadaIndex < 0)
        {
            Debug.LogWarning("No hay carta seleccionada");
            return;
        }

        // Comunicamos con el CartaManager para aplicar el efecto de la carta
        if (CartaManager.instancia != null && jugador != null)
        {
            // Delegamos el uso de la carta al sistema central
            CartaManager.instancia.UsarCartaDelStorage(cartaSeleccionadaIndex, jugador);
            Debug.Log($"Usando carta en posición {cartaSeleccionadaIndex}");
        }

        // Cerramos el panel después de usar la carta
        CerrarPanelExplicacion();
    }

    /// <summary>
    /// Cierra el panel de explicación y reinicia la selección.
    /// Se ejecuta al presionar "Cerrar" o después de usar una carta.
    /// </summary>
    private void CerrarPanelExplicacion()
    {
        // Ocultamos el panel
        cardExplaining.SetActive(false);
        // Reiniciamos el índice de selección
        cartaSeleccionadaIndex = -1;
        Debug.Log("Panel de explicación cerrado");
    }
}