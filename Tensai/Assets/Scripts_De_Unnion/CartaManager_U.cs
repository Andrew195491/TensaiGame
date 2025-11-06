using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Gestor central de todas las cartas del juego.
/// Combina el manejo de:
/// - Cartas de trivia (Historia, Geografía, Ciencia)
/// - Cartas especiales (Beneficios y Penalidades)
/// Incluye carga desde JSON, control de UI y gestión de inventario.
/// ACTUALIZADO: Compatible con MovePlayer_U y BonusUI_U
/// </summary>
public class CartaManager_U : MonoBehaviour
{
    // ============================================
    // SECCIÓN 1: CARTAS DE TRIVIA
    // ============================================

    [Header("Archivo JSON de Preguntas (Trivia)")]
    [Tooltip("Si se deja vacío, cargará Resources/cartas.json")]
    public TextAsset cartasTriviaJSON;

    [Header("UI Trivia")]
    [Tooltip("Interfaz que muestra las preguntas al jugador.")]
    public CartaUI_U cartaUI;

    // Copias cargadas desde JSON
    private List<Carta_U> historia = new();
    private List<Carta_U> geografia = new();
    private List<Carta_U> ciencia = new();

    // Barajas temporales para evitar repeticiones
    private readonly Dictionary<Tile_U.Categoria, List<Carta_U>> baraja = new();

    // ============================================
    // SECCIÓN 2: CARTAS ESPECIALES
    // ============================================

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

    // ============================================
    // SECCIÓN 3: INVENTARIO DE CARTAS
    // ============================================

    [Header("Inventario del jugador")]
    public List<Carta_U> storage = new();
    public int maxStorage = 3;

    public static CartaManager_U instancia;

    // ============================================
    // SECCIÓN 4: INICIALIZACIÓN
    // ============================================

    void Awake()
    {
        instancia = this;
        CargarCartasTriviaDesdeJSON();
        CargarCartasEspecialesDesdeJSON();
        InicializarBarajas();
    }

    void Start()
    {
        storage.Clear();
        ActualizarUIStorage();
    }

    // ============================================
    // SECCIÓN 5: CARGA DE CARTAS
    // ============================================

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

            CartasDB_U db = JsonUtility.FromJson<CartasDB_U>(json);
            if (db == null)
            {
                Debug.LogError("❌ Error al parsear el JSON de trivia.");
                return;
            }

            historia  = db.historia  != null ? new List<Carta_U>(db.historia)  : new();
            geografia = db.geografia != null ? new List<Carta_U>(db.geografia) : new();
            ciencia   = db.ciencia   != null ? new List<Carta_U>(db.ciencia)   : new();

            Debug.Log($"✅ Cartas de trivia cargadas: H={historia.Count} G={geografia.Count} C={ciencia.Count}");
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
            Debug.LogWarning("⚠️ No hay archivo JSON asignado para cartas especiales.");
            return;
        }

        try
        {
            CartasEspecialesRoot_U root = JsonUtility.FromJson<CartasEspecialesRoot_U>(cartasEspecialesJSON.text);
            if (root == null || root.Cards == null || root.Cards.Count == 0)
            {
                Debug.LogError("❌ JSON de cartas especiales vacío o mal formado.");
                return;
            }

            CartaData_U data = root.Cards[0];
            benefits = data.benefits ?? new();
            penalty = data.penalty ?? new();

            Debug.Log($"✅ Cartas especiales cargadas: Beneficios={benefits.Count}, Penalidades={penalty.Count}");
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

    // ============================================
    // SECCIÓN 6: CARTAS DE TRIVIA
    // ============================================

    private Carta_U SacarCarta(Tile_U.Categoria cat)
    {
        if (!baraja.ContainsKey(cat))
            baraja[cat] = new();

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
        Carta_U carta = baraja[cat][idx];
        baraja[cat].RemoveAt(idx);
        return carta;
    }

    public void HacerPregunta(Tile_U.Categoria categoria, bool esHumano, float probAciertoBot, Action<bool> onRespondida)
    {
        Carta_U carta = SacarCarta(categoria);
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
            bool correcta = UnityEngine.Random.value < probAciertoBot;
            int seleccion = correcta ? carta.respuestaCorrecta : OpcionAleatoriaDistintaDe(carta.respuestaCorrecta);

            cartaUI.MostrarCartaBot(carta, seleccion, correcta, () => onRespondida?.Invoke(correcta));
        }
    }

    private int OpcionAleatoriaDistintaDe(int correcta)
    {
        int pick;
        do { pick = UnityEngine.Random.Range(1, 4); } while (pick == correcta);
        return pick;
    }

    // ============================================
    // SECCIÓN 7: CARTAS ESPECIALES
    // ============================================

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

    /// <summary>
    /// Ejecuta una acción de beneficio sobre el jugador.
    /// Llamado desde MovePlayer_U cuando cae en casilla de beneficio.
    /// </summary>
    public void EjecutarAccionBeneficio(MovePlayer_U jugador)
    {
        if (jugador == null) return;

        Carta_U carta = ObtenerCartaBeneficioAleatoria();
        if (carta == null)
        {
            Debug.LogWarning("⚠️ No hay cartas de beneficio disponibles");
            return;
        }

        Debug.Log($"🎁 Beneficio obtenido: {carta.accion}");

        // Mostrar en UI y ofrecer guardar o usar
        if (cartaUI != null)
        {
            cartaUI.MostrarBeneficio(
                "¡Carta de Beneficio!",
                carta.pregunta,
                (guardar) =>
                {
                    if (guardar)
                    {
                        IntentarAgregarCarta(carta);
                    }
                    else
                    {
                        EjecutarBeneficio(carta, jugador);
                    }
                }
            );
        }
        else
        {
            // Si no hay UI, ejecutar directamente
            EjecutarBeneficio(carta, jugador);
        }
    }

    /// <summary>
    /// Ejecuta una acción de penalidad sobre el jugador.
    /// Llamado desde MovePlayer_U cuando cae en casilla de penalidad.
    /// </summary>
    public void EjecutarAccionPenalidad(MovePlayer_U jugador)
    {
        if (jugador == null) return;

        Carta_U carta = ObtenerCartaPenalidadAleatoria();
        if (carta == null)
        {
            Debug.LogWarning("⚠️ No hay cartas de penalidad disponibles");
            return;
        }

        Debug.Log($"⚡ Penalidad aplicada: {carta.accion}");

        // Mostrar en UI y luego ejecutar
        if (cartaUI != null)
        {
            cartaUI.MostrarPenalidad(
                "¡Carta de Penalidad!",
                carta.pregunta,
                () => EjecutarPenalidad(carta, jugador)
            );
        }
        else
        {
            // Si no hay UI, ejecutar directamente
            EjecutarPenalidad(carta, jugador);
        }
    }

    public void EjecutarBeneficio(Carta_U carta, MovePlayer_U jugador)
    {
        if (carta == null || jugador == null) return;

        Debug.Log($"🎁 Ejecutando beneficio: {carta.accion}");

        switch (carta.accion)
        {
            case "Avanza1": jugador.StartCoroutine(jugador.JumpMultipleTimes(1)); break;
            case "Avanza2": jugador.StartCoroutine(jugador.JumpMultipleTimes(2)); break;
            case "Avanza3": jugador.StartCoroutine(jugador.JumpMultipleTimes(3)); break;
            case "RepiteTurno": if (dadoController) dadoController.BloquearDado(false); break;
            case "TeletransporteAdelante": jugador.StartCoroutine(jugador.JumpMultipleTimes(UnityEngine.Random.Range(5, 10))); break;
            default: Debug.Log($"⚠️ Beneficio no reconocido: {carta.accion}"); break;
        }
    }

    public void EjecutarPenalidad(Carta_U carta, MovePlayer_U jugador)
    {
        if (carta == null || jugador == null) return;

        Debug.Log($"⚡ Ejecutando penalidad: {carta.accion}");

        switch (carta.accion)
        {
            case "Retrocede1": jugador.StartCoroutine(jugador.Retroceder(1)); break;
            case "Retrocede2": jugador.StartCoroutine(jugador.Retroceder(2)); break;
            case "Retrocede3": jugador.StartCoroutine(jugador.Retroceder(3)); break;
            case "PierdeTurno": if (dadoController) dadoController.BloquearDado(true); break;
            case "IrSalida": jugador.StartCoroutine(jugador.IrACasilla(0)); break;
            default: Debug.Log($"⚠️ Penalidad no reconocida: {carta.accion}"); break;
        }
    }

    // ============================================
    // SECCIÓN 8: INVENTARIO
    // ============================================

    public void IntentarAgregarCarta(Carta_U nuevaCarta)
    {
        if (storage.Count < maxStorage)
        {
            storage.Add(nuevaCarta);
            ActualizarUIStorage();
            Debug.Log($"💾 Carta guardada: {nuevaCarta.accion}");
        }
        else if (replacementUI != null)
        {
            replacementUI.MostrarPanelReemplazo(storage, nuevaCarta);
        }
        else
        {
            Debug.LogWarning("⚠️ Inventario lleno y no hay UI de reemplazo");
        }
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

        Carta_U carta = storage[index];
        if (carta.accion.Contains("Retrocede") || carta.accion == "PierdeTurno")
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
}