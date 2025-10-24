using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

/// <summary>
/// Gestiona turnos (jugador/bots), tiradas, preguntas/efectos,
/// cámara (seguimiento pegado + viaje suave al cambiar de peón) y
/// COLocación de varias fichas en el mismo tile (1 centro, 2 lados, 3 triángulo, 4 cuadrado).
/// Requiere ThirdPersonCameraHybrid con FocusTo(...), followHorizontalOnly y focusBlendTime.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum DificultadBot { Facil, Medio, Dificil }

    [Header("Referencias")]
    public CardEffectManager cardEffectManager;
    public DiceController dadoUI;
    public ThirdPersonCameraHybrid thirdPersonCam;

    [Header("Jugador y Bots")]
    public MovePlayer jugador;
    [Range(0,3)] public int numeroBots = 0;
    public GameObject[] prefabsBots;
    public DificultadBot dificultadBots = DificultadBot.Medio;

    [Header("Cámara")]
    [Tooltip("Velocidad (m/s) del viaje de la cámara cuando cambia de peón")]
    public float camTravelSpeed = 10f;
    [Tooltip("Permitir orbitar la cámara también durante el turno del bot")]
    public bool allowOrbitDuringBot = true;

    [Header("Bot Delays")]
    public float botPreRollDelay = 0.6f;
    public float botPostRollDelay = 0.35f;

    [Header("Inventario de beneficios")]
    public int maxInventario = 3;

    [Header("Colocación en Tile")]
    [Tooltip("Radio (m) para separar fichas cuando comparten tile")]
    public float tileSeparationRadius = 0.35f;

    // Orden de turnos
    private readonly List<MovePlayer> turnOrder = new();
    private int turnoIndex = 0;
    private bool turnoEnCurso = false;

    // Efectos persistentes
    private readonly Dictionary<MovePlayer, int> turnosSaltados = new();
    private readonly HashSet<MovePlayer> repetirTirada = new();

    // Inventario por peón
    private readonly Dictionary<MovePlayer, List<CartaEntry>> inventario = new();

    // Offsets para casilla inicial (solo arranque escena)
    private readonly Vector3[] offsets = new Vector3[] {
        Vector3.zero,
        new Vector3(0.35f, 0f, 0.35f),
        new Vector3(-0.35f, 0f, 0.35f),
        new Vector3(0.35f, 0f, -0.35f)
    };

    void Start()
    {
        if (jugador == null)
        {
            Debug.LogError("Asigna el MovePlayer del jugador en GameManager.");
            return;
        }

        if (cardEffectManager != null && cardEffectManager.gameManager == null)
            cardEffectManager.gameManager = this;

        // Construye orden de turnos
        turnOrder.Clear();
        turnOrder.Add(jugador);

        for (int i = 0; i < numeroBots; i++)
        {
            if (i >= prefabsBots.Length || prefabsBots[i] == null)
            {
                Debug.LogWarning($"No hay prefab para el bot {i+1}. Se omite.");
                continue;
            }
            GameObject botGO = Instantiate(prefabsBots[i]);
            var pm = botGO.GetComponent<MovePlayer>();
            if (pm == null) pm = botGO.AddComponent<MovePlayer>();
            pm.transform.position = jugador.transform.position + offsets[Mathf.Min(i+1, offsets.Length-1)];
            turnOrder.Add(pm);
        }

        foreach (var p in turnOrder)
        {
            if (!turnosSaltados.ContainsKey(p)) turnosSaltados[p] = 0;
            if (!inventario.ContainsKey(p)) inventario[p] = new List<CartaEntry>(maxInventario);
        }

        // Dado: sin handler hasta turno del jugador
        if (dadoUI != null)
        {
            dadoUI.OnRolled = null;
            dadoUI.BloquearDado(false);
        }

        // Cámara: pegada (sin lag) y solo seguimiento horizontal
        if (thirdPersonCam != null)
        {
            thirdPersonCam.followHorizontalOnly = true;   // no saltos verticales
            thirdPersonCam.focusBlendTime = 0f;           // sin suavizado mientras seguimos al mismo peón
            thirdPersonCam.SetTarget(jugador.transform, smooth: false); // posición inicial sin teleport vertical
            thirdPersonCam.SetUserControl(true);
        }

        // Organiza fichas iniciales por tile
        ArrangeAllTiles();

        turnoIndex = 0;
        turnoEnCurso = false;

        StartCoroutine(LoopTurnos());
    }

    IEnumerator LoopTurnos()
    {
        while (true)
        {
            if (turnoEnCurso) { yield return null; continue; }

            var actual = CurrentPeon;

            // Gestionar saltos de turno
            if (turnosSaltados.TryGetValue(actual, out int restan) && restan > 0)
            {
                turnosSaltados[actual] = restan - 1;
                Debug.Log($"[{Nombre(actual)}] salta turno. Restan: {turnosSaltados[actual]}");
                AvanzarTurno();
                continue; // volver al bucle; se enfocará al siguiente en la próxima iteración
            }

            // 1) Viajar la cámara hacia el peón del turno (suave, solo horizontal)
            if (thirdPersonCam != null)
            {
                thirdPersonCam.SetUserControl(false);
                yield return StartCoroutine(thirdPersonCam.FocusTo(actual.transform, camTravelSpeed, keepHorizontalOnly: true));
                bool enableUser = (actual == jugador) || allowOrbitDuringBot;
                thirdPersonCam.SetUserControl(enableUser);
            }

            // 2) Comienza el turno
            turnoEnCurso = true;

            if (actual == jugador)
            {
                if (dadoUI != null)
                {
                    dadoUI.OnRolled = OnJugadorTiroDado; // escuchar solo en turno del jugador
                    dadoUI.BloquearDado(false);
                }
                Debug.Log("Turno del JUGADOR. Lanza el dado.");
            }
            else
            {
                if (dadoUI != null)
                {
                    dadoUI.OnRolled = null; // evitar disparar handler del jugador
                    dadoUI.BloquearDado(true);
                }
                StartCoroutine(TurnoBot(actual));
            }

            yield return null;
        }
    }

    MovePlayer CurrentPeon => turnOrder[turnoIndex];

    void OnJugadorTiroDado(int numero)
    {
        if (dadoUI != null)
        {
            dadoUI.OnRolled = null;     // desuscribir para evitar dobles
            dadoUI.BloquearDado(true);  // bloquear hasta resolver turno
        }
        StartCoroutine(ResolverTurno(jugador, numero, true));
    }

    IEnumerator TurnoBot(MovePlayer bot)
    {
        float preDelay = botPreRollDelay;
        float postDelay = botPostRollDelay;

        int? numero = null;

        if (dadoUI != null)
        {
            // El bot usa callback local (no el evento global)
            yield return StartCoroutine(dadoUI.RollForBot(bot.transform, preDelay, postDelay, n => numero = n));
        }
        else
        {
            // Fallback si no hay UI del dado
            yield return new WaitForSeconds(preDelay);
            numero = Random.Range(1, 7);
            yield return new WaitForSeconds(postDelay);
        }

        while (numero == null) yield return null;

        Debug.Log($"Turno BOT [{Nombre(bot)}]: tira {numero.Value}");
        yield return StartCoroutine(ResolverTurno(bot, numero.Value, false));
    }

    IEnumerator ResolverTurno(MovePlayer peon, int pasos, bool esHumano)
    {
        if (dadoUI != null) dadoUI.BloquearDado(true);

        // Mover peón
        yield return StartCoroutine(peon.MoverAdelante(pasos));
        ArrangeAllTiles();

        // Pausa breve
        yield return new WaitForSeconds(0.25f);

        // Resolver casilla
        Tile tile = peon.GetCurrentTile();
        if (tile == null)
        {
            Debug.LogWarning("No se encontró Tile bajo el peón.");
            TerminarTurnoORepetir(peon, esHumano);
            yield break;
        }

        switch (tile.tipo)
        {
            case Tile.TipoCasilla.Neutral:
                TerminarTurnoORepetir(peon, esHumano);
                break;

            case Tile.TipoCasilla.Pregunta:
            {
                bool? resultado = null;
                cardEffectManager.HacerPregunta(tile.categoria, esHumano, ProbAciertoBots(), (bool correcta) => resultado = correcta);
                while (resultado == null) yield return null;

                if (resultado == false)
                {
                    yield return StartCoroutine(peon.MoverAtras(pasos));
                    ArrangeAllTiles();
                }

                TerminarTurnoORepetir(peon, esHumano);
                break;
            }

            case Tile.TipoCasilla.Beneficio:
                yield return StartCoroutine(cardEffectManager.EjecutarBeneficioAleatorio(peon, esHumano));
                TerminarTurnoORepetir(peon, esHumano);
                break;

            case Tile.TipoCasilla.Penalidad:
                yield return StartCoroutine(cardEffectManager.EjecutarPenalidadAleatoria(peon, esHumano));
                TerminarTurnoORepetir(peon, esHumano);
                break;
        }
    }

    void TerminarTurnoORepetir(MovePlayer peon, bool esHumano)
    {
        if (repetirTirada.Contains(peon))
        {
            repetirTirada.Remove(peon);
            Debug.Log($"[{Nombre(peon)}] repite tirada.");

            if (peon == jugador)
            {
                if (dadoUI != null)
                {
                    dadoUI.OnRolled = OnJugadorTiroDado; // permitir nueva tirada del jugador
                    dadoUI.BloquearDado(false);
                }
            }
            turnoEnCurso = false; // LoopTurnos retomará con el mismo peón (cámara ya está encima)
            return;
        }

        // Fin normal de turno
        AvanzarTurno();
        turnoEnCurso = false;
    }

    void AvanzarTurno()
    {
        turnoIndex = (turnoIndex + 1) % turnOrder.Count;
    }

    float ProbAciertoBots()
    {
        switch (dificultadBots)
        {
            case DificultadBot.Facil:   return 0.40f;
            case DificultadBot.Medio:   return 0.65f;
            case DificultadBot.Dificil: return 0.85f;
        }
        return 0.65f;
    }

    // =========================
    //  E F E C T O S   P U E N T E
    // =========================

    public bool TryStoreBenefit(MovePlayer peon, string effectId, int value = 1)
    {
        if (peon == null || string.IsNullOrEmpty(effectId)) return false;
        switch (effectId.ToLowerInvariant())
        {
            case "repeat":
            case "extra_roll":
            case "repetir":
                MarcarRepetirTirada(peon);
                Debug.Log($"[Benefit] {Nombre(peon)} obtiene repetir tirada.");
                return true;
            default:
                Debug.Log($"[Benefit] '{effectId}' no reconocido en GameManager.");
                return false;
        }
    }

    public bool TryStoreBenefit(MovePlayer peon, int effectId, int value = 1)
    {
        switch (effectId)
        {
            case 0: return TryStoreBenefit(peon, "repeat", value);
            default:
                Debug.Log($"[Benefit:int] id {effectId} no mapeado en GameManager.");
                return false;
        }
    }

    public bool TryStoreBenefit(CartaEntry entry)
    {
        if (entry == null) return false;
        var owner = CurrentPeon;
        if (owner == null) return false;

        if (!inventario.ContainsKey(owner)) inventario[owner] = new List<CartaEntry>(maxInventario);
        var inv = inventario[owner];
        if (inv.Count >= maxInventario)
        {
            Debug.LogWarning($"[Inventario] {Nombre(owner)} inventario lleno. No se guarda '{entry.nombre}'.");
            return false;
        }

        inv.Add(entry);
        Debug.Log($"[Inventario] {Nombre(owner)} guarda beneficio '{entry.nombre}'. ({inv.Count}/{maxInventario})");
        return true;
    }

    public bool TryStorePenalty(MovePlayer peon, string effectId, int value = 1)
    {
        if (peon == null || string.IsNullOrEmpty(effectId)) return false;
        switch (effectId.ToLowerInvariant())
        {
            case "skip":
            case "skip_turns":
            case "saltarturno":
                AplicarSaltarTurnos(peon, Mathf.Max(1, value));
                Debug.Log($"[Penalty] {Nombre(peon)} saltará {Mathf.Max(1, value)} turno(s).");
                return true;

            case "back":
            case "retroceder":
                StartCoroutine(peon.MoverAtras(Mathf.Max(1, value)));
                Debug.Log($"[Penalty] {Nombre(peon)} retrocede {Mathf.Max(1, value)} casilla(s).");
                return true;

            default:
                Debug.Log($"[Penalty] '{effectId}' no reconocido en GameManager.");
                return false;
        }
    }

    public bool TryStorePenalty(MovePlayer peon, int effectId, int value = 1)
    {
        switch (effectId)
        {
            case 0: return TryStorePenalty(peon, "skip", value);
            default:
                Debug.Log($"[Penalty:int] id {effectId} no mapeado en GameManager.");
                return false;
        }
    }

    public void MarcarRepetirTirada(MovePlayer peon) => repetirTirada.Add(peon);

    public void AplicarSaltarTurnos(MovePlayer peon, int turnos)
    {
        if (!turnosSaltados.ContainsKey(peon)) turnosSaltados[peon] = 0;
        turnosSaltados[peon] += Mathf.Max(1, turnos);
        Debug.Log($"[{Nombre(peon)}] perderá {turnosSaltados[peon]} turno(s).");
    }

    string Nombre(MovePlayer p) => p != null ? p.gameObject.name : "Peon";

    // =========================
    //  Colocación múltiple por tile
    // =========================

    void ArrangeAllTiles()
    {
        var byTile = new Dictionary<Tile, List<MovePlayer>>();
        foreach (var p in turnOrder)
        {
            if (p == null) continue;
            var t = p.GetCurrentTile();
            if (t == null) continue;
            if (!byTile.TryGetValue(t, out var list)) { list = new List<MovePlayer>(); byTile[t] = list; }
            list.Add(p);
        }

        foreach (var kv in byTile)
            ArrangeTile(kv.Key, kv.Value);
    }

    void ArrangeTile(Tile tile, List<MovePlayer> peones)
    {
        if (tile == null || peones == null || peones.Count == 0) return;

        // Orden determinista para que no "bailen" los offsets
        peones.Sort((a,b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));

        Vector2[] pattern = ComputeOffsets(peones.Count, tileSeparationRadius);
        Vector3 center = tile.transform.position;
        Vector3 right = tile.transform.right;
        Vector3 forward = tile.transform.forward;

        for (int i = 0; i < peones.Count && i < pattern.Length; i++)
        {
            var p = peones[i];
            Vector2 o = pattern[i];
            Vector3 targetPos = center + right * o.x + forward * o.y;
            // Mantener altura del peón
            targetPos.y = p.transform.position.y;
            p.transform.position = targetPos;
        }
    }

    // Devuelve offsets en plano XZ para 1..4 peones (en metros, respecto al centro del tile)
    Vector2[] ComputeOffsets(int count, float r)
    {
        switch (count)
        {
            case 1:
                return new[] { Vector2.zero };
            case 2:
                // lados opuestos del tile (izq / der)
                return new[] { new Vector2(-r, 0f), new Vector2(r, 0f) };
            case 3:
                // triángulo equilátero (una punta hacia +Z)
                float a = r; // distancia radial
                float h = a * 0.5f; // proyección en Z para los dos de abajo
                float s = a * 0.8660254f; // sqrt(3)/2 * r
                return new[] { new Vector2(0f, a), new Vector2(-s, -h), new Vector2(s, -h) };
            default:
                // 4 o más: cuadrado básico. Los extras se colocan en una corona externa
                var list = new List<Vector2>
                {
                    new Vector2(-r, -r),
                    new Vector2(r, -r),
                    new Vector2(-r, r),
                    new Vector2(r, r)
                };
                for (int i = 4; i < count; i++)
                {
                    float ang = (i - 4) * Mathf.PI * 2f / 8f; // 8 posiciones externas
                    list.Add(new Vector2(Mathf.Cos(ang) * (r * 1.8f), Mathf.Sin(ang) * (r * 1.8f)));
                }
                return list.ToArray();
        }
    }
}