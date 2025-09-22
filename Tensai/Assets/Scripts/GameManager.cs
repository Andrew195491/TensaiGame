using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public enum DificultadBot { Facil, Medio, Dificil }

    [Header("Referencias")]
    public CardEffectManager cardEffectManager; // arrástralo
    public DiceController dadoUI;

    [Header("Jugador y Bots")]
    public MovePlayer jugador;
    [Range(0, 3)] public int numeroBots = 0;
    public GameObject[] prefabsBots;
    public DificultadBot dificultadBots = DificultadBot.Medio;

    private readonly List<MovePlayer> turnOrder = new List<MovePlayer>();
    private int turnoIndex = 0;
    private bool turnoEnCurso = false;

    // efectos persistentes
    private readonly Dictionary<MovePlayer, int> turnosSaltados = new Dictionary<MovePlayer, int>();
    private readonly HashSet<MovePlayer> repetirTirada = new HashSet<MovePlayer>();

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

        turnOrder.Clear();
        turnOrder.Add(jugador);

        // Instanciar bots
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

            // Colocar junto al jugador en casilla 0 con offset
            pm.transform.position = jugador.transform.position + offsets[Mathf.Min(i+1, offsets.Length-1)];
            turnOrder.Add(pm);
        }

        // Iniciar estructuras persistentes
        foreach (var p in turnOrder)
            if (!turnosSaltados.ContainsKey(p)) turnosSaltados[p] = 0;

        // Dado
        if (dadoUI != null)
        {
            dadoUI.OnRolled = OnJugadorTiroDado;
            dadoUI.BloquearDado(false);
        }

        turnoIndex = 0;
        turnoEnCurso = false;

        StartCoroutine(LoopTurnos());
    }

    IEnumerator LoopTurnos()
    {
        while (true)
        {
            if (turnoEnCurso) { yield return null; continue; }

            var actual = turnOrder[turnoIndex];

            // Saltos de turno
            if (turnosSaltados.TryGetValue(actual, out int restan) && restan > 0)
            {
                turnosSaltados[actual] = restan - 1;
                Debug.Log($"[{Nombre(actual)}] salta turno. Restan: {turnosSaltados[actual]}");
                AvanzarTurno();
                continue;
            }

            turnoEnCurso = true;

            if (actual == jugador)
            {
                if (dadoUI != null) dadoUI.BloquearDado(false);
                Debug.Log("Turno del JUGADOR. Lanza el dado.");
            }
            else
            {
                if (dadoUI != null) dadoUI.BloquearDado(true);
                StartCoroutine(TurnoBot(actual));
            }

            yield return null;
        }
    }

    void OnJugadorTiroDado(int numero)
    {
        StartCoroutine(ResolverTurno(jugador, numero, true));
    }

    IEnumerator TurnoBot(MovePlayer bot)
    {
        int numero = Random.Range(1, 7);
        Debug.Log($"Turno BOT [{Nombre(bot)}]: tira {numero}");
        yield return StartCoroutine(ResolverTurno(bot, numero, false));
    }

    IEnumerator ResolverTurno(MovePlayer peon, int pasos, bool esHumano)
    {
        if (dadoUI != null) dadoUI.BloquearDado(true);

        // 1) Mover hacia adelante
        yield return StartCoroutine(peon.MoverAdelante(pasos));

        // 2) Delay suave
        yield return new WaitForSeconds(0.25f);

        // 3) Resolver según tipo de casilla
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
                Debug.Log("Casilla NEUTRAL.");
                TerminarTurnoORepetir(peon, esHumano);
                break;

            case Tile.TipoCasilla.Pregunta:
            {
                bool? resultado = null;
                cardEffectManager.HacerPregunta(tile.categoria, esHumano, ProbAciertoBots(), (bool correcta) => resultado = correcta);
                while (resultado == null) yield return null;

                if (resultado == false) // falló
                    yield return StartCoroutine(peon.MoverAtras(pasos));

                TerminarTurnoORepetir(peon, esHumano);
                break;
            }

            case Tile.TipoCasilla.Beneficio:
                yield return StartCoroutine(cardEffectManager.EjecutarBeneficioAleatorio(peon));
                TerminarTurnoORepetir(peon, esHumano);
                break;

            case Tile.TipoCasilla.Penalidad:
                yield return StartCoroutine(cardEffectManager.EjecutarPenalidadAleatoria(peon));
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
                if (dadoUI != null) dadoUI.BloquearDado(false);
            }
            else
            {
                StartCoroutine(TurnoBot(peon));
            }

            turnoEnCurso = false;
            return;
        }

        AvanzarTurno();

        if (turnOrder[turnoIndex] == jugador && dadoUI != null)
            dadoUI.BloquearDado(false);

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

    // Llamadas desde CardEffectManager (efectos persistentes)
    public void MarcarRepetirTirada(MovePlayer peon) => repetirTirada.Add(peon);

    public void AplicarSaltarTurnos(MovePlayer peon, int turnos)
    {
        if (!turnosSaltados.ContainsKey(peon)) turnosSaltados[peon] = 0;
        turnosSaltados[peon] += Mathf.Max(1, turnos);
        Debug.Log($"[{Nombre(peon)}] perderá {turnosSaltados[peon]} turno(s).");
    }

    string Nombre(MovePlayer p) => p != null ? p.gameObject.name : "Peon";
}
