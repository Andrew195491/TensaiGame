using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;

public class MovePlayer : MonoBehaviour
{
    [Header("Movimiento")]
    public float jumpHeight = 1.5f;
    public float moveDuration = 0.5f;

    [Header("Estado")]
    public int currentIndex = 0;

    private Transform[] tiles;
    private bool isMoving = false;

    void Start()
    {
        CargarTilesDesdeTablero();
        if (tiles.Length > 0)
            transform.position = tiles[currentIndex].position + Vector3.up * 1f;
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
    }

    int ExtraerNumero(string nombre)
    {
        string t = Regex.Match(nombre, @"\d+").Value;
        return int.TryParse(t, out int n) ? n : 0;
    }

    public IEnumerator MoverAdelante(int pasos)
    {
        if (isMoving || tiles.Length == 0 || pasos <= 0) yield break;

        for (int i = 0; i < pasos; i++)
        {
            currentIndex = (currentIndex + 1) % tiles.Length;
            yield return JumpTo(tiles[currentIndex].position);
        }
    }

    public IEnumerator MoverAtras(int pasos)
    {
        if (isMoving || tiles.Length == 0 || pasos <= 0) yield break;

        for (int i = 0; i < pasos; i++)
        {
            currentIndex = (currentIndex - 1 + tiles.Length) % tiles.Length;
            yield return JumpTo(tiles[currentIndex].position);
        }
    }

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

    public Tile.Categoria CategoriaActual()
    {
        Tile tile = GetCurrentTile();
        return tile != null ? tile.categoria : Tile.Categoria.Historia;
    }

    public Tile GetCurrentTile()
    {
        if (tiles == null || tiles.Length == 0) return null;
        var t = tiles[currentIndex];
        return t != null ? t.GetComponent<Tile>() : null;
    }

    // Útil para efectos tipo “teleport”
    public void TeleportAIndiceSeguro(int indice)
    {
        Transform tablero = GameObject.Find("Board")?.transform;
        if (tablero == null || tablero.childCount == 0) return;

        indice = Mathf.Clamp(indice, 0, tablero.childCount - 1);
        currentIndex = indice;
        transform.position = tablero.GetChild(currentIndex).position + Vector3.up * 1f;
    }
}
