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


    void EvaluarRespuesta(int respuestaSeleccionada)
    {
        if (respuestaSeleccionada == respuestaCorrectaActual)
        {
            Debug.Log("✅ ¡Respuesta correcta!");
        }
        else
        {
            Debug.Log("❌ Respuesta incorrecta.");
        }

        panel.SetActive(false);        // Ocultar carta
        if (dado != null)
            dado.BloquearDado(false); // 🔓 Desbloquear el dado
    }
}
