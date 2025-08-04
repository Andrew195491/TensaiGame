using UnityEngine;
using System.Collections.Generic;

public class CartaManager : MonoBehaviour
{
    public List<Carta> historia;
    public List<Carta> geografia;
    public List<Carta> ciencia;
    public CartaUI cartaUI;

    public static CartaManager instancia;
    public DiceController dadoController; // Arrástralo desde el inspector


    void Awake()
    {
        instancia = this;
    }

    // ✅ ESTE MÉTODO DEBE EXISTIR
    private Carta ObtenerCartaAleatoria(Tile.Categoria categoria)
    {
        List<Carta> lista = categoria switch
        {
            Tile.Categoria.Historia => historia,
            Tile.Categoria.Geografia => geografia,
            Tile.Categoria.Ciencia => ciencia,
            _ => null
        };

        if (lista == null || lista.Count == 0)
            return null;

        int index = Random.Range(0, lista.Count);
        return lista[index];
    }

    // ✅ AQUÍ LO USAMOS
public void MostrarCarta(Tile.Categoria categoria, System.Action onRespuestaIncorrecta = null)
{
    Carta carta = ObtenerCartaAleatoria(categoria);

    if (carta != null && cartaUI != null)
    {
        // Bloquear dado mientras se responde
        if (dadoController != null)
            dadoController.BloquearDado(true);

        cartaUI.MostrarCarta(carta, (int respuestaSeleccionada) =>
        {
            bool esCorrecta = respuestaSeleccionada == carta.respuestaCorrecta;
            Debug.Log(esCorrecta ? "✅ Respuesta correcta" : "❌ Respuesta incorrecta");

            if (!esCorrecta && onRespuestaIncorrecta != null)
                onRespuestaIncorrecta.Invoke();

            // ✅ Activar dado después de responder
            if (dadoController != null)
                dadoController.BloquearDado(false);
        });
    }
    else
    {
        Debug.LogWarning($"No hay carta o UI para la categoría {categoria}");
    }
}

}
