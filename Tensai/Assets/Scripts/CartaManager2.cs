using UnityEngine;
using System;
using System.Collections.Generic;

public class CartaManager2 : MonoBehaviour
{
    [Header("Origen de datos")]
    [Tooltip("Si se deja vacío, cargará Resources/cartas.json")]
    public TextAsset cartasJsonOverride; // opcional: arrastra un TextAsset distinto si quieres

    [Header("UI")]
    public CartaUI2 cartaUI2;

    // Copias cargadas desde JSON
    private List<Carta> historia = new();
    private List<Carta> geografia = new();
    private List<Carta> ciencia = new();

    // Barajas temporales para no repetir
    private readonly Dictionary<Tile.Categoria, List<Carta>> baraja = new();

    void Awake()
    {
       // CargarCartasDesdeJson();
        InicializarBarajas();
    }

/*
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

            CartasDB db = JsonUtility.FromJson<CartasDB>(json);
            if (db == null)
            {
                Debug.LogError("No se pudo parsear el JSON (¿estructura correcta?).");
                return;
            }

            historia  = db.historia  != null ? new List<Carta>(db.historia)   : new List<Carta>();
            geografia = db.geografia != null ? new List<Carta>(db.geografi a)  : new List<Carta>();
            ciencia   = db.ciencia   != null ? new List<Carta>(db.ciencia)    : new List<Carta>();

            Debug.Log($"Cartas cargadas: H={historia.Count} G={geografia.Count} C={ciencia.Count}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error cargando cartas JSON: {e.Message}\n{e.StackTrace}");
        }
    }
*/

    void InicializarBarajas()
    {
        baraja.Clear();
        baraja[Tile.Categoria.Historia]  = new List<Carta>(historia);
        baraja[Tile.Categoria.Geografia] = new List<Carta>(geografia);
        baraja[Tile.Categoria.Ciencia]   = new List<Carta>(ciencia);
    }

    Carta SacarCarta(Tile.Categoria cat)
    {
        if (!baraja.ContainsKey(cat))
            baraja[cat] = new List<Carta>();

        // Reponer si se agotó
        if (baraja[cat].Count == 0)
        {
            switch (cat)
            {
                case Tile.Categoria.Historia:  baraja[cat].AddRange(historia);  break;
                case Tile.Categoria.Geografia: baraja[cat].AddRange(geografia); break;
                case Tile.Categoria.Ciencia:   baraja[cat].AddRange(ciencia);   break;
            }
        }

        if (baraja[cat].Count == 0) return null;

        int idx = UnityEngine.Random.Range(0, baraja[cat].Count);
        Carta c = baraja[cat][idx];
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
    public void HacerPregunta(Tile.Categoria categoria, bool esHumano, float probAciertoBot, Action<bool> onRespondida)
    {
        Carta carta = SacarCarta(categoria);
        if (carta == null)
        {
            Debug.LogWarning($"No hay cartas disponibles en {categoria}. Se asume correcta.");
            onRespondida?.Invoke(true);
            return;
        }

        if (cartaUI2 == null)
        {
            Debug.LogWarning("CartaUI no asignado. Se simula sin UI.");
            bool correctSim = esHumano ? true : (UnityEngine.Random.value < probAciertoBot);
            onRespondida?.Invoke(correctSim);
            return;
        }

        if (esHumano)
        {
            cartaUI2.MostrarCartaJugador(carta, onRespondida);
        }
        else
        {
            bool correcta = UnityEngine.Random.value < probAciertoBot;
            int seleccion = correcta ? carta.respuestaCorrecta : OpcionAleatoriaDistintaDe(carta.respuestaCorrecta);

            cartaUI2.MostrarCartaBot(
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
       // CargarCartasDesdeJson();
        InicializarBarajas();
        Debug.Log("Cartas recargadas desde JSON.");
    }
}
