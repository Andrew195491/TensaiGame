using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;

public class CartaUI : MonoBehaviour
{
    [Header("Panel y textos")]
    public GameObject panel;
    public TextMeshProUGUI textoPregunta;
    public TextMeshProUGUI textoRespuesta1;
    public TextMeshProUGUI textoRespuesta2;
    public TextMeshProUGUI textoRespuesta3;

    [Header("Botones (solo jugador)")]
    public Button boton1;
    public Button boton2;
    public Button boton3;

    [Header("Tiempos")]
    public float delayCierreJugador = 1.5f;
    public float delayCierreBot = 1.8f;

    // Estados
    private Carta cartaActual;
    private Action<bool> onRespondida;

    // Colores
    private bool defaultsCacheados = false;
    private Color defaultBtn1, defaultBtn2, defaultBtn3;
    private Color defaultTxt1, defaultTxt2, defaultTxt3;

    private readonly Color colorCorrecto = new Color(0.2f, 0.8f, 0.2f);        // verde
    private readonly Color colorIncorrecto = new Color(0.9f, 0.25f, 0.25f);    // rojo
    private readonly Color colorCorrectaCuandoFalla = new Color(1f, 0.9f, 0.2f); // amarillo

    void Start()
    {
        panel.SetActive(false);

        // Listeners jugador
        boton1.onClick.AddListener(() => PulsarJugador(1));
        boton2.onClick.AddListener(() => PulsarJugador(2));
        boton3.onClick.AddListener(() => PulsarJugador(3));

        CachearColoresSiHaceFalta();
    }

    void CachearColoresSiHaceFalta()
    {
        if (defaultsCacheados) return;

        defaultBtn1 = boton1.targetGraphic ? boton1.targetGraphic.color : Color.white;
        defaultBtn2 = boton2.targetGraphic ? boton2.targetGraphic.color : Color.white;
        defaultBtn3 = boton3.targetGraphic ? boton3.targetGraphic.color : Color.white;

        defaultTxt1 = textoRespuesta1 ? textoRespuesta1.color : Color.white;
        defaultTxt2 = textoRespuesta2 ? textoRespuesta2.color : Color.white;
        defaultTxt3 = textoRespuesta3 ? textoRespuesta3.color : Color.white;

        defaultsCacheados = true;
    }

    void ResetVisual()
    {
        CachearColoresSiHaceFalta();

        if (boton1.targetGraphic) boton1.targetGraphic.color = defaultBtn1;
        if (boton2.targetGraphic) boton2.targetGraphic.color = defaultBtn2;
        if (boton3.targetGraphic) boton3.targetGraphic.color = defaultBtn3;

        if (textoRespuesta1) textoRespuesta1.color = defaultTxt1;
        if (textoRespuesta2) textoRespuesta2.color = defaultTxt2;
        if (textoRespuesta3) textoRespuesta3.color = defaultTxt3;

        // Rehabilitar botones para jugador
        boton1.interactable = true;
        boton2.interactable = true;
        boton3.interactable = true;
    }

    // ===================== JUGADOR =====================
    public void MostrarCartaJugador(Carta carta, Action<bool> callback)
    {
        cartaActual = carta;
        onRespondida = callback;

        ResetVisual();

        textoPregunta.text = carta.pregunta;
        textoRespuesta1.text = carta.respuesta1;
        textoRespuesta2.text = carta.respuesta2;
        textoRespuesta3.text = carta.respuesta3;

        panel.SetActive(true);
    }

    void PulsarJugador(int seleccion)
    {
        if (cartaActual == null)
        {
            CerrarInmediato();
            onRespondida?.Invoke(true);
            return;
        }

        // Desactivar interacción para evitar doble click
        boton1.interactable = false;
        boton2.interactable = false;
        boton3.interactable = false;

        bool correcta = (seleccion == cartaActual.respuestaCorrecta);

        // Pintar selección (verde o rojo)
        PintarSeleccion(seleccion, correcta);

        // Si falló, marcar también la correcta en amarillo
        if (!correcta)
            PintarRespuestaCorrectaAmarillo(cartaActual.respuestaCorrecta);

        StartCoroutine(CerrarTrasDelayJugador(correcta));
    }

    IEnumerator CerrarTrasDelayJugador(bool correcta)
    {
        yield return new WaitForSeconds(delayCierreJugador);
        CerrarInmediato();
        var cb = onRespondida;
        cartaActual = null;
        onRespondida = null;
        cb?.Invoke(correcta);
    }

    // ===================== BOT =====================
    public void MostrarCartaBot(Carta carta, int seleccion, bool esCorrecta, Action onCerrada)
    {
        cartaActual = carta;
        onRespondida = null; // no se usa para bot

        ResetVisual();

        // Rellenar textos
        textoPregunta.text = carta.pregunta;
        textoRespuesta1.text = carta.respuesta1;
        textoRespuesta2.text = carta.respuesta2;
        textoRespuesta3.text = carta.respuesta3;

        // Desactivar interacción
        boton1.interactable = false;
        boton2.interactable = false;
        boton3.interactable = false;

        // Pintar selección del bot
        PintarSeleccion(seleccion, esCorrecta);

        // Si falló, resaltar la correcta en amarillo
        if (!esCorrecta)
            PintarRespuestaCorrectaAmarillo(carta.respuestaCorrecta);

        panel.SetActive(true);
        StartCoroutine(CerrarTrasDelayBot(onCerrada));
    }

    IEnumerator CerrarTrasDelayBot(Action onCerrada)
    {
        yield return new WaitForSeconds(delayCierreBot);
        CerrarInmediato();
        onCerrada?.Invoke();
    }

    // ===================== PINTAR =====================
    void PintarSeleccion(int seleccionIdx, bool correcta)
    {
        Button selBtn = seleccionIdx == 1 ? boton1 : (seleccionIdx == 2 ? boton2 : boton3);
        TextMeshProUGUI selTxt = seleccionIdx == 1 ? textoRespuesta1 : (seleccionIdx == 2 ? textoRespuesta2 : textoRespuesta3);

        var color = correcta ? colorCorrecto : colorIncorrecto;
        if (selBtn.targetGraphic) selBtn.targetGraphic.color = color;
        if (selTxt) selTxt.color = color;
    }

    void PintarRespuestaCorrectaAmarillo(int correctaIdx)
    {
        Button corBtn = correctaIdx == 1 ? boton1 : (correctaIdx == 2 ? boton2 : boton3);
        TextMeshProUGUI corTxt = correctaIdx == 1 ? textoRespuesta1 : (correctaIdx == 2 ? textoRespuesta2 : textoRespuesta3);

        if (corBtn.targetGraphic) corBtn.targetGraphic.color = colorCorrectaCuandoFalla;
        if (corTxt) corTxt.color = colorCorrectaCuandoFalla;
    }

    // ===================== UTIL =====================
    void CerrarInmediato()
    {
        panel.SetActive(false);
        // (los colores se resetean la próxima vez con ResetVisual)
    }
}
