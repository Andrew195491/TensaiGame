using System.Collections.Generic; // Necesario para List<T>

// ============================================
// SECCIÓN 1: CLASE RAÍZ DEL JSON
// ============================================

/// <summary>
/// Clase wrapper (envoltorio) que representa la estructura completa del archivo JSON.
/// Actúa como contenedor principal que encapsula todo el contenido del JSON.
/// 
/// Estructura JSON esperada:
/// {
///   "Cards": [
///     { "benefits": [...], "penalty": [...] },
///     { "benefits": [...], "penalty": [...] },
///     ...
///   ]
/// }
/// 
/// [System.Serializable] permite que Unity o JsonUtility pueda:
/// - Deserializar (convertir) el JSON en objetos C#
/// - Serializar (convertir) objetos C# de vuelta a JSON
/// </summary>
[System.Serializable]
public class CartasEspecialesRoot
{
    /// <summary>
    /// Lista que contiene todos los elementos "Cards" del JSON.
    /// Cada elemento es un CartaData que agrupa cartas de beneficios y penalizaciones.
    /// 
    /// El nombre "Cards" debe coincidir EXACTAMENTE con la clave en el JSON.
    /// Si el JSON dice "Cards", aquí debe ser "Cards" (case-sensitive).
    /// </summary>
    public List<CartaData> Cards;
}

// ============================================
// SECCIÓN 2: CLASE DE DATOS DE CARTA
// ============================================

/// <summary>
/// Representa cada elemento individual dentro del array "Cards" del JSON.
/// Agrupa las cartas en dos categorías: beneficios (positivas) y penalizaciones (negativas).
/// 
/// Estructura de un elemento CartaData en JSON:
/// {
///   "benefits": [
///     { "pregunta": "...", "respuesta1": "...", "accion": "Avanza2" },
///     { "pregunta": "...", "respuesta1": "...", "accion": "RepiteTurno" }
///   ],
///   "penalty": [
///     { "pregunta": "...", "respuesta1": "...", "accion": "Retrocede1" },
///     { "pregunta": "...", "respuesta1": "...", "accion": "PierdeTurno" }
///   ]
/// }
/// 
/// Esta estructura permite organizar las cartas por tipo de efecto,
/// facilitando su clasificación y uso en el juego.
/// </summary>
[System.Serializable]
public class CartaData
{
    /// <summary>
    /// Lista de cartas con efectos beneficiosos/positivos para el jugador.
    /// 
    /// Ejemplos de beneficios:
    /// - Avanzar casillas (Avanza1, Avanza2, Avanza3)
    /// - Repetir turno
    /// - Teletransporte adelante
    /// - Doble dado
    /// - Robar carta a otro jugador
    /// - Inmunidad
    /// 
    /// Estas cartas se otorgan típicamente cuando el jugador responde correctamente
    /// o cae en casillas especiales de "bonus".
    /// </summary>
    public List<Carta> benefits;
    
    /// <summary>
    /// Lista de cartas con efectos negativos/penalizaciones para el jugador.
    /// 
    /// Ejemplos de penalizaciones:
    /// - Retroceder casillas (Retrocede1, Retrocede2, Retrocede3)
    /// - Perder turno
    /// - Volver a la salida
    /// - Intercambiar posición con otro jugador (puede ser malo)
    /// 
    /// Estas cartas se otorgan típicamente cuando el jugador responde incorrectamente
    /// o cae en casillas especiales de "trampa/castigo".
    /// </summary>
    public List<Carta> penalty;
}

// ============================================
// EJEMPLO DE USO PRÁCTICO
// ============================================
/*
 * DESERIALIZACIÓN (Cargar desde JSON):
 * 
 * string jsonContent = File.ReadAllText("cartas.json");
 * CartasEspecialesRoot root = JsonUtility.FromJson<CartasEspecialesRoot>(jsonContent);
 * 
 * // Acceder a los beneficios del primer elemento
 * List<Carta> primeroseBeneficios = root.Cards[0].benefits;
 * 
 * // Acceder a las penalizaciones del primer elemento
 * List<Carta> primerasPenalizaciones = root.Cards[0].penalty;
 * 
 * 
 * SERIALIZACIÓN (Guardar a JSON):
 * 
 * CartasEspecialesRoot root = new CartasEspecialesRoot();
 * root.Cards = new List<CartaData>();
 * 
 * CartaData cartaData = new CartaData();
 * cartaData.benefits = new List<Carta>() { nuevaCarta1, nuevaCarta2 };
 * cartaData.penalty = new List<Carta>() { penalizacion1 };
 * 
 * root.Cards.Add(cartaData);
 * 
 * string json = JsonUtility.ToJson(root, true); // true = formato legible
 * File.WriteAllText("cartas.json", json);
 */

// ============================================
// NOTAS DE DISEÑO
// ============================================
/*
 * VENTAJAS DE ESTA ESTRUCTURA:
 * 
 * 1. ORGANIZACIÓN: Separa claramente cartas positivas y negativas,
 *    facilitando su gestión y balanceo del juego.
 * 
 * 2. ESCALABILIDAD: Puedes tener múltiples CartaData en el array "Cards",
 *    permitiendo diferentes "mazos" o "categorías" de cartas.
 *    Ejemplo: Cards[0] = cartas de historia, Cards[1] = cartas de ciencia
 * 
 * 3. COMPATIBILIDAD JSON: La estructura coincide perfectamente con
 *    formatos JSON estándar, facilitando la edición externa del contenido.
 * 
 * 4. REUTILIZACIÓN: La clase Carta definida en Carta.cs se reutiliza aquí,
 *    manteniendo el código DRY (Don't Repeat Yourself).
 * 
 * CONSIDERACIONES:
 * 
 * - Los nombres de las propiedades (Cards, benefits, penalty) deben coincidir
 *   EXACTAMENTE con las claves del JSON (C# es case-sensitive para esto).
 * 
 * - Si el JSON usa "Benefits" con mayúscula y aquí pones "benefits" minúscula,
 *   la deserialización fallará o devolverá null.
 * 
 * - Esta es una estructura de datos pura (no tiene lógica de negocio),
 *   solo sirve para mapear el JSON a objetos C#.
 */