using UnityEngine; // Necesario para MonoBehaviour
using System.Collections; // Necesario para IEnumerator y Corrutinas
using System.Text.RegularExpressions;  // Necesario para expresiones regulares (exprisones regulares son patrones para buscar texto)

/// <summary>
/// Controla el movimiento del jugador en el tablero de juego.
/// Gestiona saltos entre casillas, animaciones, interacciones con casillas especiales
/// y movimientos hacia adelante/atrás según las mecánicas del juego.
/// </summary>
public class MovePlayer : MonoBehaviour
{
    // ============================================
    // SECCIÓN 1: REFERENCIAS Y VARIABLES DE ESTADO
    // ============================================
    
    /// <summary>
    /// Referencia al controlador del dado para bloquearlo durante movimientos.
    /// </summary>
    public DiceController dado;
    
    /// <summary>
    /// Array que almacena todas las casillas del tablero en orden.
    /// Se carga dinámicamente al inicio desde el objeto "Board".
    /// </summary>
    private Transform[] tiles;
    
    /// <summary>
    /// Índice de la casilla actual donde se encuentra el jugador.
    /// Comienza en 0 (casilla de inicio).
    /// </summary>
    private int currentIndex = 0;
    
    /// <summary>
    /// Indica si el jugador está actualmente en movimiento.
    /// Previene movimientos múltiples simultáneos.
    /// </summary>
    private bool isMoving = false;

    // ============================================
    // SECCIÓN 2: CONFIGURACIÓN DE ANIMACIÓN
    // ============================================
    
    /// <summary>
    /// Altura del salto durante la animación de movimiento.
    /// Valores mayores = saltos más altos y visibles.
    /// </summary>
    public float jumpHeight = 1.5f;
    
    /// <summary>
    /// Duración de cada salto individual entre casillas en segundos.
    /// Controla la velocidad del movimiento.
    /// </summary>
    public float moveDuration = 0.5f;

    /// <summary>
    /// Almacena cuántas casillas se movió el jugador en su último movimiento.
    /// Se usa para retroceder esa cantidad si responde incorrectamente una pregunta.
    /// </summary>
    private int ultimaCantidadMovida = 0;

    // ============================================
    // SECCIÓN 3: INICIALIZACIÓN
    // ============================================
    
    void Start()
    {
        // Cargar todas las casillas del tablero y ordenarlas
        CargarTilesDesdeTablero();

        // Posicionar al jugador en la primera casilla (inicio del juego)
        if (tiles.Length > 0)
            // Vector3.up * 1f levanta al jugador 1 unidad sobre la casilla
            transform.position = tiles[0].position + Vector3.up * 1f;
    }

    // ============================================
    // SECCIÓN 4: CARGA Y ORDENAMIENTO DE CASILLAS
    // ============================================
    
    /// <summary>
    /// Carga todas las casillas hijas del objeto "Board" y las ordena numéricamente.
    /// Esto asegura que el jugador se mueva en la secuencia correcta del tablero.
    /// </summary>
    void CargarTilesDesdeTablero()
    {
        // Buscar el objeto "Board" en la escena
        Transform tablero = GameObject.Find("Board")?.transform;

        // Validar que el tablero existe
        if (tablero == null)
        {
            Debug.LogError("No se encontró el objeto 'Board'");
            tiles = new Transform[0];
            return;
        }

        // Crear array con el tamaño exacto del número de casillas
        tiles = new Transform[tablero.childCount];
        
        // Copiar todas las casillas hijas al array
        for (int i = 0; i < tablero.childCount; i++)
            tiles[i] = tablero.GetChild(i);

        // Ordenar las casillas según el número en su nombre
        // Ejemplo: "Tile_1", "Tile_2", "Tile_10" se ordenan como 1, 2, 10
        System.Array.Sort(tiles, (a, b) =>
        {
            // Extraer el número del nombre de cada casilla
            int numA = ObtenerNumeroDesdeNombre(a.name);
            int numB = ObtenerNumeroDesdeNombre(b.name);
            // Comparar numéricamente (no alfabéticamente)
            return numA.CompareTo(numB);
        });
    }

    /// <summary>
    /// Extrae el número de una cadena de texto usando expresiones regulares.
    /// Ejemplo: "Tile_5" → 5, "Casilla_10" → 10
    /// </summary>
    /// <param name="nombre">Nombre del GameObject de la casilla</param>
    /// <returns>Número extraído o 0 si no se encuentra ninguno</returns>
    int ObtenerNumeroDesdeNombre(string nombre)
    {
        // Regex.Match busca el primer número en el string
        // @"\d+" significa "uno o más dígitos"
        string numeroTexto = Regex.Match(nombre, @"\d+").Value;
        
        // Intentar convertir el texto a número entero
        return int.TryParse(numeroTexto, out int resultado) ? resultado : 0;
    }

    // ============================================
    // SECCIÓN 5: MOVIMIENTO HACIA ADELANTE
    // ============================================
    
    /// <summary>
    /// Mueve al jugador hacia adelante una cantidad específica de casillas.
    /// Este es el método principal llamado cuando se tira el dado o se usa una carta de avance.
    /// 
    /// FLUJO:
    /// 1. Guarda la cantidad de pasos (para posible retroceso)
    /// 2. Salta de casilla en casilla con animación
    /// 3. Ejecuta la acción de la casilla final (carta, beneficio, penalidad)
    /// </summary>
    /// <param name="cantidad">Número de casillas a avanzar</param>
    public IEnumerator JumpMultipleTimes(int cantidad)
    {
        // Evitar movimiento doble si ya está en movimiento
        if (isMoving) yield break;

        // Guardar cantidad para posible retroceso si falla la pregunta
        ultimaCantidadMovida = cantidad;

        // ====== FASE 1: MOVIMIENTO ANIMADO ======
        
        // Bucle que mueve al jugador casilla por casilla
        for (int i = 0; i < cantidad; i++)
        {
            // Calcular siguiente casilla (con wrap-around circular)
            // El % tiles.Length hace que vuelva al inicio si llega al final
            currentIndex = (currentIndex + 1) % tiles.Length;
            
            // Animar el salto a la siguiente casilla
            yield return JumpToTile(tiles[currentIndex].position);
        }

        // ====== FASE 2: PAUSA ANTES DE LA ACCIÓN ======
        
        // Pequeña pausa para que el jugador vea dónde cayó
        yield return new WaitForSeconds(0.5f);

        // ====== FASE 3: EJECUTAR ACCIÓN DE LA CASILLA ======
        
        // Obtener el componente Tile de la casilla actual
        Tile tile = tiles[currentIndex].GetComponent<Tile>();
        
        if (tile != null)
        {
            // Verificar el tipo de casilla
            if (tile.categoria == Tile.Categoria.neutral ||
                tile.categoria == Tile.Categoria.Benefits ||
                tile.categoria == Tile.Categoria.Penalty)
            {
                // Casillas especiales: neutral, beneficio o penalidad
                // No requieren responder preguntas
                CartaManager.instancia.EjecutarAccionEspecial(tile.categoria, this);
            }
            else
            {
                // Casillas de trivia: Historia, Geografía, Ciencia
                // Mostrar carta con pregunta
                // Si responde mal, retrocede los pasos que avanzó
                CartaManager.instancia.MostrarCarta(tile.categoria, () =>
                {
                    // Callback ejecutado si la respuesta es incorrecta
                    StartCoroutine(Retroceder(ultimaCantidadMovida));
                });
            }
        }
    }

    // ============================================
    // SECCIÓN 6: RESPONDER CARTA (MÉTODO LEGACY)
    // ============================================
    
    /// <summary>
    /// Método para responder una carta de trivia.
    /// NOTA: Este método parece no usarse actualmente.
    /// La lógica de respuesta incorrecta se maneja en JumpMultipleTimes.
    /// Se mantiene por compatibilidad con código antiguo.
    /// </summary>
    /// <param name="correcta">Si la respuesta fue correcta o no</param>
    public void ResponderCarta(bool correcta)
    {
        if (!correcta)
        {
            StartCoroutine(Retroceder(ultimaCantidadMovida));
        }
    }

    // ============================================
    // SECCIÓN 7: MOVIMIENTO HACIA ATRÁS
    // ============================================
    
    /// <summary>
    /// Retrocede al jugador una cantidad específica de casillas.
    /// Usado para:
    /// - Respuestas incorrectas (retrocede lo que avanzó)
    /// - Cartas de penalidad (Retrocede1, Retrocede2, Retrocede3)
    /// - Efectos especiales negativos
    /// </summary>
    /// <param name="pasos">Número de casillas a retroceder</param>
    public IEnumerator Retroceder(int pasos)
    {
        // Evitar retroceso múltiple simultáneo
        if (isMoving) yield break;

        // Bloquear el dado durante el retroceso
        if (dado != null)
            dado.BloquearDado(true);

        // Retroceder casilla por casilla con animación
        for (int i = 0; i < pasos; i++)
        {
            // Calcular casilla anterior (con wrap-around)
            // + tiles.Length asegura que no haya números negativos antes del módulo
            currentIndex = (currentIndex - 1 + tiles.Length) % tiles.Length;
            
            // Animar el salto a la casilla anterior
            yield return JumpToTile(tiles[currentIndex].position);
        }

        // Desbloquear el dado después de retroceder
        if (dado != null)
            dado.BloquearDado(false);
    }

    // ============================================
    // SECCIÓN 8: ANIMACIÓN DE SALTO
    // ============================================
    
    /// <summary>
    /// Anima el salto del jugador desde su posición actual hasta una casilla destino.
    /// Crea una trayectoria parabólica (arco) para simular un salto realista.
    /// 
    /// La animación combina:
    /// - Movimiento horizontal: Lerp lineal de inicio a destino
    /// - Movimiento vertical: Función sinusoidal para crear el arco
    /// </summary>
    /// <param name="destino">Posición 3D de la casilla destino</param>
    IEnumerator JumpToTile(Vector3 destino)
    {
        // Marcar que el movimiento ha comenzado
        isMoving = true;

        // Guardar posiciones de inicio y fin
        Vector3 start = transform.position;
        Vector3 end = destino + Vector3.up * 1f; // Levanta 1 unidad sobre la casilla

        // Variable para rastrear el tiempo transcurrido
        float tiempo = 0f;
        
        // Bucle de animación frame por frame
        while (tiempo < moveDuration)
        {
            // Incrementar tiempo con el delta time del frame
            tiempo += Time.deltaTime;
            
            // Calcular progreso normalizado (0 a 1)
            float t = Mathf.Clamp01(tiempo / moveDuration);

            // ====== MOVIMIENTO HORIZONTAL ======
            // Interpolación lineal de la posición horizontal
            Vector3 horizontal = Vector3.Lerp(start, end, t);
            
            // ====== MOVIMIENTO VERTICAL (ARCO) ======
            // Sin(t * π) crea un arco perfecto de 0 → 1 → 0
            // En t=0: Sin(0) = 0 (inicio)
            // En t=0.5: Sin(π/2) = 1 (pico del salto)
            // En t=1: Sin(π) = 0 (fin)
            float vertical = Mathf.Sin(t * Mathf.PI) * jumpHeight;

            // Combinar movimiento horizontal + arco vertical
            transform.position = horizontal + Vector3.up * vertical;

            // Esperar hasta el siguiente frame
            yield return null;
        }

        // Asegurar posición final exacta (evitar imprecisiones de punto flotante)
        transform.position = end;
        
        // Marcar que el movimiento ha terminado
        isMoving = false;
    }

    // ============================================
    // SECCIÓN 9: TELETRANSPORTE A CASILLA ESPECÍFICA
    // ============================================
    
    /// <summary>
    /// Teletransporta al jugador directamente a una casilla específica sin pasar por las intermedias.
    /// Usado para efectos especiales como:
    /// - "IrSalida" (volver a la casilla 0)
    /// - Teletransportes de cartas especiales
    /// - Efectos de penalidad que envían al inicio
    /// </summary>
    /// <param name="indiceCasilla">Índice de la casilla destino (0 = inicio)</param>
    public IEnumerator IrACasilla(int indiceCasilla)
    {
        // Evitar teletransporte múltiple simultáneo
        if (isMoving) yield break;
        
        // Bloquear el dado durante el teletransporte
        if (dado != null)
            dado.BloquearDado(true);

        // ====== CALCULAR DESTINO SEGURO ======
        
        // Asegurar que el índice esté dentro del rango válido
        // Mathf.Clamp limita el valor entre 0 y tiles.Length-1
        int destino = Mathf.Clamp(indiceCasilla, 0, tiles.Length - 1);
        
        // ====== TELETRANSPORTE INSTANTÁNEO ======
        
        // Actualizar el índice actual sin pasar por casillas intermedias
        currentIndex = destino;
        
        // ====== ANIMACIÓN VISUAL ======
        
        // Animar el salto a la nueva posición (para que no sea un cambio brusco)
        yield return JumpToTile(tiles[currentIndex].position);
        
        Debug.Log($"🏠 Jugador movido a casilla {destino}");
        
        // ====== FINALIZACIÓN ======
        
        // Desbloquear el dado después del teletransporte
        if (dado != null)
            dado.BloquearDado(false);
    }
}

// ============================================
// NOTAS DE DISEÑO Y MECÁNICAS
// ============================================
/*
 * SISTEMA DE MOVIMIENTO CIRCULAR:
 * 
 * El uso de módulo (%) permite que el tablero sea circular:
 * - Si hay 20 casillas y el jugador está en la 19
 * - Al avanzar 2, irá a la casilla (19 + 2) % 20 = 1
 * - Esto permite tableros infinitos sin caerse del borde
 * 
 * MECÁNICA DE PENALIZACIÓN POR ERROR:
 * 
 * 1. Jugador tira dado y saca 4
 * 2. Avanza 4 casillas (guardando ultimaCantidadMovida = 4)
 * 3. Cae en casilla de trivia
 * 4. Si responde MAL → retrocede 4 casillas (vuelve al inicio)
 * 5. Si responde BIEN → se queda en la nueva casilla
 * 
 * TIPOS DE MOVIMIENTO:
 * 
 * 1. JumpMultipleTimes(n): Movimiento normal (dado o cartas de avance)
 *    - Pasa por todas las casillas intermedias
 *    - Activa la acción de la casilla final
 * 
 * 2. Retroceder(n): Movimiento hacia atrás
 *    - Pasa por todas las casillas intermedias
 *    - Bloquea el dado durante el proceso
 * 
 * 3. IrACasilla(índice): Teletransporte
 *    - No pasa por casillas intermedias
 *    - Va directo a la casilla especificada
 * 
 * ANIMACIÓN DE SALTO:
 * 
 * La función Sin(t * π) crea una parábola perfecta:
 *     ^
 *   h |     *
 *   e |   *   *
 *   i |  *     *
 *   g | *       *
 *   h |*         *
 *     +----------->
 *       tiempo
 * 
 * Esto da un efecto visual natural de "salto" de casilla en casilla.
 */