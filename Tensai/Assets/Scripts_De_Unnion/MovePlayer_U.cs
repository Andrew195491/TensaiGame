using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;

/// <summary>
/// Controla el movimiento del jugador o bot en el tablero de juego.
/// Soporta:
/// - Avanzar y retroceder con animación de salto
/// - Teletransporte directo a una casilla
/// - Consulta de la casilla actual y su categoría
/// - Integración con el dado y el sistema de cartas
/// ACTUALIZADO: Compatible con Tile_U y CartaManager_U
/// </summary>
public class MovePlayer_U : MonoBehaviour
{
    // ============================================
    // SECCIÓN 1: REFERENCIAS Y ESTADO
    // ============================================

    [Header("Referencias")]
    [Tooltip("Referencia al controlador del dado para bloquearlo durante movimientos.")]
    public DiceController_U dado;

    [Header("Movimiento")]
    [Tooltip("Altura del salto durante la animación.")]
    public float jumpHeight = 1.5f;

    [Tooltip("Duración de cada salto individual.")]
    public float moveDuration = 0.5f;

    [Header("Estado del jugador")]
    [Tooltip("Índice actual de la casilla donde se encuentra el jugador.")]
    public int currentIndex = 0;

    private Transform[] tiles;
    private bool isMoving = false;
    private int ultimaCantidadMovida = 0;

    // ============================================
    // SECCIÓN 2: INICIALIZACIÓN
    // ============================================

    void Start()
    {
        CargarTilesDesdeTablero();
        if (tiles.Length > 0)
            transform.position = tiles[currentIndex].position + Vector3.up * 1f;
    }

    // ============================================
    // SECCIÓN 3: CARGA Y ORDENAMIENTO DE CASILLAS
    // ============================================

    void CargarTilesDesdeTablero()
    {
        Transform tablero = GameObject.Find("Board")?.transform;
        if (tablero == null)
        {
            Debug.LogError("No se encontró el objeto 'Board'");
            tiles = new Transform[0];
            return;
        }

        tiles = new Transform[tablero.childCount];
        for (int i = 0; i < tablero.childCount; i++)
            tiles[i] = tablero.GetChild(i);

        // Ordenar casillas numéricamente según su nombre (Tile_1, Tile_2, etc.)
        System.Array.Sort(tiles, (a, b) =>
        {
            int na = ExtraerNumero(a.name);
            int nb = ExtraerNumero(b.name);
            return na.CompareTo(nb);
        });
    }

    int ExtraerNumero(string nombre)
    {
        string t = Regex.Match(nombre, @"\d+").Value;
        return int.TryParse(t, out int n) ? n : 0;
    }


    // ... (Código anterior sin cambios) ...

    // ============================================
    // SECCIÓN 4: MOVIMIENTO HACIA ADELANTE
    // ============================================

    public IEnumerator JumpMultipleTimes(int cantidad)
    {
        if (isMoving || tiles.Length == 0 || cantidad <= 0) yield break;

        ultimaCantidadMovida = cantidad;

        for (int i = 0; i < cantidad; i++)
        {
            currentIndex = (currentIndex + 1) % tiles.Length;
            yield return JumpTo(tiles[currentIndex].position);
        }

        // Code name "Fase Final"
        // -----------------------------------------------------------------
        // SE ELIMINA LA LÓGICA DE RESOLUCIÓN DE CASILLAS DE AQUÍ.
        // La responsabilidad de resolver la casilla (Pregunta, Beneficio, etc.)
        // se centraliza en 'GameManager_U.ResolverTurno' para evitar
        // duplicidad de código y asegurar el flujo de turnos correcto.
        // El GameManager esperará a que el salto termine y LUEGO
        // resolverá la casilla.
        // -----------------------------------------------------------------

        /*
        // CÓDIGO ANTIGUO ELIMINADO:
        yield return new WaitForSeconds(0.5f);
        Tile_U tile = GetCurrentTile(); 
        if (tile != null)
        {
            if (tile.tipo == Tile_U.TipoCasilla.Neutral) { ... }
            else if (tile.tipo == Tile_U.TipoCasilla.Beneficio) { ... }
            else if (tile.tipo == Tile_U.TipoCasilla.Penalidad) { ... }
            else if (tile.tipo == Tile_U.TipoCasilla.Pregunta) { ... }
        }
        */
    }

    // ... (Resto del código sin cambios) ...



    // ============================================
    // SECCIÓN 5: MOVIMIENTO HACIA ATRÁS
    // ============================================

    public IEnumerator Retroceder(int pasos)
    {
        if (isMoving || tiles.Length == 0 || pasos <= 0) yield break;

        if (dado != null)
            dado.BloquearDado(true);

        for (int i = 0; i < pasos; i++)
        {
            currentIndex = (currentIndex - 1 + tiles.Length) % tiles.Length;
            yield return JumpTo(tiles[currentIndex].position);
        }

        if (dado != null)
            dado.BloquearDado(false);
    }

    // ============================================
    // SECCIÓN 6: ANIMACIÓN DE SALTO
    // ============================================

    IEnumerator JumpTo(Vector3 destino)
    {
        isMoving = true;

        Vector3 start = transform.position;
        Vector3 end = destino + Vector3.up * 1f;

        float t = 0f;
        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / moveDuration);

            Vector3 horizontal = Vector3.Lerp(start, end, k);
            float vertical = Mathf.Sin(k * Mathf.PI) * jumpHeight;

            transform.position = horizontal + Vector3.up * vertical;
            yield return null;
        }

        transform.position = end;
        isMoving = false;
    }

    // ============================================
    // SECCIÓN 7: TELETRANSPORTE DIRECTO
    // ============================================

    public IEnumerator IrACasilla(int indiceCasilla)
    {
        if (isMoving) yield break;
        if (dado != null) dado.BloquearDado(true);

        int destino = Mathf.Clamp(indiceCasilla, 0, tiles.Length - 1);
        currentIndex = destino;

        yield return JumpTo(tiles[currentIndex].position);

        Debug.Log($"🏠 Jugador movido a casilla {destino}");

        if (dado != null) dado.BloquearDado(false);
    }

    public void TeleportAIndiceSeguro(int indice)
    {
        Transform tablero = GameObject.Find("Board")?.transform;
        if (tablero == null || tablero.childCount == 0) return;

        indice = Mathf.Clamp(indice, 0, tablero.childCount - 1);
        currentIndex = indice;
        transform.position = tablero.GetChild(currentIndex).position + Vector3.up * 1f;
    }


    // ... (Código anterior sin cambios) ...
    
    // ============================================
    // SECCIÓN 8: CONSULTAS Y UTILIDADES
    // ============================================

    public Tile_U GetCurrentTile() // CAMBIADO: Tile2 → Tile_U
    {
        if (tiles == null || tiles.Length == 0) return null;
        var t = tiles[currentIndex];
        return t != null ? t.GetComponent<Tile_U>() : null; // CAMBIADO
    }

    public Tile_U.Categoria CategoriaActual() // CAMBIADO: Tile2 → Tile_U
    {
        Tile_U tile = GetCurrentTile(); // CAMBIADO
        return tile != null ? tile.categoria : Tile_U.Categoria.Historia; // CAMBIADO
    }

    public void ResponderCarta(bool correcta)
    {
        // Code name "Fase Final"
        // Esta lógica ahora es manejada íntegramente por GameManager_U
        // en la corrutina 'ResolverTurno', que espera el resultado
        // de 'HacerPregunta' y llama a 'Retroceder' si es necesario.
        // Este método queda vacío para evitar conflictos.

        /*
        // CÓDIGO ANTIGUO ELIMINADO:
        if (!correcta)
            StartCoroutine(Retroceder(ultimaCantidadMovida));
        */
    }
}