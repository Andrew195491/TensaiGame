using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;

/// <summary>
/// Controla el movimiento del jugador o bot en el tablero.
/// SOLO mueve. NO resuelve casillas. La resolución la hace GameManager_U
/// después de un movimiento por dado.
/// </summary>
public class MovePlayer_U : MonoBehaviour
{
    [Header("Referencias (opcional)")]
    public DiceController_U dado;

    [Header("Animación")]
    public float jumpHeight = 1.5f;
    public float moveDuration = 0.5f;

    [Header("Estado")]
    public int currentIndex = 0;

    private Transform[] tiles;
    private bool isMoving = false;

    /// <summary>
    /// Si true, al aterrizar NO debe resolverse la casilla (lo usa CartaManager
    /// para movimientos provocados por beneficios/penalidades).
    /// </summary>
    [HideInInspector] public bool ignoreLandingEffects = false;

    void Start()
    {
        CargarTilesDesdeTablero();
        if (tiles.Length > 0)
            transform.position = tiles[Mathf.Clamp(currentIndex, 0, tiles.Length - 1)].position + Vector3.up;
    }

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

        System.Array.Sort(tiles, (a, b) =>
        {
            int na = ExtraerNumero(a.name);
            int nb = ExtraerNumero(b.name);
            return na.CompareTo(nb);
        });

        currentIndex = Mathf.Clamp(currentIndex, 0, Mathf.Max(tiles.Length - 1, 0));
    }

    int ExtraerNumero(string nombre)
    {
        string t = Regex.Match(nombre, @"\d+").Value;
        return int.TryParse(t, out int n) ? n : 0;
    }

    // --------------------------
    // Mover hacia adelante N
    // --------------------------
    public IEnumerator JumpMultipleTimes(int cantidad)
    {

        if (isMoving || tiles.Length == 0 || cantidad <= 0) yield break;

        if (dado != null) dado.BloquearDado(true);

        for (int i = 0; i < cantidad; i++)
        {
            currentIndex = (currentIndex + 1) % tiles.Length;
            yield return JumpTo(tiles[currentIndex].position);
        }

        if (dado != null) dado.BloquearDado(false);
    }

    // --------------------------
    // Retroceder N
    // --------------------------
    public IEnumerator Retroceder(int pasos)
    {
        if (isMoving || tiles.Length == 0 || pasos <= 0) yield break;

        if (dado != null) dado.BloquearDado(true);

        for (int i = 0; i < pasos; i++)
        {
            currentIndex = (currentIndex - 1 + tiles.Length) % tiles.Length;
            yield return JumpTo(tiles[currentIndex].position);
        }

        if (dado != null) dado.BloquearDado(false);
    }

    // --------------------------
    // Teletransporte animado
    // --------------------------
    public IEnumerator IrACasilla(int indiceCasilla)
    {
        if (isMoving || tiles.Length == 0) yield break;

        if (dado != null) dado.BloquearDado(true);

        int destino = Mathf.Clamp(indiceCasilla, 0, tiles.Length - 1);
        currentIndex = destino;
        yield return JumpTo(tiles[currentIndex].position);

        if (dado != null) dado.BloquearDado(false);
    }

    public void TeleportAIndiceSeguro(int indice)
    {
        Transform tablero = GameObject.Find("Board")?.transform;
        if (tablero == null || tablero.childCount == 0) return;

        indice = Mathf.Clamp(indice, 0, tablero.childCount - 1);
        currentIndex = indice;
        transform.position = tablero.GetChild(currentIndex).position + Vector3.up;
    }

    // --------------------------
    // Helpers
    // --------------------------
    IEnumerator JumpTo(Vector3 destino)
    {
        isMoving = true;

        Vector3 start = transform.position;
        Vector3 end = destino + Vector3.up;

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

    public Tile_U GetCurrentTile()
    {
        if (tiles == null || tiles.Length == 0) return null;
        var t = tiles[currentIndex];
        return t ? t.GetComponent<Tile_U>() : null;
    }

    public Tile_U.Categoria CategoriaActual()
    {
        var tile = GetCurrentTile();
        return tile != null ? tile.categoria : Tile_U.Categoria.Historia;
    }
}
