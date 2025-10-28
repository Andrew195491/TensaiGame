
// ============================================
// GameManager2.cs (Actualizado - Cambios principales)
// ============================================
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class GameManager2 : MonoBehaviour
{
    public enum DificultadBot { Facil, Medio, Dificil }

    [Header("Referencias")]
    public CardEffectManager2 cardEffectManager;
    public DiceController2 dadoUI;
    public ThirdPersonCameraHybrid2 thirdPersonCam;

    [Header("Jugador y Bots")]
    public MovePlayer2 jugador;
    [Range(0, 3)] public int numeroBots = 0;
    public GameObject[] prefabsBots;
    public DificultadBot dificultadBots = DificultadBot.Medio;

    [Header("Cámara")]
    public float camTravelSpeed = 10f;
    public bool allowOrbitDuringBot = true;

    [Header("Bot Delays")]
    public float botPreRollDelay = 0.6f;
    public float botPostRollDelay = 0.35f;

    [Header("Inventario de beneficios")]
    public int maxInventario = 3;

    [Header("Colocación en Tile")]
    public float tileSeparationRadius = 0.35f;

    private readonly List<MovePlayer2> turnOrder = new();
    private int turnoIndex = 0;
    private bool turnoEnCurso = false;

    private readonly Dictionary<MovePlayer2, int> turnosSaltados = new();
    private readonly HashSet<MovePlayer2> repetirTirada = new();
    private readonly Dictionary<MovePlayer2, List<CartaEntry2>> inventario = new();

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
            Debug.LogError("Asigna el MovePlayer2 del jugador en GameManager2.");
            return;
        }

        if (cardEffectManager != null && cardEffectManager.gameManager == null)
            cardEffectManager.gameManager = this;

        turnOrder.Clear();
        turnOrder.Add(jugador);

        for (int i = 0; i < numeroBots; i++)
        {
            if (i >= prefabsBots.Length || prefabsBots[i] == null)
            {
                Debug.LogWarning($"No hay prefab para el bot {i + 1}. Se omite.");
                continue;
            }
            GameObject botGO = Instantiate(prefabsBots[i]);
            var pm = botGO.GetComponent<MovePlayer2>();
            if (pm == null) pm = botGO.AddComponent<MovePlayer2>();
            pm.transform.position = jugador.transform.position + offsets[Mathf.Min(i + 1, offsets.Length - 1)];
            turnOrder.Add(pm);
        }

        foreach (var p in turnOrder)
        {
            if (!turnosSaltados.ContainsKey(p)) turnosSaltados[p] = 0;
            if (!inventario.ContainsKey(p)) inventario[p] = new List<CartaEntry2>(maxInventario);
        }

        if (dadoUI != null)
        {
            dadoUI.OnRolled = null;
            dadoUI.BloquearDado(false);
        }

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

    IEnumerator LoopTurnos()
    {
        while (true)
        {
            if (turnoEnCurso) { yield return null; continue; }

            var actual = CurrentPeon;

            if (turnosSaltados.TryGetValue(actual, out int restan) && restan > 0)
            {
                turnosSaltados[actual] = restan - 1;
                Debug.Log($"[{Nombre(actual)}] salta turno. Restan: {turnosSaltados[actual]}");
                AvanzarTurno();
                continue;
            }

            if (thirdPersonCam != null)
            {
                thirdPersonCam.SetUserControl(false);
                yield return StartCoroutine(thirdPersonCam.FocusTo(actual.transform, camTravelSpeed, keepHorizontalOnly: true));
                bool enableUser = (actual == jugador) || allowOrbitDuringBot;
                thirdPersonCam.SetUserControl(enableUser);
            }

            turnoEnCurso = true;

            if (actual == jugador)
            {
                if (dadoUI != null)
                {
                    dadoUI.OnRolled = OnJugadorTiroDado;
                    dadoUI.BloquearDado(false);
                }
                Debug.Log("Turno del JUGADOR. Lanza el dado.");
            }
            else
            {
                if (dadoUI != null)
                {
                    dadoUI.OnRolled = null;
                    dadoUI.BloquearDado(true);
                }
                StartCoroutine(TurnoBot(actual));
            }

            yield return null;
        }
    }

    MovePlayer2 CurrentPeon => turnOrder[turnoIndex];

    void OnJugadorTiroDado(int numero)
    {
        if (dadoUI != null)
        {
            dadoUI.OnRolled = null;
            dadoUI.BloquearDado(true);
        }
        StartCoroutine(ResolverTurno(jugador, numero, true));
    }

    IEnumerator TurnoBot(MovePlayer2 bot)
    {
        float preDelay = botPreRollDelay;
        float postDelay = botPostRollDelay;
        int? numero = null;

        if (dadoUI != null)
        {
            yield return StartCoroutine(dadoUI.RollForBot(bot.transform, preDelay, postDelay, n => numero = n));
        }
        else
        {
            yield return new WaitForSeconds(preDelay);
            numero = Random.Range(1, 7);
            yield return new WaitForSeconds(postDelay);
        }

        while (numero == null) yield return null;

        Debug.Log($"Turno BOT [{Nombre(bot)}]: tira {numero.Value}");
        yield return StartCoroutine(ResolverTurno(bot, numero.Value, false));
    }

    IEnumerator ResolverTurno(MovePlayer2 peon, int pasos, bool esHumano)
    {
        if (dadoUI != null) dadoUI.BloquearDado(true);

        yield return StartCoroutine(peon.MoverAdelante(pasos));
        ArrangeAllTiles();
        yield return new WaitForSeconds(0.25f);

        Tile2 tile = peon.GetCurrentTile();
        if (tile == null)
        {
            Debug.LogWarning("No se encontró Tile bajo el peón.");
            TerminarTurnoORepetir(peon, esHumano);
            yield break;
        }

        switch (tile.tipo)
        {
            case Tile2.TipoCasilla.Neutral:
                TerminarTurnoORepetir(peon, esHumano);
                break;

            case Tile2.TipoCasilla.Pregunta:
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

            case Tile2.TipoCasilla.Beneficio:
                yield return StartCoroutine(cardEffectManager.EjecutarBeneficioAleatorio(peon, esHumano));
                TerminarTurnoORepetir(peon, esHumano);
                break;

            case Tile2.TipoCasilla.Penalidad:
                yield return StartCoroutine(cardEffectManager.EjecutarPenalidadAleatoria(peon, esHumano));
                TerminarTurnoORepetir(peon, esHumano);
                break;
        }
    }

    void TerminarTurnoORepetir(MovePlayer2 peon, bool esHumano)
    {
        if (repetirTirada.Contains(peon))
        {
            repetirTirada.Remove(peon);
            Debug.Log($"[{Nombre(peon)}] repite tirada.");

            if (peon == jugador)
            {
                if (dadoUI != null)
                {
                    dadoUI.OnRolled = OnJugadorTiroDado;
                    dadoUI.BloquearDado(false);
                }
            }
            turnoEnCurso = false;
            return;
        }

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
            case DificultadBot.Facil: return 0.40f;
            case DificultadBot.Medio: return 0.65f;
            case DificultadBot.Dificil: return 0.85f;
        }
        return 0.65f;
    }

    public bool TryStoreBenefit(MovePlayer2 peon, string effectId, int value = 1)
    {
        if (peon == null || string.IsNullOrEmpty(effectId)) return false;
        switch (effectId.ToLowerInvariant())
        {
            case "repeat":
            case "extra_roll":
            case "repetir":
            case "repite tirada":
                MarcarRepetirTirada(peon);
                Debug.Log($"[Benefit] {Nombre(peon)} obtiene repetir tirada.");
                return true;
            default:
                Debug.Log($"[Benefit] '{effectId}' no reconocido en GameManager2.");
                return false;
        }
    }

    public bool TryStoreBenefit(MovePlayer2 peon, int effectId, int value = 1)
    {
        switch (effectId)
        {
            case 0: return TryStoreBenefit(peon, "repeat", value);
            default:
                Debug.Log($"[Benefit:int] id {effectId} no mapeado en GameManager2.");
                return false;
        }
    }

    public bool TryStoreBenefit(CartaEntry2 entry)
    {
        if (entry == null) return false;
        var owner = CurrentPeon;
        if (owner == null) return false;

        if (!inventario.ContainsKey(owner)) inventario[owner] = new List<CartaEntry2>(maxInventario);
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

    public bool TryStorePenalty(MovePlayer2 peon, string effectId, int value = 1)
    {
        if (peon == null || string.IsNullOrEmpty(effectId)) return false;
        switch (effectId.ToLowerInvariant())
        {
            case "skip":
            case "skip_turns":
            case "saltarturno":
            case "pierde turno":
                AplicarSaltarTurnos(peon, Mathf.Max(1, value));
                Debug.Log($"[Penalty] {Nombre(peon)} saltará {Mathf.Max(1, value)} turno(s).");
                return true;

            case "back":
            case "retroceder":
            case "retrocede 2":
                StartCoroutine(peon.MoverAtras(Mathf.Max(1, value)));
                Debug.Log($"[Penalty] {Nombre(peon)} retrocede {Mathf.Max(1, value)} casilla(s).");
                return true;

            default:
                Debug.Log($"[Penalty] '{effectId}' no reconocido en GameManager2.");
                return false;
        }
    }

    public bool TryStorePenalty(MovePlayer2 peon, int effectId, int value = 1)
    {
        switch (effectId)
        {
            case 0: return TryStorePenalty(peon, "skip", value);
            default:
                Debug.Log($"[Penalty:int] id {effectId} no mapeado en GameManager2.");
                return false;
        }
    }

    public void MarcarRepetirTirada(MovePlayer2 peon) => repetirTirada.Add(peon);

    public void AplicarSaltarTurnos(MovePlayer2 peon, int turnos)
    {
        if (!turnosSaltados.ContainsKey(peon)) turnosSaltados[peon] = 0;
        turnosSaltados[peon] += Mathf.Max(1, turnos);
        Debug.Log($"[{Nombre(peon)}] perderá {turnosSaltados[peon]} turno(s).");
    }

    string Nombre(MovePlayer2 p) => p != null ? p.gameObject.name : "Peon";

    void ArrangeAllTiles()
    {
        var byTile = new Dictionary<Tile2, List<MovePlayer2>>();
        foreach (var p in turnOrder)
        {
            if (p == null) continue;
            var t = p.GetCurrentTile();
            if (t == null) continue;
            if (!byTile.TryGetValue(t, out var list)) { list = new List<MovePlayer2>(); byTile[t] = list; }
            list.Add(p);
        }

        foreach (var kv in byTile)
            ArrangeTile(kv.Key, kv.Value);
    }

    void ArrangeTile(Tile2 tile, List<MovePlayer2> peones)
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
            case 1:
                return new[] { Vector2.zero };
            case 2:
                return new[] { new Vector2(-r, 0f), new Vector2(r, 0f) };
            case 3:
                float a = r;
                float h = a * 0.5f;
                float s = a * 0.8660254f;
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
}
