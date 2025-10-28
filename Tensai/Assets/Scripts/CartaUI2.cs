
// ============================================
// CartaUI2.cs (Actualizado)
// ============================================
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;

public class CartaUI2 : MonoBehaviour
{
    [Header("Panel base")]
    public GameObject panel;
    public Image panelFondo;

    [Header("Vista pregunta")]
    public GameObject bloqueRespuestas;
    public TextMeshProUGUI textoPregunta;
    public TextMeshProUGUI textoRespuesta1;
    public TextMeshProUGUI textoRespuesta2;
    public TextMeshProUGUI textoRespuesta3;
    public Button boton1;
    public Button boton2;
    public Button boton3;

    [Header("Vista efecto (texto)")]
    public TextMeshProUGUI textoTituloEfecto;
    public TextMeshProUGUI textoDescripcion;

    [Header("Vista efecto (botones)")]
    public GameObject bloquePenalidad;
    public Button btnAceptarPenalidad;
    public GameObject bloqueBeneficio;
    public Button btnDescartarBeneficio;
    public Button btnGuardarBeneficio;

    [Header("Colores y tiempos")]
    public Color colorBeneficio = new Color(0.2f, 0.6f, 0.2f);
    public Color colorPenalidad = new Color(0.75f, 0.25f, 0.25f);
    public Color colorNeutral = Color.white;
    public float delayCierreJugador = 1.5f;
    public float delayCierreBot = 1.8f;
    public float delayEfectoAuto = 1.6f;

    private Carta2 cartaActual;
    private Action<bool> onRespondida;
    private Action onAceptarPenalidadCB;
    private Action<bool> onBeneficioDecisionCB;

    private bool defaultsCacheados = false;
    private Color defaultBtn1, defaultBtn2, defaultBtn3;
    private Color defaultTxt1, defaultTxt2, defaultTxt3;

    private readonly Color colOK = new Color(0.2f, 0.8f, 0.2f);
    private readonly Color colBAD = new Color(0.9f, 0.25f, 0.25f);
    private readonly Color colCOR = new Color(1f, 0.9f, 0.2f);

    void Start()
    {
        if (panel) panel.SetActive(false);
        if (boton1) boton1.onClick.AddListener(() => PulsarJugador(1));
        if (boton2) boton2.onClick.AddListener(() => PulsarJugador(2));
        if (boton3) boton3.onClick.AddListener(() => PulsarJugador(3));

        if (btnAceptarPenalidad) btnAceptarPenalidad.onClick.AddListener(() =>
        {
            Cerrar();
            var cb = onAceptarPenalidadCB;
            onAceptarPenalidadCB = null;
            cb?.Invoke();
        });

        if (btnDescartarBeneficio) btnDescartarBeneficio.onClick.AddListener(() =>
        {
            Cerrar();
            var cb = onBeneficioDecisionCB;
            onBeneficioDecisionCB = null;
            cb?.Invoke(false);
        });

        if (btnGuardarBeneficio) btnGuardarBeneficio.onClick.AddListener(() =>
        {
            Cerrar();
            var cb = onBeneficioDecisionCB;
            onBeneficioDecisionCB = null;
            cb?.Invoke(true);
        });

        CachearColores();
    }

    void CachearColores()
    {
        if (defaultsCacheados) return;
        if (boton1 && boton1.targetGraphic) defaultBtn1 = boton1.targetGraphic.color; else defaultBtn1 = Color.white;
        if (boton2 && boton2.targetGraphic) defaultBtn2 = boton2.targetGraphic.color; else defaultBtn2 = Color.white;
        if (boton3 && boton3.targetGraphic) defaultBtn3 = boton3.targetGraphic.color; else defaultBtn3 = Color.white;
        defaultTxt1 = textoRespuesta1 ? textoRespuesta1.color : Color.white;
        defaultTxt2 = textoRespuesta2 ? textoRespuesta2.color : Color.white;
        defaultTxt3 = textoRespuesta3 ? textoRespuesta3.color : Color.white;
        defaultsCacheados = true;
    }

    void ResetVisual()
    {
        if (panelFondo) panelFondo.color = colorNeutral;
        if (bloqueRespuestas) bloqueRespuestas.SetActive(false);
        if (bloquePenalidad) bloquePenalidad.SetActive(false);
        if (bloqueBeneficio) bloqueBeneficio.SetActive(false);
        if (textoTituloEfecto) textoTituloEfecto.text = string.Empty;
        if (textoDescripcion) textoDescripcion.text = string.Empty;

        CachearColores();
        if (boton1 && boton1.targetGraphic) boton1.targetGraphic.color = defaultBtn1;
        if (boton2 && boton2.targetGraphic) boton2.targetGraphic.color = defaultBtn2;
        if (boton3 && boton3.targetGraphic) boton3.targetGraphic.color = defaultBtn3;
        if (textoRespuesta1) textoRespuesta1.color = defaultTxt1;
        if (textoRespuesta2) textoRespuesta2.color = defaultTxt2;
        if (textoRespuesta3) textoRespuesta3.color = defaultTxt3;
        if (boton1) boton1.interactable = true;
        if (boton2) boton2.interactable = true;
        if (boton3) boton3.interactable = true;
    }

    public void MostrarCartaJugador(Carta2 carta, Action<bool> callback)
    {
        cartaActual = carta;
        onRespondida = callback;
        ResetVisual();
        if (bloqueRespuestas) bloqueRespuestas.SetActive(true);
        if (textoPregunta) textoPregunta.text = carta.pregunta;
        if (textoRespuesta1) textoRespuesta1.text = carta.respuesta1;
        if (textoRespuesta2) textoRespuesta2.text = carta.respuesta2;
        if (textoRespuesta3) textoRespuesta3.text = carta.respuesta3;
        if (panel) panel.SetActive(true);
    }

    void PulsarJugador(int seleccion)
    {
        if (cartaActual == null)
        {
            Cerrar();
            onRespondida?.Invoke(true);
            return;
        }
        if (boton1) boton1.interactable = false;
        if (boton2) boton2.interactable = false;
        if (boton3) boton3.interactable = false;
        bool correcta = (seleccion == cartaActual.respuestaCorrecta);
        PintarSeleccion(seleccion, correcta);
        if (!correcta) PintarCorrecta(cartaActual.respuestaCorrecta);
        StartCoroutine(CerrarTras(delayCierreJugador, correcta));
    }

    IEnumerator CerrarTras(float s, bool resultado)
    {
        yield return new WaitForSeconds(s);
        Cerrar();
        var cb = onRespondida;
        cartaActual = null;
        onRespondida = null;
        cb?.Invoke(resultado);
    }

    public void MostrarCartaBot(Carta2 carta, int seleccion, bool esCorrecta, Action alCerrar)
    {
        cartaActual = carta;
        onRespondida = null;
        ResetVisual();
        if (bloqueRespuestas) bloqueRespuestas.SetActive(true);
        if (textoPregunta) textoPregunta.text = carta.pregunta;
        if (textoRespuesta1) textoRespuesta1.text = carta.respuesta1;
        if (textoRespuesta2) textoRespuesta2.text = carta.respuesta2;
        if (textoRespuesta3) textoRespuesta3.text = carta.respuesta3;
        if (boton1) boton1.interactable = false;
        if (boton2) boton2.interactable = false;
        if (boton3) boton3.interactable = false;
        PintarSeleccion(seleccion, esCorrecta);
        if (!esCorrecta) PintarCorrecta(carta.respuestaCorrecta);
        if (panel) panel.SetActive(true);
        StartCoroutine(CerrarBotTras(delayCierreBot, alCerrar));
    }

    IEnumerator CerrarBotTras(float s, Action alCerrar)
    {
        yield return new WaitForSeconds(s);
        Cerrar();
        alCerrar?.Invoke();
    }

    public void MostrarPenalidadInteractiva(string titulo, string descripcion, Action onAceptar)
    {
        ResetVisual();
        if (panelFondo) panelFondo.color = colorPenalidad;
        if (textoTituloEfecto) textoTituloEfecto.text = string.IsNullOrEmpty(titulo) ? "Penalidad" : titulo;
        if (textoDescripcion) textoDescripcion.text = string.IsNullOrEmpty(descripcion) ? "" : descripcion;
        onAceptarPenalidadCB = onAceptar;
        if (bloquePenalidad) bloquePenalidad.SetActive(true);
        if (panel) panel.SetActive(true);
    }

    public void MostrarBeneficioInteractivo(string titulo, string descripcion, Action<bool> onDecision)
    {
        ResetVisual();
        if (panelFondo) panelFondo.color = colorBeneficio;
        if (textoTituloEfecto) textoTituloEfecto.text = string.IsNullOrEmpty(titulo) ? "Beneficio" : titulo;
        if (textoDescripcion) textoDescripcion.text = string.IsNullOrEmpty(descripcion) ? "" : descripcion;
        onBeneficioDecisionCB = onDecision;
        if (bloqueBeneficio) bloqueBeneficio.SetActive(true);
        if (panel) panel.SetActive(true);
    }

    public void MostrarEfectoAuto(string titulo, string descripcion, bool esBeneficio, Action onCerrada)
    {
        ResetVisual();
        if (panelFondo) panelFondo.color = esBeneficio ? colorBeneficio : colorPenalidad;
        if (textoTituloEfecto) textoTituloEfecto.text = string.IsNullOrEmpty(titulo) ? (esBeneficio ? "Beneficio" : "Penalidad") : titulo;
        if (textoDescripcion) textoDescripcion.text = string.IsNullOrEmpty(descripcion) ? "" : descripcion;
        if (panel) panel.SetActive(true);
        StartCoroutine(AutoCerrarEfecto(onCerrada));
    }

    IEnumerator AutoCerrarEfecto(Action onCerrada)
    {
        yield return new WaitForSeconds(delayEfectoAuto);
        Cerrar();
        onCerrada?.Invoke();
    }

    void PintarSeleccion(int idx, bool ok)
    {
        Button b = idx == 1 ? boton1 : (idx == 2 ? boton2 : boton3);
        TextMeshProUGUI t = idx == 1 ? textoRespuesta1 : (idx == 2 ? textoRespuesta2 : textoRespuesta3);
        var c = ok ? colOK : colBAD;
        if (b && b.targetGraphic) b.targetGraphic.color = c;
        if (t) t.color = c;
    }

    void PintarCorrecta(int idx)
    {
        Button b = idx == 1 ? boton1 : (idx == 2 ? boton2 : boton3);
        TextMeshProUGUI t = idx == 1 ? textoRespuesta1 : (idx == 2 ? textoRespuesta2 : textoRespuesta3);
        if (b && b.targetGraphic) b.targetGraphic.color = colCOR;
        if (t) t.color = colCOR;
    }

    private void Cerrar()
    {
        if (panel) panel.SetActive(false);
    }
}
