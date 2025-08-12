using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public enum DificultadBot { Facil, Medio, Dificil }

    [Header("Referencias")]
    public CartaManager cartaManager;
    public DiceController dadoUI;

    [Header("Jugador y Bots")]
    public MovePlayer jugador;                 // tu peón jugador (con MovePlayer)
    [Range(0,3)] public int numeroBots = 0;   // máximo 3 (total 4 jugadores)
    public GameObject[] prefabsBots;          // asigna hasta 3 prefabs distintos
    public DificultadBot dificultadBots = DificultadBot.Medio;

    private readonly List<MovePlayer> turnOrder = new List<MovePlayer>();
    private int turnoIndex = 0;
    private bool turnoEnCurso = false;

    // offsets para que varias fichas quepan en la misma casilla
    private readonly Vector3[] offsets = new Vector3[] {
        Vector3.zero,
        new Vector3(0.35f, 0f, 0.35f),
        new Vector3(-0.35f, 0f, 0.35f),
        new Vector3(0.35f, 0f, -0.35f)
    };

    void Start()
    {
        // Orden de turnos: Jugador primero y luego bots
        if (jugador == null)
        {
            Debug.LogError("Asigna el MovePlayer del jugador en GameManager.");
            return;
        }

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

            // Colocar en casilla 0 con pequeño offset
            pm.transform.position += offsets[Mathf.Min(i+1, offsets.Length-1)];
            turnOrder.Add(pm);
        }

        // Conectar dado UI
        dadoUI.OnRolled = OnJugadorTiroDado;
        dadoUI.BloquearDado(false);

        // Inicia turno del jugador
        turnoIndex = 0;
        turnoEnCurso = false;
        StartCoroutine(LoopTurnos());
    }

    IEnumerator LoopTurnos()
    {
        while (true)
        {
            if (turnoEnCurso) { yield return null; continue; }

            turnoEnCurso = true;
            MovePlayer actual = turnOrder[turnoIndex];

            if (actual == jugador)
            {
                // Turno del jugador: habilitar dado y esperar a que tire
                dadoUI.BloquearDado(false);
                Debug.Log("Turno del JUGADOR. Lanza el dado.");
                // Esperar a que OnJugadorTiroDado dispare la corrutina de turno
            }
            else
            {
                // Turno de un bot
                dadoUI.BloquearDado(true);
                StartCoroutine(TurnoBot(actual));
            }

            yield return null;
        }
    }

    void OnJugadorTiroDado(int numero)
    {
        // El botón ya quedó bloqueado dentro del dado al tirar
        StartCoroutine(ResolverTurno(jugador, numero, true));
    }

    IEnumerator TurnoBot(MovePlayer bot)
    {
        int numero = Random.Range(1, 7);
        Debug.Log($"Turno BOT: tira {numero}");
        yield return StartCoroutine(ResolverTurno(bot, numero, false));
    }

    IEnumerator ResolverTurno(MovePlayer peon, int pasos, bool esHumano)
    {
        // Bloquear dado mientras se mueve/pregunta
        dadoUI.BloquearDado(true);

        // 1) Mover hacia adelante
        yield return StartCoroutine(peon.MoverAdelante(pasos));

        // 2) Pequeño delay
        yield return new WaitForSeconds(0.25f);

        // 3) Preguntar
        bool? resultado = null; // null = esperando
        var categoria = peon.CategoriaActual();

        cartaManager.HacerPregunta(categoria, esHumano, ProbAciertoBots(), (bool correcta) =>
        {
            resultado = correcta;
        });

        // Esperar a que se responda (o simular en bot)
        while (resultado == null) yield return null;

        // 4) Si fue incorrecta, retroceder lo movido
        if (resultado == false)
        {
            yield return StartCoroutine(peon.MoverAtras(pasos));
        }

        // 5) Fin de turno: pasar al siguiente
        AvanzarTurno();

        // Si le toca al jugador, permitir volver a tirar
        if (turnOrder[turnoIndex] == jugador)
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
}
