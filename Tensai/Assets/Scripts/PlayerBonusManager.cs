using UnityEngine;
using System.Collections.Generic;

public class PlayerBonusManager : MonoBehaviour
{
    public static PlayerBonusManager instancia;

    public int maxCartas = 3;
    public List<Carta> cartasBonus = new List<Carta>();

    public BonusUI bonusUI; // Asignar en el inspector

    void Awake()
    {
        instancia = this;
    }

    public void AgregarCarta(Carta nuevaCarta, MovePlayer jugador)
    {
        if (cartasBonus.Count < maxCartas)
        {
            cartasBonus.Add(nuevaCarta);
            bonusUI.ActualizarUI(cartasBonus);
            Debug.Log($"{jugador.name} obtuvo una carta bonus: {nuevaCarta.pregunta}");
        }
        else
        {
            Debug.Log("⚠ Inventario lleno: deberías usar una antes de agregar otra.");
        }
    }


    public void UsarCarta(int indice, MovePlayer jugador)
    {
        if (indice < 0 || indice >= cartasBonus.Count) return;

        Carta carta = cartasBonus[indice];
        AplicarEfecto(carta, jugador);

        cartasBonus.RemoveAt(indice);
        bonusUI.ActualizarUI(cartasBonus);
    }

    private void AplicarEfecto(Carta carta, MovePlayer jugador)
    {
        // Aquí definimos efectos según el texto de la carta o algún campo
        if (carta.pregunta.Contains("Avanzas"))
        {
            jugador.StartCoroutine(jugador.JumpMultipleTimes(2));
        }
        else if (carta.pregunta.Contains("Retrocedes"))
        {
            jugador.StartCoroutine(jugador.Retroceder(3));
        }
    }
}
