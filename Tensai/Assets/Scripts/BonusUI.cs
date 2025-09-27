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

    [Header("Referencias del jugador")]
    public MovePlayer jugador; // Asignar en el inspector

    private List<GameObject> storageSlots = new List<GameObject>();

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
    }

    /// <summary>
    /// Refresca los paneles visibles en pantalla según la cantidad de cartas almacenadas
    /// </summary>
    public void ActualizarUI(List<Carta> cartas)
    {
        // Apagar todos los slots primero
        foreach (var slot in storageSlots)
            slot.SetActive(false);

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

            // Configurar botón para usar la carta
            Button boton = slot.GetComponent<Button>();
            if (boton == null) 
                boton = slot.AddComponent<Button>();

            int index = i; // Capturar índice para el closure
            boton.onClick.RemoveAllListeners();
            boton.onClick.AddListener(() => UsarCartaEnPosicion(index));

            // Configurar los eventos de hover
            EventTrigger trigger = slot.GetComponent<EventTrigger>();
            if (trigger == null) trigger = slot.AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            // OnPointerEnter → mostrar explicación
            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((data) => {
                MostrarExplicacion(cartas[index]);
            });
            trigger.triggers.Add(entryEnter);

            // OnPointerExit → ocultar explicación
            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((data) => {
                OcultarExplicacion();
            });
            trigger.triggers.Add(entryExit);
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

    private void UsarCartaEnPosicion(int posicion)
    {
        if (CartaManager.instancia != null && jugador != null)
        {
            CartaManager.instancia.UsarCartaDelStorage(posicion, jugador);
            Debug.Log($"🎯 Usando carta en posición {posicion}");
        }
    }

    private void MostrarExplicacion(Carta carta)
    {
        cardExplaining.SetActive(true);
        
        string tipoIcono = carta.accion.Contains("Avanza") || carta.accion == "RepiteTurno" ? "✨" : "⚡";
        cardExplainingText.text = $"{tipoIcono} {carta.pregunta}\n\n💡 Haz clic para usar esta carta";
    }

    private void OcultarExplicacion()
    {
        cardExplaining.SetActive(false);
    }
}