using System;
using System.Collections.Generic;

// ============================================
// CLASE RAÍZ: CartasDB_U
// ============================================

/// <summary>
/// Representa la base de datos completa de cartas del juego,
/// incluyendo tanto las cartas de trivia (historia, geografía, ciencia)
/// como las cartas especiales (beneficios y penalidades).
/// 
/// Es el modelo principal para deserializar archivos JSON como:
/// {
///   "historia": [ ... ],
///   "geografia": [ ... ],
///   "ciencia": [ ... ],
///   "benefits": [ ... ],
///   "penalty": [ ... ]
/// }
/// </summary>
[Serializable]
public class CartasDB_U
{
    // =============================
    // CARTAS DE TRIVIA
    // =============================

    /// <summary>
    /// Lista de cartas de la categoría Historia.
    /// </summary>
    public List<Carta_U> historia;

    /// <summary>
    /// Lista de cartas de la categoría Geografía.
    /// </summary>
    public List<Carta_U> geografia;

    /// <summary>
    /// Lista de cartas de la categoría Ciencia.
    /// </summary>
    public List<Carta_U> ciencia;

    // =============================
    // CARTAS ESPECIALES
    // =============================

    /// <summary>
    /// Lista de cartas con efectos positivos o beneficiosos.
    /// Ejemplo: Avanza1, RepiteTurno, TeletransporteAdelante, etc.
    /// </summary>
    public List<Carta_U> benefits;

    /// <summary>
    /// Lista de cartas con efectos negativos o penalizaciones.
    /// Ejemplo: Retrocede2, PierdeTurno, IrSalida, etc.
    /// </summary>
    public List<Carta_U> penalty;
}

// ============================================
// CLASES COMPLEMENTARIAS: CartasEspecialesRoot_U y CartaData_U
// ============================================

/// <summary>
/// Clase envoltorio que representa la raíz del JSON de cartas especiales.
/// Estructura esperada:
/// {
///   "Cards": [
///     { "benefits": [...], "penalty": [...] },
///     { "benefits": [...], "penalty": [...] }
///   ]
/// }
/// </summary>
[Serializable]
public class CartasEspecialesRoot_U
{
    /// <summary>
    /// Lista de grupos de cartas especiales (beneficios + penalidades).
    /// </summary>
    public List<CartaData_U> Cards;
}

/// <summary>
/// Representa un grupo de cartas especiales dentro del array "Cards".
/// Cada grupo contiene listas separadas para beneficios y penalizaciones.
/// </summary>
[Serializable]
public class CartaData_U
{
    /// <summary>
    /// Cartas con efectos positivos.
    /// </summary>
    public List<Carta_U> benefits;

    /// <summary>
    /// Cartas con efectos negativos.
    /// </summary>
    public List<Carta_U> penalty;
}

// ============================================
// NOTAS DE DISEÑO
// ============================================
/*
 * 🔹 Esta versión unifica los sistemas de:
 *   - Cartas de trivia (CartasDB2)
 *   - Cartas especiales (CartaData / CartasEspecialesRoot)
 * 
 * 🔹 Se utiliza en:
 *   - CartaManager_U (gestión completa)
 *   - Lectura de archivos JSON en Resources o asignados por Inspector
 * 
 * 🔹 Ventajas:
 *   - Una sola estructura de datos para todo el sistema de cartas
 *   - Compatible con JsonUtility de Unity
 *   - Soporte para expansión: se pueden añadir más categorías fácilmente
 * 
 * 🔹 Uso típico:
 *   CartasDB_U db = JsonUtility.FromJson<CartasDB_U>(json);
 *   var carta = db.historia[0]; // Acceder a una carta de historia
 *   var beneficio = db.benefits[0]; // Acceder a una carta de beneficio
 */
