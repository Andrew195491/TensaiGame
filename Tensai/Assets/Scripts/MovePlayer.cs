using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;

public class MovePlayer : MonoBehaviour
{
    [Header("Movimiento")]
    public float jumpHeight = 1.5f;
    public float moveDuration = 0.5f;

    [Header("Estado")]
    public int currentIndex = 0;             // casilla actual
    public int ultimaCantidadMovida = 0;     // pasos del último avance

    private Transform[] tiles;
    private bool isMoving = false;

    void Start()
    {
        CargarTilesDesdeTablero();

        if (tiles.Length > 0)
        {
            currentIndex = 0;
            transform.position = tiles[currentIndex].position + Vector3.up * 1f;
        }
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
            int numA = NumeroEnNombre(a.name);
            int numB = NumeroEnNombre(b.name);
            return numA.CompareTo(numB);
        });
    }

    int NumeroEnNombre(string nombre)
    {
        var m = Regex.Match(nombre, @"\d+").Value;
        return int.TryParse(m, out int n) ? n : 0;
    }

    public IEnumerator MoverAdelante(int pasos)
    {
        if (isMoving || tiles.Length == 0) yield break;

        ultimaCantidadMovida = pasos;

        for (int i = 0; i < pasos; i++)
        {
            currentIndex = (currentIndex + 1) % tiles.Length;
            yield return JumpTo(tiles[currentIndex].position);
        }
    }

    public IEnumerator MoverAtras(int pasos)
    {
        if (isMoving || tiles.Length == 0) yield break;

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
        if (tiles == null || tiles.Length == 0) return Tile.Categoria.Historia;
        Tile tile = tiles[currentIndex].GetComponent<Tile>();
        return tile != null ? tile.categoria : Tile.Categoria.Historia;
    }
}
