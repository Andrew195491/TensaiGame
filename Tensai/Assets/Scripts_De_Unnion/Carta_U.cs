using UnityEngine; // Necesario para MonoBehaviour y Range

// ============================================
// CLASE UNIFICADA: Carta_U
// ============================================

/// <summary>
/// Clase que representa una carta de trivia en el juego.
/// Cada carta contiene una pregunta con tres posibles respuestas,
/// una respuesta correcta y una posible acción o efecto adicional.
/// </summary>
[System.Serializable]
public class Carta_U
{
    // ============================================
    // SECCIÓN 1: DATOS DE LA PREGUNTA
    // ============================================

    /// <summary>
    /// Texto de la pregunta que se mostrará al jugador.
    /// Ejemplo: "¿Cuál es la capital de Francia?"
    /// </summary>
    public string pregunta;

    /// <summary>
    /// Primera opción de respuesta.
    /// Ejemplo: "Madrid"
    /// </summary>
    public string respuesta1;

    /// <summary>
    /// Segunda opción de respuesta.
    /// Ejemplo: "París"
    /// </summary>
    public string respuesta2;

    /// <summary>
    /// Tercera opción de respuesta.
    /// Ejemplo: "Roma"
    /// </summary>
    public string respuesta3;

    // ============================================
    // SECCIÓN 2: VALIDACIÓN DE RESPUESTA
    // ============================================

    /// <summary>
    /// Indica cuál de las tres respuestas es la correcta.
    /// Valores permitidos: 1, 2 o 3.
    /// 
    /// [Range(1, 3)] limita el valor en el Inspector de Unity
    /// y previene errores al configurar cartas.
    /// </summary>
    [Range(1, 3)]
    public int respuestaCorrecta;

    // ============================================
    // SECCIÓN 3: EFECTO / ACCIÓN DE LA CARTA
    // ============================================

    /// <summary>
    /// Define qué acción o efecto se aplica al responder esta carta.
    /// Ejemplos:
    /// - Movimiento: "Avanza1", "Retrocede2"
    /// - Turnos: "RepiteTurno", "PierdeTurno"
    /// - Especiales: "Inmunidad", "TeletransporteAdelante"
    /// </summary>
    public string accion;
}

// ============================================
// NOTAS DE DISEÑO
// ============================================
/*
 * 🔹 Esta clase combina las ventajas de "Carta" y "Carta2":
 *    - Simplicidad del modelo de datos (de Carta2)
 *    - Comentarios detallados y estructura profesional (de Carta)
 * 
 * 🔹 Beneficios:
 *    1. Editable desde el Inspector de Unity
 *    2. Compatible con serialización JSON o ScriptableObjects
 *    3. Permite definir preguntas de trivia con acciones personalizadas
 * 
 * 🔹 Uso típico:
 *    - En sistemas de cartas de trivia
 *    - En gestores de cartas (CartaManager)
 *    - En configuraciones de juego tipo “pregunta y efecto”
 */
