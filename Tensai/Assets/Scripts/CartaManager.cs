using UnityEngine;
using System;
using System.Collections.Generic;

public class CartaManager : MonoBehaviour
{
    [Header("Cartas por categoría")]
    public List<Carta> historia;
    public List<Carta> geografia;
    public List<Carta> ciencia;

    [Header("UI")]
    public CartaUI cartaUI;

    // barajas temporales para no repetir hasta agotar
    private Dictionary<Tile.Categoria, List<Carta>> baraja = new Dictionary<Tile.Categoria, List<Carta>>();

    void Awake()
    {
        baraja[Tile.Categoria.Historia] = new List<Carta>(historia);
        baraja[Tile.Categoria.Geografia] = new List<Carta>(geografia);
        baraja[Tile.Categoria.Ciencia]  = new List<Carta>(ciencia);
    }

    Carta SacarCarta(Tile.Categoria cat)
    {
        if (!baraja.ContainsKey(cat))
            baraja[cat] = new List<Carta>();

        if (baraja[cat].Count == 0)
        {
            // reponer
            switch (cat)
            {
                case Tile.Categoria.Historia:  baraja[cat].AddRange(historia); break;
                case Tile.Categoria.Geografia: baraja[cat].AddRange(geografia); break;
                case Tile.Categoria.Ciencia:   baraja[cat].AddRange(ciencia); break;
            }
        }

        if (baraja[cat].Count == 0) return null;

        int idx = UnityEngine.Random.Range(0, baraja[cat].Count);
        Carta c = baraja[cat][idx];
        baraja[cat].RemoveAt(idx);
        return c;
    }

    /// <summary>
    /// Si es humano: muestra UI y devuelve en callback si acierta. 
    /// Si es bot: simula respuesta con probabilidad de acierto y llama callback.
    /// </summary>
    public void HacerPregunta(Tile.Categoria categoria, bool esHumano, float probAciertoBot, Action<bool> onRespondida)
    {
        Carta carta = SacarCarta(categoria);
        if (carta == null)
        {
            Debug.LogWarning($"No hay cartas en {categoria}");
            onRespondida?.Invoke(true); // no penalizamos
            return;
        }

        if (esHumano)
        {
            if (cartaUI == null)
            {
                Debug.LogWarning("CartaUI no asignado. Se asume correcta.");
                onRespondida?.Invoke(true);
                return;
            }

            cartaUI.MostrarCarta(carta, onRespondida);
        }
        else
        {
            // BOT: decide correcta según probabilidad, sin UI
            bool correcta = UnityEngine.Random.value < probAciertoBot;
            Debug.Log($"BOT responde {(correcta ? "✅ correcta" : "❌ incorrecta")}");
            onRespondida?.Invoke(correcta);
        }
    }
}
