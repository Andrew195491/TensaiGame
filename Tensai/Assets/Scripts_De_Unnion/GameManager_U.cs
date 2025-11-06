// ============================================
// GameManager_U.cs (Unificado y Corregido)
// ============================================
// Control principal del flujo del juego por turnos.
// Gestiona:
// - Jugador y bots (IA básica con dificultad ajustable)
// - Tiradas de dado (manual o automática)
// - Resolución de casillas: Pregunta, Beneficio, Penalidad, Neutral
// - Inventario de cartas (beneficios) con límite y efectos.
// ============================================

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class GameManager_U : MonoBehaviour
{
    // ============================================
    // SECCIÓN 1: CONFIGURACIÓN GENERAL
    // ============================================

    public enum DificultadBot { Facil, Medio, Dificil }

    [Header("Referencias Principales")]
    public CartaManager_U cartaManager;
    public DiceController_U dadoUI;
    public ThirdPersonCameraHybrid_U thirdPersonCam;

    [Header("Jugador y Bots")]
    public MovePlayer_U jugador;
    [Range(0, 3)] public int numeroBots = 0;
    public GameObject[] prefabsBots;
    public DificultadBot dificultadBots = DificultadBot.Medio;

    [Header("Cámara")]
    public float camTravelSpeed = 10f;
    public bool allowOrbitDuringBot = true;

    [Header("Delays de Bots")]
    public float botPreRollDelay = 0.6f;
    public float botPostRollDelay = 0.35f;

    [Header("Inventario de Beneficios")]
    public int maxInventario = 3;

    [Header("Distribución en Casilla")]
    public float tileSeparationRadius = 0.35f;

    // ============================================
    // SECCIÓN 2: VARIABLES DE JUEGO
    // ============================================

    private readonly List<MovePlayer_U> turnOrder = new();
    private int turnoIndex = 0;
    private bool turnoEnCurso = false;

    private readonly Dictionary<MovePlayer_U, int> turnosSaltados = new();
    private readonly HashSet<MovePlayer_U> repetirTirada = new();
    private readonly Dictionary<MovePlayer_U, List<Carta_U>> inventario = new();

    // Posiciones relativas cuando varios jugadores comparten casilla
    private readonly Vector3[] offsets = new Vector3[]
    {
        Vector3.zero,
        new Vector3(0.35f, 0f, 0.35f),
        new Vector3(-0.35f, 0f, 0.35f),
        new Vector3(0.35f, 0f, -0.35f)
    };

    // ============================================
    // SECCIÓN 3: INICIO DEL JUEGO
    // ============================================

    void Start()
    {
        if (jugador == null)
        {
            Debug.LogError("❌ Asigna el MovePlayer_U del jugador en GameManager_U.");
            return;
        }

        // Vincular referencias de otros sistemas
        if (cartaManager != null && cartaManager.dadoController == null)
            cartaManager.dadoController = dadoUI;

        // Orden de turnos: jugador + bots
        turnOrder.Clear();
        turnOrder.Add(jugador);

        for (int i = 0; i < numeroBots; i++)
        {
            GameObject botGO;
            
            // Si hay prefab asignado, usarlo
            if (i < prefabsBots.Length && prefabsBots[i] != null)
            {
                botGO = Instantiate(prefabsBots[i]);
            }
            else
            {
                // Crear bot simple automáticamente
                Debug.LogWarning($"⚠️ No hay prefab asignado para el bot {i + 1}. Creando bot simple.");
                botGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                botGO.name = $"Bot_{i + 1}";
                
                // Darle un color distintivo
                var renderer = botGO.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color[] colores = { Color.red, Color.blue, Color.green };
                    renderer.material.color = colores[i % colores.Length];
                }
            }

            var pm = botGO.GetComponent<MovePlayer_U>();
            if (pm == null) pm = botGO.AddComponent<MovePlayer_U>();
            
            // Configurar el dado para el bot
            pm.dado = dadoUI;
            
            pm.transform.position = jugador.transform.position + offsets[Mathf.Min(i + 1, offsets.Length - 1)];
            turnOrder.Add(pm);
        }

        // Inicializar estructuras
        foreach (var p in turnOrder)
        {
            if (!turnosSaltados.ContainsKey(p)) turnosSaltados[p] = 0;
            if (!inventario.ContainsKey(p)) inventario[p] = new List<Carta_U>(maxInventario);
        }

        // Dado inicial
        if (dadoUI != null)
        {
            dadoUI.OnRolled = null;
            dadoUI.BloquearDado(false);
        }

        // Cámara
        if (thirdPersonCam != null)
        {
            thirdPersonCam.followHorizontalOnly = true;
            thirdPersonCam.focusBlendTime = 0f;
            thirdPersonCam.SetTarget(jugador.transform, smooth: false);
            thirdPersonCam.SetUserControl(true);
        }

        ArrangeAllTiles();
        turnoIndex = 0;
        turnoEnCurso = false;

        StartCoroutine(LoopTurnos());
    }

    // ============================================
    // SECCIÓN 4: BUCLE DE TURNOS PRINCIPAL
    // ============================================

    IEnumerator LoopTurnos()
    {
        while (true)
        {
            if (turnoEnCurso) { yield return null; continue; }

            var actual = CurrentPeon;

            // Si el jugador debe saltar turno
            if (turnosSaltados.TryGetValue(actual, out int restan) && restan > 0)
            {
                turnosSaltados[actual] = restan - 1;
                Debug.Log($"⏭️ {Nombre(actual)} salta turno. Restan: {turnosSaltados[actual]}");
                AvanzarTurno();
                continue;
            }

            // Movimiento de cámara al jugador activo
            if (thirdPersonCam != null)
            {
                thirdPersonCam.SetUserControl(false);
                yield return StartCoroutine(thirdPersonCam.FocusTo(actual.transform, camTravelSpeed, true));
                bool enableUser = (actual == jugador) || allowOrbitDuringBot;
                thirdPersonCam.SetUserControl(enableUser);
            }

            turnoEnCurso = true;

            // Turno del jugador humano
            if (actual == jugador)
            {
                dadoUI.OnRolled = OnJugadorTiroDado;
                dadoUI.BloquearDado(false);
                Debug.Log("🎲 Turno del JUGADOR. Lanza el dado.");
            }
            else // Turno del bot
            {
                dadoUI.OnRolled = null;
                dadoUI.BloquearDado(true);
                StartCoroutine(TurnoBot(actual));
            }

            yield return null;
        }
    }

    MovePlayer_U CurrentPeon => turnOrder[turnoIndex];

    // ============================================
    // SECCIÓN 5: RESOLUCIÓN DE TURNOS
    // ============================================

    void OnJugadorTiroDado(int numero)
    {
        dadoUI.OnRolled = null;
        dadoUI.BloquearDado(true);
        StartCoroutine(ResolverTurno(jugador, numero, true));
    }

    IEnumerator TurnoBot(MovePlayer_U bot)
    {
        float pre = botPreRollDelay, post = botPostRollDelay;
        int? numero = null;

        if (dadoUI != null)
        {
            yield return StartCoroutine(dadoUI.RollForBot(bot.transform, pre, post, n => numero = n));
        }
        else
        {
            yield return new WaitForSeconds(pre);
            numero = Random.Range(1, 7);
            yield return new WaitForSeconds(post);
        }

        while (numero == null) yield return null;
        Debug.Log($"🤖 BOT {Nombre(bot)} tira {numero.Value}");
        yield return StartCoroutine(ResolverTurno(bot, numero.Value, false));
    }

    IEnumerator ResolverTurno(MovePlayer_U peon, int pasos, bool esHumano)
    {
        if (dadoUI != null) dadoUI.BloquearDado(true);

        yield return StartCoroutine(peon.JumpMultipleTimes(pasos));
        ArrangeAllTiles();
        yield return new WaitForSeconds(0.25f);

        Tile_U tile = peon.GetCurrentTile();
        if (tile == null)
        {
            Debug.LogWarning("⚠️ No se encontró Tile bajo el peón.");
            TerminarTurnoORepetir(peon, esHumano);
            yield break;
        }

        switch (tile.tipo)
        {
            case Tile_U.TipoCasilla.Neutral:
                TerminarTurnoORepetir(peon, esHumano);
                break;

            case Tile_U.TipoCasilla.Pregunta:
            {
                bool? resultado = null;
                cartaManager.HacerPregunta(tile.categoria, esHumano, ProbAciertoBots(), (bool correcta) => resultado = correcta);
                while (resultado == null) yield return null;

                if (resultado == false)
                {
                    yield return StartCoroutine(peon.Retroceder(pasos));
                    ArrangeAllTiles();
                }

                TerminarTurnoORepetir(peon, esHumano);
                break;
            }

            case Tile_U.TipoCasilla.Beneficio:
                cartaManager.EjecutarAccionBeneficio(peon);
                yield return new WaitForSeconds(0.5f);
                TerminarTurnoORepetir(peon, esHumano);
                break;

            case Tile_U.TipoCasilla.Penalidad:
                cartaManager.EjecutarAccionPenalidad(peon);
                yield return new WaitForSeconds(0.5f);
                TerminarTurnoORepetir(peon, esHumano);
                break;
        }
    }

    // ============================================
    // SECCIÓN 6: CONTROL DE TURNO
    // ============================================

    void TerminarTurnoORepetir(MovePlayer_U peon, bool esHumano)
    {
        if (repetirTirada.Contains(peon))
        {
            repetirTirada.Remove(peon);
            Debug.Log($"🔁 {Nombre(peon)} repite tirada.");

            if (peon == jugador)
            {
                dadoUI.OnRolled = OnJugadorTiroDado;
                dadoUI.BloquearDado(false);
            }
            turnoEnCurso = false;
            return;
        }

        AvanzarTurno();
        turnoEnCurso = false;
    }

    void AvanzarTurno() => turnoIndex = (turnoIndex + 1) % turnOrder.Count;

    // ============================================
    // SECCIÓN 7: EFECTOS DE BENEFICIOS Y PENALIDADES
    // ============================================

    float ProbAciertoBots() => dificultadBots switch
    {
        DificultadBot.Facil => 0.40f,
        DificultadBot.Medio => 0.65f,
        DificultadBot.Dificil => 0.85f,
        _ => 0.65f
    };

    public void MarcarRepetirTirada(MovePlayer_U peon) => repetirTirada.Add(peon);

    public void AplicarSaltarTurnos(MovePlayer_U peon, int turnos)
    {
        if (!turnosSaltados.ContainsKey(peon)) turnosSaltados[peon] = 0;
        turnosSaltados[peon] += Mathf.Max(1, turnos);
        Debug.Log($"⏳ {Nombre(peon)} perderá {turnosSaltados[peon]} turno(s).");
    }

    // ============================================
    // SECCIÓN 8: INVENTARIO
    // ============================================

    public bool TryStoreBenefit(Carta_U carta)
    {
        if (carta == null) return false;
        var owner = CurrentPeon;
        if (owner == null) return false;

        if (!inventario.ContainsKey(owner))
            inventario[owner] = new List<Carta_U>(maxInventario);

        var inv = inventario[owner];
        if (inv.Count >= maxInventario)
        {
            Debug.LogWarning($"[Inventario] {Nombre(owner)} inventario lleno. No se guarda '{carta.pregunta}'.");
            return false;
        }

        inv.Add(carta);
        Debug.Log($"[Inventario] {Nombre(owner)} guarda beneficio '{carta.pregunta}'. ({inv.Count}/{maxInventario})");
        return true;
    }

    // ============================================
    // SECCIÓN 9: POSICIONAMIENTO EN CASILLAS
    // ============================================

    void ArrangeAllTiles()
    {
        var byTile = new Dictionary<Tile_U, List<MovePlayer_U>>();
        foreach (var p in turnOrder)
        {
            if (p == null) continue;
            var t = p.GetCurrentTile();
            if (t == null) continue;
            if (!byTile.TryGetValue(t, out var list))
            {
                list = new List<MovePlayer_U>();
                byTile[t] = list;
            }
            list.Add(p);
        }

        foreach (var kv in byTile)
            ArrangeTile(kv.Key, kv.Value);
    }

    void ArrangeTile(Tile_U tile, List<MovePlayer_U> peones)
    {
        if (tile == null || peones == null || peones.Count == 0) return;

        peones.Sort((a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));

        Vector2[] pattern = ComputeOffsets(peones.Count, tileSeparationRadius);
        Vector3 center = tile.transform.position;
        Vector3 right = tile.transform.right;
        Vector3 forward = tile.transform.forward;

        for (int i = 0; i < peones.Count && i < pattern.Length; i++)
        {
            var p = peones[i];
            Vector2 o = pattern[i];
            Vector3 targetPos = center + right * o.x + forward * o.y;
            targetPos.y = p.transform.position.y;
            p.transform.position = targetPos;
        }
    }

    Vector2[] ComputeOffsets(int count, float r)
    {
        switch (count)
        {
            case 1: return new[] { Vector2.zero };
            case 2: return new[] { new Vector2(-r, 0f), new Vector2(r, 0f) };
            case 3:
                float a = r, h = a * 0.5f, s = a * 0.8660254f;
                return new[] { new Vector2(0f, a), new Vector2(-s, -h), new Vector2(s, -h) };
            default:
                var list = new List<Vector2>
                {
                    new Vector2(-r, -r),
                    new Vector2(r, -r),
                    new Vector2(-r, r),
                    new Vector2(r, r)
                };
                for (int i = 4; i < count; i++)
                {
                    float ang = (i - 4) * Mathf.PI * 2f / 8f;
                    list.Add(new Vector2(Mathf.Cos(ang) * (r * 1.8f), Mathf.Sin(ang) * (r * 1.8f)));
                }
                return list.ToArray();
        }
    }

    // ============================================
    // SECCIÓN 10: UTILIDADES
    // ============================================

    string Nombre(MovePlayer_U p) => p != null ? p.gameObject.name : "Peon";
}