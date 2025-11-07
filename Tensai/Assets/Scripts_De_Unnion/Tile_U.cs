using UnityEngine;

/// <summary>
/// Representa una casilla individual en el tablero del juego.
/// 
/// 🔹 Combina características de Tile y Tile2:
/// - Soporta tipos de casilla (Pregunta, Neutral, Beneficio, Penalidad)
/// - Integra categorías de trivia (Historia, Geografía, Ciencia)
/// - Mantiene el diseño de componente de datos (Data Component)
/// 
/// USO:
/// - Se asigna en el Inspector a cada casilla del tablero.
/// - MovePlayer detecta el tipo/categoría al caer en ella.
/// - CartaManager maneja el comportamiento según el tipo.
/// </summary>
public class Tile_U : MonoBehaviour
{
    // ============================================
    // SECCIÓN 1: ENUMERACIONES DE TIPO Y CATEGORÍA
    // ============================================

    /// <summary>
    /// Tipo general de casilla.
    /// Define el comportamiento base de la casilla.
    /// </summary>
    public enum TipoCasilla
    {
        /// <summary>
        /// Casilla de pregunta: requiere responder una trivia.
        /// </summary>
        Pregunta,

        /// <summary>
        /// Casilla neutral: sin efecto, solo descanso.
        /// </summary>
        Neutral,

        /// <summary>
        /// Casilla de beneficio: aplica un efecto positivo.
        /// </summary>
        Beneficio,

        /// <summary>
        /// Casilla de penalidad: aplica un efecto negativo inmediato.
        /// </summary>
        Penalidad
    }

    /// <summary>
    /// Categorías temáticas de las preguntas.
    /// Usadas solo si el tipo es <see cref="TipoCasilla.Pregunta"/>.
    /// </summary>
    public enum Categoria
    {
        /// <summary>
        /// Preguntas de historia: personajes, eventos y fechas.
        /// </summary>
        Historia,

        /// <summary>
        /// Preguntas de geografía: países, capitales, mapas.
        /// </summary>
        Geografia,

        /// <summary>
        /// Preguntas de ciencia: física, biología, tecnología.
        /// </summary>
        Ciencia
    }

    // ============================================
    // SECCIÓN 2: CONFIGURACIÓN DE LA CASILLA
    // ============================================

    [Header("Tipo de casilla")]
    [Tooltip("Define el tipo de casilla en el tablero.")]
    public TipoCasilla tipo = TipoCasilla.Pregunta;

    // Fase Final: La categoría solo se muestra en el inspector si el tipo es Pregunta
    [Header("Solo si es Pregunta")]
    [Tooltip("Categoría de pregunta asociada (solo válida si tipo = Pregunta).")]
    [SerializeField]
    private Categoria _categoria = Categoria.Historia;

    // ============================================
    // SECCIÓN 3: PROPIEDADES CON VALIDACIÓN
    // ============================================

    /// <summary>
    /// Propiedad pública para acceder a la categoría con validación.
    /// Fase Final: Solo retorna categoría si el tipo es Pregunta, sino retorna Historia por defecto.
    /// </summary>
    public Categoria categoria
    {
        get
        {
            if (tipo == TipoCasilla.Pregunta)
                return _categoria;
            else
                return Categoria.Historia; // Valor por defecto cuando no es pregunta
        }
        set
        {
            // Fase Final: Solo permite cambiar la categoría si el tipo es Pregunta
            if (tipo == TipoCasilla.Pregunta)
                _categoria = value;
        }
    }

    // ============================================
    // SECCIÓN 4: UTILIDAD / FUNCIONAMIENTO
    // ============================================

    /// <summary>
    /// Determina si esta casilla requiere mostrar una pregunta.
    /// </summary>
    public bool EsCasillaDePregunta => tipo == TipoCasilla.Pregunta;

    /// <summary>
    /// Devuelve un texto descriptivo para depuración o UI.
    /// Fase Final: Solo incluye la categoría si el tipo es Pregunta.
    /// </summary>
    public string ObtenerDescripcion()
    {
        if (tipo == TipoCasilla.Pregunta)
            return $"Pregunta de {_categoria}";
        
        return tipo.ToString();
    }

    // ============================================
    // SECCIÓN 5: MÉTODO PARA EDITOR (OPCIONAL)
    // ============================================

    /// <summary>
    /// Método usado por custom editors para determinar si mostrar la categoría.
    /// Fase Final: Ayuda a los editores personalizados a mostrar/ocultar la categoría.
    /// </summary>
    public bool DebeMostrarCategoria()
    {
        return tipo == TipoCasilla.Pregunta;
    }
}

// ============================================
// COMENTARIOS DE DISEÑO
// ============================================
/*
 * 1. DATA COMPONENT PATTERN:
 *    - Este script no ejecuta lógica de juego.
 *    - Solo almacena información estructural de cada casilla.
 *    - MovePlayer y CartaManager utilizan estos datos.
 *
 * 2. ENUMS COMBINADOS:
 *    - TipoCasilla: separa tipos funcionales (Neutral, Beneficio, Penalidad).
 *    - Categoria: detalla la temática de las preguntas.
 *
 * 3. FLUJO DE USO:
 *    A) MovePlayer detecta la casilla actual:
 *       Tile_U tile = tiles[index].GetComponent<Tile_U>();
 *    B) Si tile.tipo == Pregunta → CartaManager.MostrarCarta(tile.categoria);
 *    C) Si tile.tipo == Beneficio o Penalidad → ejecuta acción especial.
 *
 * 4. VENTAJAS:
 *    - Inspector amigable (dropdowns de enum)
 *    - Estructura extensible y clara
 *    - Compatibilidad con el sistema de casillas existente
 *
 * 5. POSIBLES EXTENSIONES:
 *    - public Color colorCasilla;
 *    - public AudioClip sonidoAlCaer;
 *    - public Sprite icono;
 *    - public bool requiereItem;
 */