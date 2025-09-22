using UnityEngine;

public class Tile : MonoBehaviour
{
    public enum TipoCasilla { Pregunta, Neutral, Beneficio, Penalidad }
    public enum Categoria { Historia, Geografia, Ciencia }

    [Header("Tipo de casilla")]
    public TipoCasilla tipo = TipoCasilla.Pregunta;

    [Header("Solo si es Pregunta")]
    public Categoria categoria = Categoria.Historia;
}
