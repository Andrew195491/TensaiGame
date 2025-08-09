using UnityEngine;

public class Tile : MonoBehaviour
{
    public enum Categoria
    {
        Historia,
        Geografia,
        Ciencia,

        // Casillas especiales 
        neutral,
        Benefits,
        Penalty
    }
    
    public Categoria categoria;

}
