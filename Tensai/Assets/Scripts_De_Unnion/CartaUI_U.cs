// ============================================
// CartaUI_U.cs (Unificado, con modos y reset completo)
// ============================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class CartaUI_U : MonoBehaviour
{
    // -------- Modo de la UI (evita que el bot afecte al jugador) --------
    private enum Modo { None, PreguntaHumano, PreguntaBot, Beneficio, Penalidad }
    private Modo modoActual = Modo.None;

    [Header("Botones de respuestas (trivia)")]
    [SerializeField] private GameObject[] botonesRespuesta; // Asigna BotonRespuesta1/2/3

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

    [Header("Bot – feedback")]
    public Color colorResalteBot = new Color(1f, 0.92f, 0.3f);

    // ---- estado / callbacks ----
    private Carta_U cartaActual;
    private Action<bool> onRespondidaBool;            // preguntas (true=acierta)
    private Action onAceptarPenalidadCB;              // penalidad (Aceptar)
    private Action<bool> onBeneficioDecisionCB;       // beneficio (true=guardar)

    // cache de colores para resetear
    private bool defaultsCacheados = false;
    private Color defaultBtn1, defaultBtn2, defaultBtn3;
    private Color defaultTxt1, defaultTxt2, defaultTxt3;
    // debajo de los caches existentes
    private Color defaultBtnBenefGuardar, defaultBtnBenefDescartar, defaultBtnPenalAceptar;


    // colores de feedback
    private readonly Color colOK  = new(0.2f, 0.8f, 0.2f);
    private readonly Color colBAD = new(0.9f, 0.25f, 0.25f);
    private readonly Color colCOR = new(1f, 0.9f, 0.2f);

    // =================== Inicio ===================
    void Start()
    {
        if (panel) panel.SetActive(false);

        // clicks de respuestas
        if (boton1) boton1.onClick.AddListener(() => PulsarJugador(1));
        if (boton2) boton2.onClick.AddListener(() => PulsarJugador(2));
        if (boton3) boton3.onClick.AddListener(() => PulsarJugador(3));

        // penalidad
        if (btnAceptarPenalidad) btnAceptarPenalidad.onClick.AddListener(() =>
        {
            if (modoActual != Modo.Penalidad) return;
            var cb = onAceptarPenalidadCB; // guarda y limpia
            Cerrar();
            cb?.Invoke();                  // GameManager avanza turno al recibir esto
        });

        // beneficio
        if (btnDescartarBeneficio) btnDescartarBeneficio.onClick.AddListener(() =>
        {
            if (modoActual != Modo.Beneficio) return;
            var cb = onBeneficioDecisionCB;
            Cerrar();
            cb?.Invoke(false);             // descartar → avanza turno fuera
        });

        if (btnGuardarBeneficio) btnGuardarBeneficio.onClick.AddListener(() =>
        {
            if (modoActual != Modo.Beneficio) return;
            var cb = onBeneficioDecisionCB;
            Cerrar();
            cb?.Invoke(true);              // guardar → avanza turno fuera
        });

        CachearColores();
    }

    // =================== Helpers visuales ===================
    void SetRespuestasVisible(bool visible)
    {
        if (botonesRespuesta == null) return;
        foreach (var go in botonesRespuesta)
            if (go) go.SetActive(visible);
    }

    void CachearColores()
    {
        if (defaultsCacheados) return;
        defaultBtn1 = (boton1 && boton1.targetGraphic) ? boton1.targetGraphic.color : Color.white;
        defaultBtn2 = (boton2 && boton2.targetGraphic) ? boton2.targetGraphic.color : Color.white;
        defaultBtn3 = (boton3 && boton3.targetGraphic) ? boton3.targetGraphic.color : Color.white;
        defaultTxt1 = textoRespuesta1 ? textoRespuesta1.color : Color.white;
        defaultTxt2 = textoRespuesta2 ? textoRespuesta2.color : Color.white;
        defaultTxt3 = textoRespuesta3 ? textoRespuesta3.color : Color.white;
        defaultBtnBenefGuardar   = (btnGuardarBeneficio   && btnGuardarBeneficio.targetGraphic)   ? btnGuardarBeneficio.targetGraphic.color   : Color.white;
        defaultBtnBenefDescartar = (btnDescartarBeneficio && btnDescartarBeneficio.targetGraphic) ? btnDescartarBeneficio.targetGraphic.color : Color.white;
        defaultBtnPenalAceptar   = (btnAceptarPenalidad   && btnAceptarPenalidad.targetGraphic)   ? btnAceptarPenalidad.targetGraphic.color   : Color.white;
        defaultsCacheados = true;
    }

    void ResetVisual()
    {
        if (panelFondo) panelFondo.color = colorNeutral;

        if (bloqueRespuestas) bloqueRespuestas.SetActive(false);
        if (bloquePenalidad)  bloquePenalidad.SetActive(false);
        if (bloqueBeneficio)  bloqueBeneficio.SetActive(false);

        if (textoTituloEfecto) textoTituloEfecto.text = "";
        if (textoDescripcion)  textoDescripcion.text  = "";

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

        if (btnGuardarBeneficio)
        {
            btnGuardarBeneficio.interactable = true;
            if (btnGuardarBeneficio.targetGraphic)
                btnGuardarBeneficio.targetGraphic.color = defaultBtnBenefGuardar;
        }
        if (btnDescartarBeneficio)
        {
            btnDescartarBeneficio.interactable = true;
            if (btnDescartarBeneficio.targetGraphic)
                btnDescartarBeneficio.targetGraphic.color = defaultBtnBenefDescartar;
        }
        if (btnAceptarPenalidad)
        {
            btnAceptarPenalidad.interactable = true;
            if (btnAceptarPenalidad.targetGraphic)
                btnAceptarPenalidad.targetGraphic.color = defaultBtnPenalAceptar;
        }

        // por defecto, respuestas visibles (solo las ocultamos en beneficios/penalidades)
        SetRespuestasVisible(true);
    }

    void Abrir()
    {
        if (panel) panel.SetActive(true);
    }

    void Cerrar()
    {
        // reset completo para no contaminar turnos siguientes
        modoActual = Modo.None;
        cartaActual = null;
        onRespondidaBool = null;
        onAceptarPenalidadCB = null;
        onBeneficioDecisionCB = null;

        ResetVisual();
        if (panel) panel.SetActive(false);
    }

    // =================== Preguntas (jugador) ===================
    public void MostrarCartaJugador(Carta_U carta, Action<bool> callback)
    {
        Cerrar(); // asegura estado limpio
        modoActual = Modo.PreguntaHumano;

        cartaActual = carta;
        onRespondidaBool = callback;

        ResetVisual();
        if (bloqueRespuestas) bloqueRespuestas.SetActive(true);

        if (textoPregunta)   textoPregunta.text   = carta.pregunta;
        if (textoRespuesta1) textoRespuesta1.text = carta.respuesta1;
        if (textoRespuesta2) textoRespuesta2.text = carta.respuesta2;
        if (textoRespuesta3) textoRespuesta3.text = carta.respuesta3;

        // jugador puede pulsar
        if (boton1) boton1.interactable = true;
        if (boton2) boton2.interactable = true;
        if (boton3) boton3.interactable = true;

        SetRespuestasVisible(true);
        Abrir();
    }

    void PulsarJugador(int seleccion)
    {
        if (modoActual != Modo.PreguntaHumano || cartaActual == null) return;

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
        var cb = onRespondidaBool;
        Cerrar();                // limpia y cierra
        cb?.Invoke(resultado);   // GameManager decide retroceder y pasar turno
    }

    // =================== Preguntas (bot – visual) ===================
    public void MostrarCartaBot(Carta_U carta, int seleccion, bool esCorrecta, Action alCerrar)
    {
        Cerrar();
        modoActual = Modo.PreguntaBot;

        cartaActual = carta;

        ResetVisual();
        if (bloqueRespuestas) bloqueRespuestas.SetActive(true);

        if (textoPregunta)   textoPregunta.text   = carta.pregunta;
        if (textoRespuesta1) textoRespuesta1.text = carta.respuesta1;
        if (textoRespuesta2) textoRespuesta2.text = carta.respuesta2;
        if (textoRespuesta3) textoRespuesta3.text = carta.respuesta3;

        // desactivar inputs para que el jugador no pueda tocar
        if (boton1) boton1.interactable = false;
        if (boton2) boton2.interactable = false;
        if (boton3) boton3.interactable = false;

        // pintar resultado del bot
        PintarSeleccion(seleccion, esCorrecta);
        if (!esCorrecta) PintarCorrecta(carta.respuestaCorrecta);

        SetRespuestasVisible(true);
        Abrir();

        StartCoroutine(CerrarBotTras(delayCierreBot, alCerrar));
    }

    IEnumerator CerrarBotTras(float s, Action alCerrar)
    {
        yield return new WaitForSeconds(s);
        Cerrar();
        alCerrar?.Invoke();
    }

    // =================== Beneficios / Penalidades ===================
    public void MostrarBeneficio(string titulo, string descripcion, Action<bool> onDecision)
    {
        Cerrar();
        modoActual = Modo.Beneficio;

        ResetVisual();
        SetRespuestasVisible(false);                 // <- oculta 1/2/3
        if (panelFondo)        panelFondo.color = colorBeneficio;
        if (textoTituloEfecto) textoTituloEfecto.text = string.IsNullOrEmpty(titulo) ? "Beneficio" : titulo;
        if (textoDescripcion)  textoDescripcion.text  = descripcion;

        if (bloqueBeneficio)   bloqueBeneficio.SetActive(true);
        Abrir();
        onBeneficioDecisionCB = onDecision;
    }

    public void MostrarPenalidad(string titulo, string descripcion, Action onAceptar)
    {
        Cerrar();
        modoActual = Modo.Penalidad;

        ResetVisual();
        SetRespuestasVisible(false);                 // <- oculta 1/2/3
        if (panelFondo)        panelFondo.color = colorPenalidad;
        if (textoTituloEfecto) textoTituloEfecto.text = string.IsNullOrEmpty(titulo) ? "Penalidad" : titulo;
        if (textoDescripcion)  textoDescripcion.text  = descripcion;

        if (bloquePenalidad)   bloquePenalidad.SetActive(true);
        Abrir();
        onAceptarPenalidadCB = onAceptar;
    }

    // ---- versiones “auto” para BOT ----
    public void MostrarBeneficioBotAutoGuardar(Carta_U carta, float delaySeg, Action onCerrado)
    {
        Cerrar();
        modoActual = Modo.Beneficio;

        ResetVisual();
        SetRespuestasVisible(false);
        if (panelFondo)        panelFondo.color = colorBeneficio;
        if (textoTituloEfecto) textoTituloEfecto.text = "¡Carta de Beneficio!";
        if (textoDescripcion)  textoDescripcion.text  = carta != null ? carta.pregunta : "";

        if (bloqueBeneficio)   bloqueBeneficio.SetActive(true);

        // desactivar clicks y resaltar GUARDAR
        if (btnGuardarBeneficio)   { btnGuardarBeneficio.interactable = false;   if (btnGuardarBeneficio.targetGraphic)   btnGuardarBeneficio.targetGraphic.color = colorResalteBot; }
        if (btnDescartarBeneficio) { btnDescartarBeneficio.interactable = false; }

        Abrir();
        StartCoroutine(AutoCerrar(delaySeg, onCerrado));
    }

    public void MostrarPenalidadBotAutoAceptar(Carta_U carta, float delaySeg, Action onCerrado)
    {
        Cerrar();
        modoActual = Modo.Penalidad;

        ResetVisual();
        SetRespuestasVisible(false);
        if (panelFondo)        panelFondo.color = colorPenalidad;
        if (textoTituloEfecto) textoTituloEfecto.text = "¡Carta de Penalidad!";
        if (textoDescripcion)  textoDescripcion.text  = carta != null ? carta.pregunta : "";

        if (bloquePenalidad)   bloquePenalidad.SetActive(true);

        if (btnAceptarPenalidad) { btnAceptarPenalidad.interactable = false; if (btnAceptarPenalidad.targetGraphic) btnAceptarPenalidad.targetGraphic.color = colorResalteBot; }

        Abrir();
        StartCoroutine(AutoCerrar(delaySeg, onCerrado));
    }

    IEnumerator AutoCerrar(float s, Action onCerrado)
    {
        yield return new WaitForSeconds(s);
        Cerrar();
        onCerrado?.Invoke(); // GameManager avanza turno al recibir este callback
    }

    // =================== utilidades de feedback ===================
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
}
