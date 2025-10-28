using UnityEngine;
using System;
using System.Collections.Generic;

public class CartaManager2 : MonoBehaviour
{
    [Header("Origen de datos")]
    [Tooltip("Si se deja vacío, cargará Resources/cartas.json")]
    public TextAsset cartasJsonOverride; // opcional: arrastra un TextAsset distinto si quieres

    [Header("UI")]
    public CartaUI2 cartaUI; // ⬅️ CAMBIO: CartaUI → CartaUI2

    // Copias cargadas desde JSON
    private List<Carta2> historia = new(); // ⬅️ CAMBIO: Carta → Carta2
    private List<Carta2> geografia = new(); // ⬅️ CAMBIO: Carta → Carta2
    private List<Carta2> ciencia = new(); // ⬅️ CAMBIO: Carta → Carta2

    // Barajas temporales para no repetir
    private readonly Dictionary<Tile2.Categoria, List<Carta2>> baraja = new(); // ⬅️ CAMBIO: Tile → Tile2, Carta → Carta2

    void Awake()
    {
        CargarCartasDesdeJson();
        InicializarBarajas();
    }

    void CargarCartasDesdeJson()
    {
        try
        {
            string json;
            if (cartasJsonOverride != null)
            {
                json = cartasJsonOverride.text;
            }
            else
            {
                // Busca Resources/cartas.json
                TextAsset ta = Resources.Load<TextAsset>("cartas");
                if (ta == null)
                {
                    Debug.LogError("No se encontró Resources/cartas.json. Crea la carpeta Resources y coloca ahí el archivo.");
                    return;
                }
                json = ta.text;
            }

            CartasDB2 db = JsonUtility.FromJson<CartasDB2>(json); // ⬅️ CAMBIO: CartasDB → CartasDB2
            if (db == null)
            {
                Debug.LogError("No se pudo parsear el JSON (¿estructura correcta?).");
                return;
            }

            historia  = db.historia  != null ? new List<Carta2>(db.historia)   : new List<Carta2>(); // ⬅️ CAMBIO: Carta → Carta2
            geografia = db.geografia != null ? new List<Carta2>(db.geografia)  : new List<Carta2>(); // ⬅️ CAMBIO: Carta → Carta2
            ciencia   = db.ciencia   != null ? new List<Carta2>(db.ciencia)    : new List<Carta2>(); // ⬅️ CAMBIO: Carta → Carta2

            Debug.Log($"Cartas cargadas: H={historia.Count} G={geografia.Count} C={ciencia.Count}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error cargando cartas JSON: {e.Message}\n{e.StackTrace}");
        }
    }

    void InicializarBarajas()
    {
        baraja.Clear();
        baraja[Tile2.Categoria.Historia]  = new List<Carta2>(historia); // ⬅️ CAMBIO: Tile → Tile2, Carta → Carta2
        baraja[Tile2.Categoria.Geografia] = new List<Carta2>(geografia); // ⬅️ CAMBIO: Tile → Tile2, Carta → Carta2
        baraja[Tile2.Categoria.Ciencia]   = new List<Carta2>(ciencia); // ⬅️ CAMBIO: Tile → Tile2, Carta → Carta2
    }

    Carta2 SacarCarta(Tile2.Categoria cat) // ⬅️ CAMBIO: Carta → Carta2, Tile → Tile2
    {
        if (!baraja.ContainsKey(cat))
            baraja[cat] = new List<Carta2>(); // ⬅️ CAMBIO: Carta → Carta2

        // Reponer si se agotó
        if (baraja[cat].Count == 0)
        {
            switch (cat)
            {
                case Tile2.Categoria.Historia:  baraja[cat].AddRange(historia);  break; // ⬅️ CAMBIO: Tile → Tile2
                case Tile2.Categoria.Geografia: baraja[cat].AddRange(geografia); break; // ⬅️ CAMBIO: Tile → Tile2
                case Tile2.Categoria.Ciencia:   baraja[cat].AddRange(ciencia);   break; // ⬅️ CAMBIO: Tile → Tile2
            }
        }

        if (baraja[cat].Count == 0) return null;

        int idx = UnityEngine.Random.Range(0, baraja[cat].Count);
        Carta2 c = baraja[cat][idx]; // ⬅️ CAMBIO: Carta → Carta2
        baraja[cat].RemoveAt(idx);
        return c;
    }

    int OpcionAleatoriaDistintaDe(int correcta1a3)
    {
        int pick;
        do { pick = UnityEngine.Random.Range(1, 4); } while (pick == correcta1a3);
        return pick;
    }

    /// <summary>
    /// Si es humano: muestra UI interactiva con colores, cierra tras delay y devuelve si acertó.
    /// Si es bot: muestra UI no interactiva con su selección coloreada y cierra sola, luego callback.
    /// </summary>
    public void HacerPregunta(Tile2.Categoria categoria, bool esHumano, float probAciertoBot, Action<bool> onRespondida) // ⬅️ CAMBIO: Tile → Tile2
    {
        Carta2 carta = SacarCarta(categoria); // ⬅️ CAMBIO: Carta → Carta2
        if (carta == null)
        {
            Debug.LogWarning($"No hay cartas disponibles en {categoria}. Se asume correcta.");
            onRespondida?.Invoke(true);
            return;
        }

        if (cartaUI == null)
        {
            Debug.LogWarning("CartaUI2 no asignado. Se simula sin UI.");
            bool correctSim = esHumano ? true : (UnityEngine.Random.value < probAciertoBot);
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

            cartaUI.MostrarCartaBot(
                carta,
                seleccion,
                correcta,
                () => onRespondida?.Invoke(correcta)
            );
        }
    }

    // (Opcional) Llamar en runtime si quieres recargar el JSON sin reiniciar escena
    public void RecargarDesdeJson()
    {
        CargarCartasDesdeJson();
        InicializarBarajas();
        Debug.Log("Cartas recargadas desde JSON.");
    }
}