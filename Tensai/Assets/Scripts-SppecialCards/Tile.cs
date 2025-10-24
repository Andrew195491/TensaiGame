using UnityEngine; // Necesario para MonoBehaviour

/// <summary>
/// Representa una casilla individual en el tablero del juego.
/// 
/// PROPÓSITO:
/// Esta clase es la unidad básica del tablero. Cada GameObject hijo del Board
/// tiene este componente para definir su tipo y comportamiento.
/// 
/// DISEÑO SIMPLE:
/// - Solo almacena datos (categoría de la casilla)
/// - No tiene lógica de juego (eso está en CartaManager y MovePlayer)
/// - Patrón "Data Component" - solo contiene información
/// 
/// FUNCIONAMIENTO:
/// 1. Se asigna a cada casilla del tablero en el Inspector
/// 2. MovePlayer verifica la categoría cuando un jugador cae en ella
/// 3. CartaManager ejecuta la acción correspondiente según la categoría
/// </summary>
public class Tile : MonoBehaviour
{
    // ============================================
    // SECCIÓN 1: DEFINICIÓN DE CATEGORÍAS DE CASILLA
    // ============================================
    
    /// <summary>
    /// Enumeración que define todos los tipos posibles de casillas en el juego.
    /// 
    /// ORGANIZACIÓN:
    /// - Primeras 3: Casillas de trivia (requieren responder preguntas)
    /// - Últimas 3: Casillas especiales (efectos automáticos)
    /// 
    /// USO EN EL PROYECTO:
    /// - MovePlayer.JumpMultipleTimes() las distingue para saber si mostrar pregunta
    /// - CartaManager.MostrarCarta() selecciona preguntas según la categoría
    /// - CartaManager.EjecutarAccionEspecial() maneja casillas especiales
    /// </summary>
    public enum Categoria
    {
        // ========================================
        // CATEGORÍAS DE TRIVIA (Requieren pregunta)
        // ========================================
        
        /// <summary>
        /// Casilla de historia.
        /// Muestra preguntas sobre eventos históricos, personajes y fechas.
        /// Lista de preguntas en: CartaManager.historia
        /// </summary>
        Historia,
        
        /// <summary>
        /// Casilla de geografía.
        /// Muestra preguntas sobre países, capitales, ríos y continentes.
        /// Lista de preguntas en: CartaManager.geografia
        /// </summary>
        Geografia,
        
        /// <summary>
        /// Casilla de ciencia.
        /// Muestra preguntas sobre física, química, biología y tecnología.
        /// Lista de preguntas en: CartaManager.ciencia
        /// </summary>
        Ciencia,

        // ========================================
        // CASILLAS ESPECIALES (Sin pregunta)
        // ========================================
        
        /// <summary>
        /// Casilla neutral - "zona segura".
        /// No tiene efecto alguno, el jugador simplemente descansa.
        /// 
        /// COMPORTAMIENTO:
        /// - Muestra mensaje: "Casilla Neutral: ¡Descansas un momento!"
        /// - No hay pregunta
        /// - No hay efecto de juego
        /// - Útil como "respiro" en el tablero
        /// 
        /// Manejado en: CartaManager.ManejarCasillaNeutral()
        /// </summary>
        neutral,
        
        /// <summary>
        /// Casilla de beneficios - "casilla de suerte".
        /// Otorga una carta especial con efecto positivo al jugador.
        /// 
        /// COMPORTAMIENTO:
        /// - Da una carta aleatoria de CartaManager.benefits
        /// - El jugador puede guardarla o descartarla
        /// - Si el inventario está lleno, muestra panel de reemplazo
        /// 
        /// EJEMPLOS DE BENEFICIOS:
        /// - Avanzar casillas extra (Avanza1, Avanza2, Avanza3)
        /// - Repetir turno
        /// - Teletransporte adelante
        /// - Doble dado
        /// - Robar carta a otro jugador
        /// 
        /// Manejado en: CartaManager.ManejarCasillaBeneficios()
        /// </summary>
        Benefits,
        
        /// <summary>
        /// Casilla de penalización - "casilla trampa".
        /// Aplica inmediatamente un efecto negativo al jugador.
        /// 
        /// COMPORTAMIENTO:
        /// - Obtiene una carta aleatoria de CartaManager.penalty
        /// - El efecto se aplica INMEDIATAMENTE (no se guarda)
        /// - No se puede evitar ni guardar para después
        /// 
        /// EJEMPLOS DE PENALIZACIONES:
        /// - Retroceder casillas (Retrocede1, Retrocede2, Retrocede3)
        /// - Perder turno
        /// - Volver a la casilla de salida
        /// - Perder todas las cartas del inventario
        /// - Teletransporte hacia atrás
        /// 
        /// Manejado en: CartaManager.ManejarCasillaPenalidad()
        /// </summary>
        Penalty
    }
    
    // ============================================
    // SECCIÓN 2: PROPIEDAD DE LA CASILLA
    // ============================================
    
    /// <summary>
    /// La categoría específica de esta instancia de casilla.
    /// 
    /// CONFIGURACIÓN:
    /// - Se asigna manualmente en el Inspector de Unity para cada casilla
    /// - Cada casilla del tablero (hijo de "Board") tiene su propia categoría
    /// - Permite crear tableros variados y balanceados
    /// 
    /// ACCESO DESDE OTROS SCRIPTS:
    /// 
    /// Ejemplo en MovePlayer.cs:
    /// ```csharp
    /// Tile tile = tiles[currentIndex].GetComponent<Tile>();
    /// if (tile.categoria == Tile.Categoria.Historia) {
    ///     // Mostrar pregunta de historia
    /// }
    /// ```
    /// 
    /// IMPORTANTE:
    /// - Es pública para que el Inspector pueda mostrarla
    /// - Es [SerializeField] implícito (Unity serializa públicas automáticamente)
    /// - Debe configurarse ANTES de iniciar el juego
    /// </summary>
    [Header("Configuración de la Casilla")]
    [Tooltip("Define el tipo de casilla y su comportamiento en el juego")]
    public Categoria categoria;
}

// ============================================
// PATRONES DE DISEÑO UTILIZADOS
// ============================================
/*
 * 1. DATA COMPONENT PATTERN:
 *    - Este script solo almacena datos (categoria)
 *    - No tiene lógica de juego
 *    - Otros scripts leen estos datos para actuar
 *    - Ventaja: Separación de responsabilidades
 * 
 * 2. ENUM PARA TIPOS:
 *    - Usa enum en lugar de strings o ints
 *    - Ventajas:
 *      ✓ Type-safe (el compilador detecta errores)
 *      ✓ Autocompletado en IDE
 *      ✓ Dropdown en Inspector de Unity
 *      ✓ Refactorización fácil
 *    - Comparado con alternativas:
 *      ✗ String: Propenso a typos ("Hsitoria" vs "Historia")
 *      ✗ Int: No descriptivo (¿qué significa 3?)
 * 
 * 3. COMPONENT-BASED ARCHITECTURE:
 *    - Cada GameObject tiene componentes especializados
 *    - Tile define QUÉ es la casilla
 *    - Otros scripts definen QUÉ HACE con esa información
 *    - Ventaja: Modularidad y reutilización
 */

// ============================================
// FLUJO DE INTERACCIÓN CON OTROS SCRIPTS
// ============================================
/*
 * PASO 1: CONFIGURACIÓN (Inspector/Editor)
 * - Diseñador coloca casillas en el tablero
 * - Asigna categoría a cada una manualmente
 * - Resultado: Tablero con distribución de casillas
 * 
 * PASO 2: INICIALIZACIÓN (Runtime - Start)
 * - MovePlayer.CargarTilesDesdeTablero() carga todas las casillas
 * - Ordena el array de casillas numéricamente
 * - Cada casilla mantiene su componente Tile con su categoría
 * 
 * PASO 3: MOVIMIENTO (Durante juego)
 * - Jugador tira el dado
 * - MovePlayer.JumpMultipleTimes() mueve el jugador
 * - Al llegar a destino: GetComponent<Tile>() obtiene la categoría
 * 
 * PASO 4: ACCIÓN (Basada en categoría)
 * 
 * A) Si es Historia/Geografia/Ciencia:
 *    → CartaManager.MostrarCarta(tile.categoria)
 *    → Muestra pregunta de esa categoría
 *    → Si falla: retrocede
 *    → Si acierta: se queda
 * 
 * B) Si es neutral/Benefits/Penalty:
 *    → CartaManager.EjecutarAccionEspecial(tile.categoria, jugador)
 *    → Neutral: Mensaje informativo
 *    → Benefits: Ofrece carta para guardar
 *    → Penalty: Aplica efecto negativo inmediato
 */

// ============================================
// EJEMPLO DE CONFIGURACIÓN DE TABLERO
// ============================================
/*
 * TABLERO DE 20 CASILLAS (EJEMPLO):
 * 
 * Tile_0:  neutral     (Inicio del juego)
 * Tile_1:  Historia
 * Tile_2:  Geografia
 * Tile_3:  Benefits    (Primera casilla de suerte)
 * Tile_4:  Ciencia
 * Tile_5:  Historia
 * Tile_6:  Penalty     (Primera trampa)
 * Tile_7:  Geografia
 * Tile_8:  Historia
 * Tile_9:  neutral     (Zona de respiro)
 * Tile_10: Ciencia
 * Tile_11: Benefits
 * Tile_12: Historia
 * Tile_13: Geografia
 * Tile_14: Penalty
 * Tile_15: Ciencia
 * Tile_16: Historia
 * Tile_17: Benefits
 * Tile_18: Geografia
 * Tile_19: Historia    (Meta - última casilla)
 * 
 * DISTRIBUCIÓN TÍPICA:
 * - ~60% Preguntas de trivia (Historia/Geografia/Ciencia)
 * - ~20% Beneficios (para mantener interés)
 * - ~10% Penalizaciones (para añadir riesgo)
 * - ~10% Neutral (respiros estratégicos)
 */

// ============================================
// EXTENSIONES POSIBLES
// ============================================
/*
 * MEJORAS QUE PODRÍAN AGREGARSE:
 * 
 * 1. PROPIEDADES ADICIONALES:
 *    public Color colorCasilla;        // Color visual de la casilla
 *    public Sprite iconoCasilla;       // Ícono para la casilla
 *    public AudioClip sonidoCasilla;   // Sonido al caer en ella
 * 
 * 2. MÚLTIPLES NIVELES DE DIFICULTAD:
 *    public enum Dificultad { Fácil, Media, Difícil }
 *    public Dificultad dificultadPregunta;
 * 
 * 3. CASILLAS CONDICIONALES:
 *    public bool requiereItem;         // ¿Necesita un item específico?
 *    public string itemRequerido;      // Nombre del item
 * 
 * 4. EFECTOS VISUALES:
 *    public ParticleSystem efectoAlCaer; // Partículas al caer
 *    public Material materialEspecial;   // Material cuando está ocupada
 * 
 * 5. ESTADÍSTICAS:
 *    private int vecesVisitada = 0;    // Contador de visitas
 *    public void OnJugadorCae() { vecesVisitada++; }
 * 
 * NOTA: Mantenerlo simple es mejor para la mayoría de proyectos.
 * Solo agregar complejidad cuando sea necesario.
 */