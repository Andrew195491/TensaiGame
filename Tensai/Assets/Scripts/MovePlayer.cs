using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;

public class MovePlayer : MonoBehaviour
{
    public DiceController dado;
    private Transform[] tiles;
    private int currentIndex = 0;
    private bool isMoving = false;

    public float jumpHeight = 1.5f;
    public float moveDuration = 0.5f;

    private int ultimaCantidadMovida = 0;

    void Start()
    {
        CargarTilesDesdeTablero();

        if (tiles.Length > 0)
            transform.position = tiles[0].position + Vector3.up * 1f;
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
            int numA = ObtenerNumeroDesdeNombre(a.name);
            int numB = ObtenerNumeroDesdeNombre(b.name);
            return numA.CompareTo(numB);
        });
    }

    int ObtenerNumeroDesdeNombre(string nombre)
    {
        string numeroTexto = Regex.Match(nombre, @"\d+").Value;
        return int.TryParse(numeroTexto, out int resultado) ? resultado : 0;
    }

    public IEnumerator JumpMultipleTimes(int cantidad)
    {
        if (isMoving) yield break;

        ultimaCantidadMovida = cantidad;

        for (int i = 0; i < cantidad; i++)
        {
            currentIndex = (currentIndex + 1) % tiles.Length;
            yield return JumpToTile(tiles[currentIndex].position);
        }

        yield return new WaitForSeconds(0.5f);

        Tile tile = tiles[currentIndex].GetComponent<Tile>();
        if (tile != null)
        {
            CartaManager.instancia.MostrarCarta(tile.categoria, () =>
            {
                StartCoroutine(Retroceder(ultimaCantidadMovida));
            });


        }
    }

    public void ResponderCarta(bool correcta)
    {
        if (!correcta)
        {
            StartCoroutine(Retroceder(ultimaCantidadMovida));
        }
    }

    IEnumerator Retroceder(int pasos)
    {
        if (isMoving) yield break;

        if (dado != null)
            dado.BloquearDado(true); // 🔒 Bloqueamos el dado mientras retrocede

        for (int i = 0; i < pasos; i++)
        {
            currentIndex = (currentIndex - 1 + tiles.Length) % tiles.Length;
            yield return JumpToTile(tiles[currentIndex].position);
        }

        if (dado != null)
            dado.BloquearDado(false); // 🔓 Lo desbloqueamos al terminar
    }


    IEnumerator JumpToTile(Vector3 destino)
    {
        isMoving = true;

        Vector3 start = transform.position;
        Vector3 end = destino + Vector3.up * 1f;

        float tiempo = 0f;
        while (tiempo < moveDuration)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / moveDuration);

            Vector3 horizontal = Vector3.Lerp(start, end, t);
            float vertical = Mathf.Sin(t * Mathf.PI) * jumpHeight;

            transform.position = horizontal + Vector3.up * vertical;

            yield return null;
        }

        transform.position = end;
        isMoving = false;
    }
}
