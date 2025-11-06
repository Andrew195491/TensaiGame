// ============================================
// CartaUI_U.cs (Unificado y Corregido)
// ============================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

/// <summary>
/// Sistema avanzado de interfaz de cartas unificado.
/// Combina la funcionalidad clásica de preguntas y mensajes de `CartaUI`
/// con la interfaz visual mejorada y lógica extendida de `CartaUI2`.
/// 
/// Soporta:
/// - Preguntas interactivas (jugador y bots)
/// - Cartas de beneficios y penalidades
/// - Mensajes automáticos o informativos
/// - Decisiones de inventario (guardar o descartar)
/// 
/// ACTUALIZADO: Compatible con Carta_U
/// </summary>
public class CartaUI_U : MonoBehaviour
{
    // ============================================
    // SECCIÓN 1: ELEMENTOS DE INTERFAZ
    // ============================================

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

    [Header("Vista efecto / mensaje")]
    public TextMeshProUGUI textoTituloEfecto;
    public TextMeshProUGUI textoDescripcion;

    [Header("Botones efecto")]
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

    // ============================================
    // SECCIÓN 2: VARIABLES INTERNAS
    // ============================================

    private Carta_U cartaActual; // CAMBIADO: Carta2 → Carta_U
    private Action<bool> onRespondida;
    private Action onAceptarPenalidadCB;
    private Action<bool> onBeneficioDecisionCB;

    private bool defaultsCacheados = false;
    private Color defaultBtn1, defaultBtn2, defaultBtn3;
    private Color defaultTxt1, defaultTxt2, defaultTxt3;

    private readonly Color colOK = new Color(0.2f, 0.8f, 0.2f);
    private readonly Color colBAD = new Color(0.9f, 0.25f, 0.25f);
    private readonly Color colCOR = new Color(1f, 0.9f, 0.2f);

    // ============================================
    // SECCIÓN 3: INICIALIZACIÓN
    // ============================================

    void Start()
    {
        if (panel) panel.SetActive(false);

        // Listeners de botones de preguntas
        if (boton1) boton1.onClick.AddListener(() => PulsarJugador(1));
        if (boton2) boton2.onClick.AddListener(() => PulsarJugador(2));
        if (boton3) boton3.onClick.AddListener(() => PulsarJugador(3));

        // Botones de efectos
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

    // ============================================
    // SECCIÓN 4: CACHE Y RESETEO VISUAL
    // ============================================

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
        if (textoTituloEfecto) textoTituloEfecto.text = "";
        if (textoDescripcion) textoDescripcion.text = "";

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

    // ============================================
    // SECCIÓN 5: CARTAS DE PREGUNTA
    // ============================================

    public void MostrarCartaJugador(Carta_U carta, Action<bool> callback) // CAMBIADO: Carta2 → Carta_U
    {
        cartaActual = carta;
        onRespondida = callback;
        ResetVisual();
        if (bloqueRespuestas) bloqueRespuestas.SetActive(true);
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
            Cerrar();
            onRespondida?.Invoke(true);
            return;
        }

        boton1.interactable = false;
        boton2.interactable = false;
        boton3.interactable = false;

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

    // ============================================
    // SECCIÓN 6: CARTAS DE BOT
    // ============================================

    public void MostrarCartaBot(Carta_U carta, int seleccion, bool esCorrecta, Action alCerrar) // CAMBIADO: Carta2 → Carta_U
    {
        cartaActual = carta;
        ResetVisual();
        bloqueRespuestas.SetActive(true);
        textoPregunta.text = carta.pregunta;
        textoRespuesta1.text = carta.respuesta1;
        textoRespuesta2.text = carta.respuesta2;
        textoRespuesta3.text = carta.respuesta3;
        boton1.interactable = false;
        boton2.interactable = false;
        boton3.interactable = false;
        PintarSeleccion(seleccion, esCorrecta);
        if (!esCorrecta) PintarCorrecta(carta.respuestaCorrecta);
        panel.SetActive(true);
        StartCoroutine(CerrarBotTras(delayCierreBot, alCerrar));
    }

    IEnumerator CerrarBotTras(float s, Action alCerrar)
    {
        yield return new WaitForSeconds(s);
        Cerrar();
        alCerrar?.Invoke();
    }

    // ============================================
    // SECCIÓN 7: BENEFICIOS Y PENALIDADES
    // ============================================

    public void MostrarPenalidad(string titulo, string descripcion, Action onAceptar)
    {
        ResetVisual();
        panelFondo.color = colorPenalidad;
        textoTituloEfecto.text = string.IsNullOrEmpty(titulo) ? "Penalidad" : titulo;
        textoDescripcion.text = descripcion;
        onAceptarPenalidadCB = onAceptar;
        bloquePenalidad.SetActive(true);
        panel.SetActive(true);
    }

    public void MostrarBeneficio(string titulo, string descripcion, Action<bool> onDecision)
    {
        ResetVisual();
        panelFondo.color = colorBeneficio;
        textoTituloEfecto.text = string.IsNullOrEmpty(titulo) ? "Beneficio" : titulo;
        textoDescripcion.text = descripcion;
        onBeneficioDecisionCB = onDecision;
        bloqueBeneficio.SetActive(true);
        panel.SetActive(true);
    }

    public void MostrarEfectoAuto(string titulo, string descripcion, bool esBeneficio, Action onCerrada)
    {
        ResetVisual();
        panelFondo.color = esBeneficio ? colorBeneficio : colorPenalidad;
        textoTituloEfecto.text = titulo;
        textoDescripcion.text = descripcion;
        panel.SetActive(true);
        StartCoroutine(AutoCerrarEfecto(onCerrada));
    }

    IEnumerator AutoCerrarEfecto(Action onCerrada)
    {
        yield return new WaitForSeconds(delayEfectoAuto);
        Cerrar();
        onCerrada?.Invoke();
    }

    // ============================================
    // SECCIÓN 8: FUNCIONES VISUALES
    // ============================================

    void PintarSeleccion(int idx, bool ok)
    {
        Button b = idx == 1 ? boton1 : idx == 2 ? boton2 : boton3;
        TextMeshProUGUI t = idx == 1 ? textoRespuesta1 : idx == 2 ? textoRespuesta2 : textoRespuesta3;
        var c = ok ? colOK : colBAD;
        if (b && b.targetGraphic) b.targetGraphic.color = c;
        if (t) t.color = c;
    }

    void PintarCorrecta(int idx)
    {
        Button b = idx == 1 ? boton1 : idx == 2 ? boton2 : boton3;
        TextMeshProUGUI t = idx == 1 ? textoRespuesta1 : idx == 2 ? textoRespuesta2 : textoRespuesta3;
        if (b && b.targetGraphic) b.targetGraphic.color = colCOR;
        if (t) t.color = colCOR;
    }

    private void Cerrar()
    {
        if (panel) panel.SetActive(false);
    }
}