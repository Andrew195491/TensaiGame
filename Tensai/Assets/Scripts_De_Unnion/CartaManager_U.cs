using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gestor central de cartas: trivia + especiales (beneficios/penalidades).
/// - Muestra UI con CartaUI_U.
/// - Para efectos que mueven al peón, usa "modo silencioso" (no dispara nuevas cartas):
///   envuelve el movimiento con ignoreLandingEffects en MovePlayer_U.
/// - Los bots: guardan SIEMPRE beneficios y aceptan SIEMPRE penalidades con resaltado y delay.
/// </summary>
public class CartaManager_U : MonoBehaviour
{
    // =========================
    // 1) Trivia (JSON)
    // =========================
    [Header("Archivo JSON de Preguntas (Trivia)")]
    [Tooltip("Si se deja vacío, cargará Resources/cartas.json")]
    public TextAsset cartasTriviaJSON;

    [Header("Game flow")]
    public GameManager_U gameManager;

    [Header("UI Trivia")]
    public CartaUI_U cartaUI;

    private List<Carta_U> historia = new();
    private List<Carta_U> geografia = new();
    private List<Carta_U> ciencia   = new();

    // barajas para evitar repeticiones hasta agotar
    private readonly Dictionary<Tile_U.Categoria, List<Carta_U>> baraja = new();

    // =========================
    // 2) Especiales (JSON)
    // =========================
    [Header("Archivo JSON de Cartas Especiales")]
    [Tooltip("Contiene las cartas de beneficios y penalidades.")]
    public TextAsset cartasEspecialesJSON;

    [Header("Cartas especiales - Beneficios")]
    public List<Carta_U> benefits = new();

    [Header("Cartas especiales - Penalidades")]
    public List<Carta_U> penalty = new();

    [Header("UI y Controladores")]
    public DiceController_U dadoController;
    public BonusUI_U bonusUI;
    public ReplacementUI_U replacementUI;

    // =========================
    // 3) Inventario
    // =========================
    [Header("Inventario del jugador")]
    public List<Carta_U> storage = new();
    public int maxStorage = 3;

    public static CartaManager_U instancia;

    [Header("Tiempos BOT")]
    [Tooltip("Tiempo que se muestra la UI de especial para bots antes de cerrar.")]
    public float botSpecialDelay = 1.5f;

    // =========================
    // 4) Ciclo de vida
    // =========================
    void Awake()
    {
        instancia = this;
        if (gameManager == null) gameManager = FindObjectOfType<GameManager_U>();
        CargarCartasTriviaDesdeJSON();
        CargarCartasEspecialesDesdeJSON();
        InicializarBarajas();
    }

    void Start()
    {
        storage.Clear();
        ActualizarUIStorage();
    }

    // =========================
    // 5) Carga de JSON
    // =========================
    void CargarCartasTriviaDesdeJSON()
    {
        try
        {
            string json = cartasTriviaJSON != null
                ? cartasTriviaJSON.text
                : Resources.Load<TextAsset>("cartas")?.text;

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("❌ No se encontró el archivo JSON de trivia.");
                return;
            }

            var db = JsonUtility.FromJson<CartasDB_U>(json);
            if (db == null)
            {
                Debug.LogError("❌ Error al parsear el JSON de trivia.");
                return;
            }

            historia  = db.historia  != null ? new List<Carta_U>(db.historia)  : new();
            geografia = db.geografia != null ? new List<Carta_U>(db.geografia) : new();
            ciencia   = db.ciencia   != null ? new List<Carta_U>(db.ciencia)   : new();

            Debug.Log($"✅ Trivia: H={historia.Count} G={geografia.Count} C={ciencia.Count}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al cargar cartas trivia: {e.Message}");
        }
    }

    void CargarCartasEspecialesDesdeJSON()
    {
        if (cartasEspecialesJSON == null)
        {
            Debug.LogWarning("⚠️ No hay JSON de cartas especiales.");
            return;
        }

        try
        {
            var root = JsonUtility.FromJson<CartasEspecialesRoot_U>(cartasEspecialesJSON.text);
            if (root == null || root.Cards == null || root.Cards.Count == 0)
            {
                Debug.LogError("❌ JSON de especiales vacío o mal formado.");
                return;
            }

            var data = root.Cards[0];
            benefits = data.benefits ?? new();
            penalty  = data.penalty  ?? new();

            Debug.Log($"✅ Especiales: Beneficios={benefits.Count}, Penalidades={penalty.Count}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error al cargar cartas especiales: {e.Message}");
        }
    }

    void InicializarBarajas()
    {
        baraja.Clear();
        baraja[Tile_U.Categoria.Historia]  = new List<Carta_U>(historia);
        baraja[Tile_U.Categoria.Geografia] = new List<Carta_U>(geografia);
        baraja[Tile_U.Categoria.Ciencia]   = new List<Carta_U>(ciencia);
    }

    // =========================
    // 6) Trivia
    // =========================
    Carta_U SacarCarta(Tile_U.Categoria cat)
    {
        if (!baraja.ContainsKey(cat)) baraja[cat] = new();

        if (baraja[cat].Count == 0)
        {
            switch (cat)
            {
                case Tile_U.Categoria.Historia:  baraja[cat].AddRange(historia);  break;
                case Tile_U.Categoria.Geografia: baraja[cat].AddRange(geografia); break;
                case Tile_U.Categoria.Ciencia:   baraja[cat].AddRange(ciencia);   break;
            }
        }

        if (baraja[cat].Count == 0) return null;

        int idx = UnityEngine.Random.Range(0, baraja[cat].Count);
        var carta = baraja[cat][idx];
        baraja[cat].RemoveAt(idx);
        return carta;
    }

    public void HacerPregunta(Tile_U.Categoria categoria, bool esHumano, float probAciertoBot, Action<bool> onRespondida)
    {
        var carta = SacarCarta(categoria);
        if (carta == null)
        {
            onRespondida?.Invoke(true);
            return;
        }

        if (cartaUI == null)
        {
            bool correctSim = esHumano || UnityEngine.Random.value < probAciertoBot;
            onRespondida?.Invoke(correctSim);
            return;
        }

        if (esHumano)
        {
            cartaUI.MostrarCartaJugador(carta, onRespondida);
        }
        else
        {
            bool correcta  = UnityEngine.Random.value < probAciertoBot;
            int  seleccion = correcta ? carta.respuestaCorrecta : OpcionAleatoriaDistintaDe(carta.respuestaCorrecta);
            cartaUI.MostrarCartaBot(carta, seleccion, correcta, () => onRespondida?.Invoke(correcta));
        }
    }

    int OpcionAleatoriaDistintaDe(int correcta)
    {
        int pick;
        do { pick = UnityEngine.Random.Range(1, 4); } while (pick == correcta);
        return pick;
    }

    public IEnumerator EjecutarBeneficioAplicando(MovePlayer_U pj, Carta_U carta)
{
    if (pj == null || carta == null) yield break;

    switch (carta.accion)
    {
        case "Avanza1":
            yield return pj.JumpMultipleTimes(1);
            break;

        case "Avanza2":
            yield return pj.JumpMultipleTimes(2);
            break;

        case "Avanza3":
            yield return pj.JumpMultipleTimes(3);
            break;

        case "TeletransporteAdelante":
            yield return pj.JumpMultipleTimes(UnityEngine.Random.Range(5, 10));
            break;

        case "RepiteTurno":
            // Si quieres que el bot repita tirada automáticamente:
            // Busca el GameManager y marca repetir turno para este peón
            var gm = FindObjectOfType<GameManager_U>();
            if (gm != null) gm.MarcarRepetirTirada(pj);
            break;

        // Añade aquí otros beneficios que tengas (DobleDado, ElegirDado, etc.)
        default:
            Debug.LogWarning($"[CartaManager_U] Beneficio '{carta.accion}' no implementado en EjecutarBeneficioAplicando.");
            break;
    }

    // IMPORTANTE: no resuelvas aquí la casilla en la que caiga tras moverse.
    // La resolución de casilla solo ocurre en GameManager_U.ResolverTurno() tras la tirada normal.
}

    // =========================
    // 7) Especiales (para GameManager_U)
    // =========================
    public IEnumerator EjecutarBeneficioCasilla(MovePlayer_U pj, bool esHumano)
    {
        var carta = ObtenerCartaBeneficioAleatoria();
        if (carta == null) yield break;

        if (esHumano || cartaUI == null)
        {
            bool decidido = false;
            cartaUI?.MostrarBeneficio(
                "¡Carta de Beneficio!",
                carta.pregunta,
                guardar =>
                {
                    if (guardar) IntentarAgregarCarta(carta); // -> va al storage del jugador
                    else         EjecutarBeneficio(carta, pj); // -> efecto inmediato
                    decidido = true;
                });

            while (!decidido) yield return null;
        }
        else
        {
            // BOT: visualizar “Guardar” en amarillo y auto-cerrar
            cartaUI?.MostrarBeneficioBotAutoGuardar(carta, botSpecialDelay, () => { });
            yield return new WaitForSeconds(botSpecialDelay);

            // En lugar de storage del jugador, lo guardamos para ESTE bot
            if (gameManager != null)
                gameManager.QueueBotBenefit(pj, carta);
            else
                Debug.LogWarning("GameManager_U no asignado en CartaManager_U: no se pudo encolar beneficio del bot.");
        }
    }


    public IEnumerator EjecutarPenalidadCasilla(MovePlayer_U pj, bool esHumano)
    {
        var carta = ObtenerCartaPenalidadAleatoria();
        if (carta == null) yield break;

        if (cartaUI == null)
        {
            yield return StartCoroutine(EjecutarPenalidadAplicando(pj, carta));
            yield break;
        }

        if (esHumano)
        {
            bool acepto = false;
            cartaUI.MostrarPenalidad(
                "¡Carta de Penalidad!",
                carta.pregunta,
                () =>
                {
                    acepto = true;
                    instancia.StartCoroutine(instancia.EjecutarPenalidadAplicando(pj, carta, () => { }));
                });

            while (!acepto) yield return null;
        }
        else
        {
            // BOT: SIEMPRE aceptar con feedback + delay
            bool fin = false;
            cartaUI.MostrarPenalidadBotAutoAceptar(carta, botSpecialDelay, () => fin = true);
            while (!fin) yield return null;
            yield return StartCoroutine(EjecutarPenalidadAplicando(pj, carta));
        }
    }

    // =========================
    // 8) Aplicación real de efectos
    //     (moviendo en modo silencioso)
    // =========================
    public void EjecutarBeneficio(Carta_U carta, MovePlayer_U jugador)
    {
        // Método legacy usado por BonusUI_U -> UsarCartaDelStorage
        instancia.StartCoroutine(instancia.EjecutarBeneficioAplicando(jugador, carta));
    }

    public void EjecutarPenalidad(Carta_U carta, MovePlayer_U jugador)
    {
        // Método legacy usado por BonusUI_U -> UsarCartaDelStorage
        instancia.StartCoroutine(instancia.EjecutarPenalidadAplicando(jugador, carta));
    }

    IEnumerator EjecutarBeneficioAplicando(MovePlayer_U j, Carta_U carta, Action onDone = null)
    {
        if (j == null || carta == null) { onDone?.Invoke(); yield break; }

        switch (carta.accion)
        {
            case "Avanza1": yield return StartCoroutine(MoverSinEfectos(j, j.JumpMultipleTimes(1))); break;
            case "Avanza2": yield return StartCoroutine(MoverSinEfectos(j, j.JumpMultipleTimes(2))); break;
            case "Avanza3": yield return StartCoroutine(MoverSinEfectos(j, j.JumpMultipleTimes(3))); break;

            case "TeletransporteAdelante":
                yield return StartCoroutine(MoverSinEfectos(j, j.JumpMultipleTimes(UnityEngine.Random.Range(5, 10))));
                break;

            case "RepiteTurno":
                // Si gestionas repetir turno, hazlo desde GameManager_U.
                // Aquí no hay movimiento.
                break;

            // Añade aquí otros efectos no-movimiento si los tienes.
            default:
                Debug.Log($"⚠️ Beneficio no reconocido: {carta.accion}");
                break;
        }

        onDone?.Invoke();
    }

    IEnumerator EjecutarPenalidadAplicando(MovePlayer_U j, Carta_U carta, Action onDone = null)
    {
        if (j == null || carta == null) { onDone?.Invoke(); yield break; }

        switch (carta.accion)
        {
            case "Retrocede1": yield return StartCoroutine(MoverSinEfectos(j, j.Retroceder(1))); break;
            case "Retrocede2": yield return StartCoroutine(MoverSinEfectos(j, j.Retroceder(2))); break;
            case "Retrocede3": yield return StartCoroutine(MoverSinEfectos(j, j.Retroceder(3))); break;

            case "IrSalida":   yield return StartCoroutine(MoverSinEfectos(j, j.IrACasilla(0))); break;

            case "PierdeTurno":
                // si controlas el "pierde turno" en GameManager_U, márcalo allí.
                break;

            default:
                Debug.Log($"⚠️ Penalidad no reconocida: {carta.accion}");
                break;
        }

        onDone?.Invoke();
    }

    /// Envuelve cualquier movimiento para que NO dispare cartas al aterrizar.
    IEnumerator MoverSinEfectos(MovePlayer_U j, IEnumerator movimiento)
    {
        if (j == null || movimiento == null) yield break;
        j.ignoreLandingEffects = true;    // <- tu MovePlayer_U debe tener este flag
        yield return StartCoroutine(movimiento);
        j.ignoreLandingEffects = false;
    }

    // =========================
    // 9) Inventario
    // =========================
// Antes:
// public void IntentarAgregarCarta(Carta_U nuevaCarta)

public bool IntentarAgregarCarta(Carta_U nuevaCarta)
{
    if (nuevaCarta == null) return false;

    // hay espacio: guardo y devuelvo true
    if (storage.Count < maxStorage)
    {
        storage.Add(nuevaCarta);
        ActualizarUIStorage();
        Debug.Log($"💾 Carta guardada: {nuevaCarta.accion}");
        return true;
    }

    // inventario lleno: si tienes UI de reemplazo, la muestras (aquí no sabes aún si se guardará)
    if (replacementUI != null)
    {
        replacementUI.MostrarPanelReemplazo(storage, nuevaCarta);
        return false; // de momento no se ha guardado
    }

    // inventario lleno sin UI
    Debug.LogWarning("⚠️ Inventario lleno y no hay UI de reemplazo");
    return false;
}


    public void ReemplazarCartaEnStorage(int index, Carta_U nuevaCarta)
    {
        if (index >= 0 && index < storage.Count)
        {
            storage[index] = nuevaCarta;
            ActualizarUIStorage();
        }
    }

    public void UsarCartaDelStorage(int index, MovePlayer_U jugador)
    {
        if (index < 0 || index >= storage.Count) return;

        var carta = storage[index];

        // Aplica en modo silencioso vía EjecutarBeneficio/Penalidad
        if (carta.accion.Contains("Retrocede") || carta.accion == "PierdeTurno" || carta.accion == "IrSalida")
            EjecutarPenalidad(carta, jugador);
        else
            EjecutarBeneficio(carta, jugador);

        storage.RemoveAt(index);
        ActualizarUIStorage();
    }

    public void ActualizarUIStorage()
    {
        if (bonusUI != null)
            bonusUI.ActualizarUI(storage);
    }

    // =========================
    // 10) Utilidades
    // =========================
    public Carta_U ObtenerCartaBeneficioAleatoria()
    {
        if (benefits.Count == 0) return null;
        return benefits[UnityEngine.Random.Range(0, benefits.Count)];
    }

    public Carta_U ObtenerCartaPenalidadAleatoria()
    {
        if (penalty.Count == 0) return null;
        return penalty[UnityEngine.Random.Range(0, penalty.Count)];
    }
}
