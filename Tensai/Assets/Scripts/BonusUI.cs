using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
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
                txt.text = cartas[i].pregunta; // Puedes poner un resumen aquí

            // Configurar los eventos de hover
            int index = i;
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

    private void MostrarExplicacion(Carta carta)
    {
        cardExplaining.SetActive(true);
        cardExplainingText.text = $"📜 {carta.pregunta}\n➡️ {carta.respuesta1}";
    }

    private void OcultarExplicacion()
    {
        cardExplaining.SetActive(false);
    }
}
