using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random; // evita ambigüedad con System.Random

public class CardEffectManager : MonoBehaviour
{
    [Header("Origen de datos")]
    [Tooltip("Si se deja vacío, usa Resources/cartas.json")]
    public TextAsset cartasJsonOverride;

    [Header("UI")]
    public CartaUI cartaUI;

    [Header("Opcional")]
    public GameManager gameManager; // arrástralo o lo asigna el GameManager en Start

    // Banco base (persistente) y barajas (consumibles sin repetición)
    private List<Carta> baseHistoria = new();
    private List<Carta> baseGeografia = new();
    private List<Carta> baseCiencia  = new();

    private readonly Dictionary<Tile.Categoria, List<Carta>> baraja = new();

    // Efectos (permiten repetición)
    private List<CartaEntry> beneficios  = new();
    private List<CartaEntry> penalidades = new();

    // ---------------- Ciclo de vida ----------------
    void Awake()
    {
        CargarDesdeJson();
        InicializarBarajas();
    }

    // ---------------- Carga JSON ----------------
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

        var db = JsonUtility.FromJson<CartasDB>(json);
        if (db == null) { Debug.LogError("JSON inválido"); return; }

        baseHistoria  = db.historia  != null ? new List<Carta>(db.historia)  : new List<Carta>();
        baseGeografia = db.geografia != null ? new List<Carta>(db.geografia) : new List<Carta>();
        baseCiencia   = db.ciencia   != null ? new List<Carta>(db.ciencia)   : new List<Carta>();

        beneficios  = db.beneficios  != null ? new List<CartaEntry>(db.beneficios)  : new List<CartaEntry>();
        penalidades = db.penalidades != null ? new List<CartaEntry>(db.penalidades) : new List<CartaEntry>();

        Debug.Log($"[JSON] H={baseHistoria.Count} G={baseGeografia.Count} C={baseCiencia.Count} " +
                  $"Beneficios={beneficios.Count} Penalidades={penalidades.Count}");
    }

    void InicializarBarajas()
    {
        baraja.Clear();
        baraja[Tile.Categoria.Historia]  = new List<Carta>(baseHistoria);
        baraja[Tile.Categoria.Geografia] = new List<Carta>(baseGeografia);
        baraja[Tile.Categoria.Ciencia]   = new List<Carta>(baseCiencia);
    }

    // ---------------- Preguntas ----------------
    Carta SacarPregunta(Tile.Categoria cat)
    {
        if (!baraja.ContainsKey(cat))
            baraja[cat] = new List<Carta>();

        // Recargar cuando se agote (sin repetición hasta pasar por todas)
        if (baraja[cat].Count == 0)
        {
            switch (cat)
            {
                case Tile.Categoria.Historia:  baraja[cat].AddRange(baseHistoria);  break;
                case Tile.Categoria.Geografia: baraja[cat].AddRange(baseGeografia); break;
                case Tile.Categoria.Ciencia:   baraja[cat].AddRange(baseCiencia);   break;
            }
        }

        if (baraja[cat].Count == 0) return null;

        int idx = Random.Range(0, baraja[cat].Count);
        Carta c = baraja[cat][idx];
        baraja[cat].RemoveAt(idx);
        return c;
    }

    int OpcionAleatoriaDistintaDe(int correcta1a3)
    {
        int pick;
        do { pick = Random.Range(1, 4); } while (pick == correcta1a3);
        return pick;
    }

    /// <summary>
    /// Muestra/Simula pregunta y devuelve true/false en callback según acierto.
    /// </summary>
    public void HacerPregunta(Tile.Categoria categoria, bool esHumano, float probAciertoBot, Action<bool> onRespondida)
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

    // ---------------- Efectos (beneficios / penalidades) ----------------
    CartaEntry RandomBeneficio()  => beneficios.Count  > 0 ? beneficios[Random.Range(0, beneficios.Count)]   : null;
    CartaEntry RandomPenalidad()  => penalidades.Count > 0 ? penalidades[Random.Range(0, penalidades.Count)] : null;

    /// <summary>
    /// Beneficio: HUMANO puede Guardar (se añade al inventario y no se aplica) o Descartar.
    /// BOT: por defecto intenta guardar si hay espacio, si no lo ignora. (No aplica).
    /// </summary>
    public IEnumerator EjecutarBeneficioAleatorio(MovePlayer peon, bool esHumano)
    {
        var e = RandomBeneficio();
        if (e == null)
        {
            Debug.Log("[EFECTO] No hay beneficios.");
            yield break;
        }

        string titulo = string.IsNullOrEmpty(e.nombre) ? "Beneficio" : e.nombre;
        string desc   = e.descripcion;

        if (esHumano)
        {
            bool? decision = null; // true=guardar, false=descartar
            cartaUI.MostrarBeneficioInteractivo(titulo, desc, d => decision = d);
            while (decision == null) yield return null;

            if (decision.Value)
            {
                // Guardar en inventario (NO se aplica ahora)
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
            // Bot: muestra info y auto-cierra
            bool uiCerrada = false;
            cartaUI.MostrarEfectoAuto(titulo, desc, true, () => uiCerrada = true);
            while (!uiCerrada) yield return null;

            // Intenta guardar en inventario compartido o ignora si no hay.
            if (gameManager != null && gameManager.TryStoreBenefit(peon, (e.nombre ?? "").ToLowerInvariant()))
                Debug.Log("[EFECTO] Bot guardó el beneficio (inventario).");
            else
                Debug.Log("[EFECTO] Bot no guardó (inventario lleno/sin UI).");
        }
    }

    /// <summary>
    /// Penalidad: HUMANO pulsa Aceptar para aplicar. BOT aplica tras mostrarla.
    /// </summary>
    public IEnumerator EjecutarPenalidadAleatoria(MovePlayer peon, bool esHumano)
    {
        var e = RandomPenalidad();
        if (e == null)
        {
            Debug.Log("[EFECTO] No hay penalidades.");
            yield break;
        }

        string titulo = string.IsNullOrEmpty(e.nombre) ? "Penalidad" : e.nombre;
        string desc   = e.descripcion;

        if (esHumano)
        {
            bool pulsado = false;
            cartaUI.MostrarPenalidadInteractiva(titulo, desc, () => pulsado = true);
            while (!pulsado) yield return null;

            // Aplica al aceptar
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

    // ---------------- Aplicación de efectos ----------------
    IEnumerator EjecutarEfectoCoroutine(CartaEntry e, MovePlayer peon)
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
