using UnityEngine; // Necesario para MonoBehaviour

// ============================================
// ATRIBUTO DE SERIALIZACIÓN
// ============================================

/// <summary>
/// [System.Serializable] permite que esta clase sea visible y editable
/// en el Inspector de Unity, facilitando la creación de cartas sin código.
/// También permite guardar/cargar cartas con sistemas de persistencia.
/// </summary>
[System.Serializable]

/// <summary>
/// Clase que representa una carta de trivia en el juego.
/// Cada carta contiene una pregunta con 3 respuestas posibles y una acción/efecto.
/// </summary>
public class Carta
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
    /// Valores permitidos: 1, 2 o 3
    /// 
    /// [Range(1, 3)] es un atributo que:
    /// - Limita los valores entre 1 y 3 en el Inspector de Unity
    /// - Muestra un slider visual en lugar de un campo numérico
    /// - Previene errores de configuración (no permite 0, 4, etc.)
    /// 
    /// Ejemplo: Si respuestaCorrecta = 2, entonces respuesta2 es la correcta
    /// </summary>
    [Range(1, 3)] 
    public int respuestaCorrecta;

    // ============================================
    // SECCIÓN 3: EFECTO/BONUS DE LA CARTA
    // ============================================
    
    /// <summary>
    /// Define qué efecto o acción se aplica al responder esta carta.
    /// 
    /// Ejemplos de acciones posibles:
    /// - Movimiento: "Avanza1", "Avanza2", "Avanza3", "Retrocede1", "Retrocede2", "Retrocede3"
    /// - Turnos: "RepiteTurno", "PierdeTurno"
    /// - Especiales: "Intercambia", "Inmunidad", "DobleDado", "TeletransporteAdelante"
    /// - Otros: "ElegirDado", "RobarCarta", "IrSalida"
    /// 
    /// Este string se usa en CartaManager u otros scripts para determinar
    /// qué efecto aplicar cuando se responde correctamente la carta.
    /// </summary>
    public string accion; // 🔹 Tipo de acción
}

// ============================================
// NOTAS DE DISEÑO
// ============================================
/*
 * VENTAJAS DE ESTA ESTRUCTURA:
 * 
 * 1. Simplicidad: Es una clase de datos pura (POCO - Plain Old C# Object)
 *    sin lógica compleja, fácil de entender y mantener.
 * 
 * 2. Flexibilidad: Permite crear cartas con diferentes efectos sin modificar
 *    el código, solo cambiando el valor del string "accion".
 * 
 * 3. Serialización: Al ser [System.Serializable], se puede:
 *    - Editar en el Inspector de Unity
 *    - Guardar en JSON/XML para persistencia
 *    - Usar en ScriptableObjects
 *    - Enviar por red en juegos multijugador
 * 
 * 4. Validación: El atributo [Range(1,3)] previene errores de configuración
 *    asegurando que siempre haya una respuesta correcta válida.
 * 
 * USO TÍPICO:
 * Esta clase se utiliza generalmente en:
 * - Listas de cartas en CartaManager
 * - Arrays de cartas en el Inspector
 * - Sistemas de inventario de cartas
 * - Archivos JSON de configuración de preguntas
 */