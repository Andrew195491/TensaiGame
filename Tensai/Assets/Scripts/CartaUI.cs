using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

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

    private Carta cartaActual;
    private Action<bool> onRespondida;

    void Start()
    {
        panel.SetActive(false);

        boton1.onClick.AddListener(() => Pulsar(1));
        boton2.onClick.AddListener(() => Pulsar(2));
        boton3.onClick.AddListener(() => Pulsar(3));
    }

    public void MostrarCarta(Carta carta, Action<bool> callback)
    {
        cartaActual = carta;
        onRespondida = callback;

        textoPregunta.text = carta.pregunta;
        textoRespuesta1.text = carta.respuesta1;
        textoRespuesta2.text = carta.respuesta2;
        textoRespuesta3.text = carta.respuesta3;

        panel.SetActive(true);
    }

    void Pulsar(int seleccion)
    {
        if (cartaActual == null) { panel.SetActive(false); return; }

        bool correcta = (seleccion == cartaActual.respuestaCorrecta);
        Debug.Log(correcta ? "✅ ¡Respuesta correcta!" : "❌ Respuesta incorrecta.");

        panel.SetActive(false);

        var cb = onRespondida;
        cartaActual = null;
        onRespondida = null;

        cb?.Invoke(correcta);
    }
}
