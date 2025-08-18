using UnityEngine;
using System.Collections.Generic;

public class CartaManager : MonoBehaviour
{
    public List<Carta> historia;
    public List<Carta> geografia;
    public List<Carta> ciencia;

    // Cartas especiales
    public List<Carta> benefits;
    public List<Carta> penalty;

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
            Tile.Categoria.neutral => null, // No hay cartas para neutral
            Tile.Categoria.Benefits => benefits, 
            Tile.Categoria.Penalty => penalty,
            _ => null
        };

        if (lista == null || lista.Count == 0)
            return null;

        int index = Random.Range(0, lista.Count);
        return lista[index];
    }

    // Ejecucion de cartas especiales
    public void EjecutarAccionEspecial(Tile.Categoria categoria, MovePlayer jugador)
    {
        if (cartaUI == null) return;

        switch (categoria)
        {
            // casos de casillas neutrales
            case Tile.Categoria.neutral:
                cartaUI.MostrarMensajeEspecial("Casilla neutral: No pasa nada.", () => {
                    Debug.Log("Neutral: turno terminado");
                });
                break;

            // casos de casillas de beneficios
/*
            case Tile.Categoria.Benefits:
                cartaUI.MostrarMensajeEspecial("Casilla de Beneficios: Avança 2 casillas.", () =>
                {
                    jugador.StartCoroutine(jugador.JumpMultipleTimes(2));
                    Debug.Log("Casilla de beneficio: Avanzas 2 casillas.");
                });
                break;
/*
*/
            case Tile.Categoria.Benefits:
                Carta cartaBonus = ObtenerCartaAleatoria(Tile.Categoria.Benefits);
                if (cartaBonus != null)
                    PlayerBonusManager.instancia.AgregarCarta(cartaBonus, jugador);
                break;
/*
*/

            // casos de casillas de penalidad
            case Tile.Categoria.Penalty:
                cartaUI.MostrarMensajeEspecial("Casilla de penalidad: Retrocedes 3 casillas.", () =>
                {
                    jugador.StartCoroutine(jugador.Retroceder(3));
                    Debug.Log("Casilla de penalidad: Retrocedes 3 casillas.");
                });
                break;
        }
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
