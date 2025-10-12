using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CartaUI : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI textoPregunta;
    public TextMeshProUGUI textoRespuesta1;
    public TextMeshProUGUI textoRespuesta2;
    public TextMeshProUGUI textoRespuesta3;

    public Button boton1;
    public Button boton2;
    public Button boton3;

    public DiceController dado;

    private int respuestaCorrectaActual = 1;

    void Start()
    {
        // Asignar listeners a los botones
        boton1.onClick.AddListener(() => EvaluarRespuesta(1));
        boton2.onClick.AddListener(() => EvaluarRespuesta(2));
        boton3.onClick.AddListener(() => EvaluarRespuesta(3));

        panel.SetActive(false);
    }

    public void MostrarCarta(Carta carta, System.Action<int> onRespuesta)
    {
        panel.SetActive(true);
        textoPregunta.text = carta.pregunta;
        textoRespuesta1.text = carta.respuesta1;
        textoRespuesta2.text = carta.respuesta2;
        textoRespuesta3.text = carta.respuesta3;

        // Asignar acciones
        boton1.onClick.RemoveAllListeners();
        boton2.onClick.RemoveAllListeners();
        boton3.onClick.RemoveAllListeners();

        boton1.onClick.AddListener(() => { panel.SetActive(false); onRespuesta(1); });
        boton2.onClick.AddListener(() => { panel.SetActive(false); onRespuesta(2); });
        boton3.onClick.AddListener(() => { panel.SetActive(false); onRespuesta(3); });
    }

    // MÃƒÂ©todo para mostrar mensajes de las casillas especiales sin respuestas
    public void MostrarMensajeEspecial(string mensaje, System.Action onCerrar)
    {
        panel.SetActive(true);

        // Solo mostramos el mensaje, sin respuestas mÃƒÂºltiples
        textoPregunta.text = mensaje;
        textoRespuesta1.text = "";
        textoRespuesta2.text = "";
        textoRespuesta3.text = "";

        // Ocultar botones de respuestas 2 y 3 si quieres
        boton2.gameObject.SetActive(false);
        boton3.gameObject.SetActive(false);

        // BotÃƒÂ³n 1 serÃƒÂ¡ solo "Aceptar"
        boton1.GetComponentInChildren<TextMeshProUGUI>().text = "Aceptar";
        boton1.onClick.RemoveAllListeners();
        boton1.onClick.AddListener(() =>
        {
            panel.SetActive(false);
            // Restauramos visibilidad para la prÃƒÂ³xima carta
            boton2.gameObject.SetActive(true);
            boton3.gameObject.SetActive(true);
            onCerrar?.Invoke();
        });
    }



    void EvaluarRespuesta(int respuestaSeleccionada)
    {
        if (respuestaSeleccionada == respuestaCorrectaActual)
        {
            Debug.Log("Ã¢Å“â€¦ Ã‚Â¡Respuesta correcta!");
        }
        else
        {
            Debug.Log("Ã¢ÂÅ’ Respuesta incorrecta.");
        }

        panel.SetActive(false);        // Ocultar carta
        if (dado != null)
            dado.BloquearDado(false); // Ã°Å¸â€â€œ Desbloquear el dado
    }


// REEMPLAZA el método MostrarDecisionBeneficio con este:
/// <summary>
/// Muestra las opciones para una carta de beneficio: Guardarla o Descartarla.
/// </summary>
public void MostrarDecisionAlmacenar(Carta carta, MovePlayer jugador, System.Action onDecisionMade)
{
    panel.SetActive(true);

    // 1. Configurar el texto
    textoPregunta.text = $"¡Has obtenido un beneficio!\n<b>{carta.pregunta}</b>\n\n¿Quieres guardarla en tu inventario?";
    textoRespuesta1.text = ""; // Ocultamos textos de respuestas
    textoRespuesta2.text = "";
    textoRespuesta3.text = "";

    // 2. Ocultar botón innecesario
    boton3.gameObject.SetActive(false);
    
    // 3. Configurar el botón 1 para "Guardar"
    boton1.gameObject.SetActive(true);
    boton1.GetComponentInChildren<TextMeshProUGUI>().text = "Guardar";
    boton1.onClick.RemoveAllListeners();
    boton1.onClick.AddListener(() =>
    {
        // Intentará agregar la carta. El CartaManager gestionará si el inventario está lleno.
        CartaManager.instancia.IntentarAgregarCarta(carta);
        CerrarPanelDecision(onDecisionMade);
    });

    // 4. Configurar el botón 2 para "Cancelar" (descartar)
    boton2.gameObject.SetActive(true);
    boton2.GetComponentInChildren<TextMeshProUGUI>().text = "Cancelar";
    boton2.onClick.RemoveAllListeners();
    boton2.onClick.AddListener(() =>
    {
        Debug.Log("Carta de beneficio descartada.");
        CerrarPanelDecision(onDecisionMade);
    });
}

// ESTE MÉTODO ES EL MISMO, solo asegúrate de que esté
private void CerrarPanelDecision(System.Action onDecisionMade)
{
    panel.SetActive(false);
    boton3.gameObject.SetActive(true); // Restauramos la visibilidad
    onDecisionMade?.Invoke();
}



}