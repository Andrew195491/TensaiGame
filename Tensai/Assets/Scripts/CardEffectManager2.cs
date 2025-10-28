
// ============================================
// CardEffectManager2.cs (Actualizado)
// ============================================
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class CardEffectManager2 : MonoBehaviour
{
    [Header("Origen de datos")]
    [Tooltip("Si se deja vacío, usa Resources/cartas.json")]
    public TextAsset cartasJsonOverride;

    [Header("UI")]
    public CartaUI2 cartaUI;

    [Header("Opcional")]
    public GameManager2 gameManager;

    private List<Carta2> baseHistoria = new();
    private List<Carta2> baseGeografia = new();
    private List<Carta2> baseCiencia = new();
    private readonly Dictionary<Tile2.Categoria, List<Carta2>> baraja = new();

    private List<CartaEntry2> beneficios = new();
    private List<CartaEntry2> penalidades = new();

    void Awake()
    {
        CargarDesdeJson();
        InicializarBarajas();
    }

    void CargarDesdeJson()
    {
        string json;
        if (cartasJsonOverride != null) json = cartasJsonOverride.text;
        else
        {
            TextAsset ta = Resources.Load<TextAsset>("cartas");
            if (ta == null) { Debug.LogError("Falta Resources/cartas.json"); return; }
            json = ta.text;
        }

        var db = JsonUtility.FromJson<CartasDB2>(json);
        if (db == null) { Debug.LogError("JSON inválido"); return; }

        baseHistoria = db.historia != null ? new List<Carta2>(db.historia) : new List<Carta2>();
        baseGeografia = db.geografia != null ? new List<Carta2>(db.geografia) : new List<Carta2>();
        baseCiencia = db.ciencia != null ? new List<Carta2>(db.ciencia) : new List<Carta2>();
        beneficios = db.beneficios != null ? new List<CartaEntry2>(db.beneficios) : new List<CartaEntry2>();
        penalidades = db.penalidades != null ? new List<CartaEntry2>(db.penalidades) : new List<CartaEntry2>();

        Debug.Log($"[JSON] H={baseHistoria.Count} G={baseGeografia.Count} C={baseCiencia.Count} " +
                  $"Beneficios={beneficios.Count} Penalidades={penalidades.Count}");
    }

    void InicializarBarajas()
    {
        baraja.Clear();
        baraja[Tile2.Categoria.Historia] = new List<Carta2>(baseHistoria);
        baraja[Tile2.Categoria.Geografia] = new List<Carta2>(baseGeografia);
        baraja[Tile2.Categoria.Ciencia] = new List<Carta2>(baseCiencia);
    }

    Carta2 SacarPregunta(Tile2.Categoria cat)
    {
        if (!baraja.ContainsKey(cat))
            baraja[cat] = new List<Carta2>();

        if (baraja[cat].Count == 0)
        {
            switch (cat)
            {
                case Tile2.Categoria.Historia: baraja[cat].AddRange(baseHistoria); break;
                case Tile2.Categoria.Geografia: baraja[cat].AddRange(baseGeografia); break;
                case Tile2.Categoria.Ciencia: baraja[cat].AddRange(baseCiencia); break;
            }
        }

        if (baraja[cat].Count == 0) return null;

        int idx = Random.Range(0, baraja[cat].Count);
        Carta2 c = baraja[cat][idx];
        baraja[cat].RemoveAt(idx);
        return c;
    }

    int OpcionAleatoriaDistintaDe(int correcta1a3)
    {
        int pick;
        do { pick = Random.Range(1, 4); } while (pick == correcta1a3);
        return pick;
    }

    public void HacerPregunta(Tile2.Categoria categoria, bool esHumano, float probAciertoBot, Action<bool> onRespondida)
    {
        var carta = SacarPregunta(categoria);
        if (carta == null)
        {
            Debug.LogWarning($"Sin preguntas en {categoria}. Se asume correcta.");
            onRespondida?.Invoke(true);
            return;
        }

        if (cartaUI == null)
        {
            bool sim = esHumano ? true : (Random.value < probAciertoBot);
            onRespondida?.Invoke(sim);
            return;
        }

        if (esHumano)
        {
            cartaUI.MostrarCartaJugador(carta, onRespondida);
        }
        else
        {
            bool correcta = Random.value < probAciertoBot;
            int seleccion = correcta ? carta.respuestaCorrecta : OpcionAleatoriaDistintaDe(carta.respuestaCorrecta);
            cartaUI.MostrarCartaBot(carta, seleccion, correcta, () => onRespondida?.Invoke(correcta));
        }
    }

    CartaEntry2 RandomBeneficio() => beneficios.Count > 0 ? beneficios[Random.Range(0, beneficios.Count)] : null;
    CartaEntry2 RandomPenalidad() => penalidades.Count > 0 ? penalidades[Random.Range(0, penalidades.Count)] : null;

    public IEnumerator EjecutarBeneficioAleatorio(MovePlayer2 peon, bool esHumano)
    {
        var e = RandomBeneficio();
        if (e == null)
        {
            Debug.Log("[EFECTO] No hay beneficios.");
            yield break;
        }

        string titulo = string.IsNullOrEmpty(e.nombre) ? "Beneficio" : e.nombre;
        string desc = e.descripcion;

        if (esHumano)
        {
            bool? decision = null;
            cartaUI.MostrarBeneficioInteractivo(titulo, desc, d => decision = d);
            while (decision == null) yield return null;

            if (decision.Value)
            {
                if (gameManager != null && gameManager.TryStoreBenefit(peon, (e.nombre ?? "").ToLowerInvariant()))
                    Debug.Log("[EFECTO] Beneficio guardado en inventario.");
                else
                    Debug.LogWarning("[EFECTO] Inventario lleno. No se guarda el beneficio.");
            }
            else
            {
                Debug.Log("[EFECTO] Beneficio descartado.");
            }
        }
        else
        {
            bool uiCerrada = false;
            cartaUI.MostrarEfectoAuto(titulo, desc, true, () => uiCerrada = true);
            while (!uiCerrada) yield return null;

            if (gameManager != null && gameManager.TryStoreBenefit(peon, (e.nombre ?? "").ToLowerInvariant()))
                Debug.Log("[EFECTO] Bot guardó el beneficio (inventario).");
            else
                Debug.Log("[EFECTO] Bot no guardó (inventario lleno/sin UI).");
        }
    }

    public IEnumerator EjecutarPenalidadAleatoria(MovePlayer2 peon, bool esHumano)
    {
        var e = RandomPenalidad();
        if (e == null)
        {
            Debug.Log("[EFECTO] No hay penalidades.");
            yield break;
        }

        string titulo = string.IsNullOrEmpty(e.nombre) ? "Penalidad" : e.nombre;
        string desc = e.descripcion;

        if (esHumano)
        {
            bool pulsado = false;
            cartaUI.MostrarPenalidadInteractiva(titulo, desc, () => pulsado = true);
            while (!pulsado) yield return null;
            yield return StartCoroutine(EjecutarEfectoCoroutine(e, peon));
        }
        else
        {
            bool uiCerrada = false;
            cartaUI.MostrarEfectoAuto(titulo, desc, false, () => uiCerrada = true);
            while (!uiCerrada) yield return null;
            yield return StartCoroutine(EjecutarEfectoCoroutine(e, peon));
        }
    }

    IEnumerator EjecutarEfectoCoroutine(CartaEntry2 e, MovePlayer2 peon)
    {
        string nombre = string.IsNullOrEmpty(e.nombre) ? e.efecto.ToString() : e.nombre;
        Debug.Log($"[EFECTO] Ejecutando: {nombre} | Tipo={e.efecto} | pasos={e.pasos} | turnos={e.turnos} | peón={peon.name}");

        switch (e.efecto)
        {
            case TipoEfecto.MoverRelativo:
                if (e.pasos > 0)
                {
                    Debug.Log($"[EFECTO] Avanzar {e.pasos} casillas.");
                    yield return StartCoroutine(peon.MoverAdelante(e.pasos));
                }
                else if (e.pasos < 0)
                {
                    Debug.Log($"[EFECTO] Retroceder {-e.pasos} casillas.");
                    yield return StartCoroutine(peon.MoverAtras(-e.pasos));
                }
                else
                {
                    Debug.Log("[EFECTO] 0 pasos (sin movimiento).");
                }
                break;

            case TipoEfecto.RepetirTirada:
                if (gameManager != null) gameManager.MarcarRepetirTirada(peon);
                Debug.Log("[EFECTO] Repetir tirada marcado.");
                break;

            case TipoEfecto.SaltarTurnos:
                int turnos = Mathf.Max(1, e.turnos);
                if (gameManager != null) gameManager.AplicarSaltarTurnos(peon, turnos);
                Debug.Log($"[EFECTO] Saltar {turnos} turno(s).");
                break;
        }

        Debug.Log($"[EFECTO] Finalizado: {nombre}");
    }
}
