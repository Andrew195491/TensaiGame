using UnityEngine; // Necesario para MonoBehaviour
using System.Collections.Generic;  // Necesario para List<T> (colecciones genéricas)

/// <summary>
/// Gestor del sistema de inventario de cartas bonus del jugador.
/// 
/// ⚠️ NOTA IMPORTANTE: Este script parece ser una versión ANTIGUA o ALTERNATIVA
/// del sistema de gestión de cartas. En el proyecto actual, CartaManager maneja
/// esta funcionalidad de forma más completa.
/// 
/// Este script podría estar:
/// - En desuso (legacy code)
/// - Ser una versión simplificada para pruebas
/// - Usarse en una escena diferente del proyecto
/// 
/// Funcionalidad principal:
/// - Almacena hasta 3 cartas bonus
/// - Permite agregar nuevas cartas al inventario
/// - Permite usar cartas aplicando sus efectos
/// - Actualiza la UI del inventario
/// </summary>
public class PlayerBonusManager : MonoBehaviour
{
    // ============================================
    // SECCIÓN 1: SINGLETON Y CONFIGURACIÓN
    // ============================================
    
    /// <summary>
    /// Instancia singleton para acceso global desde otros scripts.
    /// Permite que cualquier parte del código acceda a este gestor.
    /// Ejemplo: PlayerBonusManager.instancia.AgregarCarta(carta, jugador);
    /// </summary>
    public static PlayerBonusManager instancia;

    /// <summary>
    /// Capacidad máxima del inventario de cartas bonus.
    /// Por defecto 3, igual que en CartaManager.
    /// </summary>
    public int maxCartas = 3;
    
    /// <summary>
    /// Lista que almacena las cartas bonus actualmente en el inventario del jugador.
    /// Similar a la variable "storage" en CartaManager.
    /// </summary>
    public List<Carta> cartasBonus = new List<Carta>();

    /// <summary>
    /// Referencia a la interfaz de usuario que muestra las cartas bonus.
    /// Debe asignarse manualmente en el Inspector de Unity.
    /// </summary>
    public BonusUI bonusUI; // Asignar en el inspector

    // ============================================
    // SECCIÓN 2: INICIALIZACIÓN
    // ============================================
    
    void Awake()
    {
        // Establecer esta instancia como el singleton
        instancia = this;
    }

    // ============================================
    // SECCIÓN 3: AGREGAR CARTA AL INVENTARIO
    // ============================================
    
    /// <summary>
    /// Intenta agregar una nueva carta al inventario del jugador.
    /// Solo se agrega si hay espacio disponible (menos de maxCartas).
    /// 
    /// ⚠️ DIFERENCIA CON CartaManager:
    /// CartaManager.IntentarAgregarCarta() muestra un panel de reemplazo cuando está lleno.
    /// Este método simplemente muestra un mensaje de advertencia.
    /// </summary>
    /// <param name="nuevaCarta">Carta a agregar al inventario</param>
    /// <param name="jugador">Referencia al jugador que obtiene la carta</param>
    public void AgregarCarta(Carta nuevaCarta, MovePlayer jugador)
    {
        // Verificar si hay espacio disponible en el inventario
        if (cartasBonus.Count < maxCartas)
        {
            // Agregar la carta a la lista
            cartasBonus.Add(nuevaCarta);
            
            // Actualizar la interfaz de usuario para mostrar la nueva carta
            bonusUI.ActualizarUI(cartasBonus);
            
            // Log informativo con el nombre del jugador y la carta obtenida
            Debug.Log($"{jugador.name} obtuvo una carta bonus: {nuevaCarta.pregunta}");
        }
        else
        {
            // Si el inventario está lleno, solo mostrar advertencia
            // NO hay sistema de reemplazo en esta versión
            Debug.Log("⚠ Inventario lleno: deberías usar una antes de agregar otra.");
        }
    }

    // ============================================
    // SECCIÓN 4: USAR CARTA DEL INVENTARIO
    // ============================================
    
    /// <summary>
    /// Usa una carta del inventario aplicando su efecto al jugador.
    /// Después de usar la carta, la elimina del inventario.
    /// </summary>
    /// <param name="indice">Posición de la carta en el inventario (0, 1 o 2)</param>
    /// <param name="jugador">Jugador sobre el cual se aplica el efecto</param>
    public void UsarCarta(int indice, MovePlayer jugador)
    {
        // Validar que el índice sea válido
        // Si está fuera del rango, salir silenciosamente
        if (indice < 0 || indice >= cartasBonus.Count) return;

        // Obtener la carta en la posición indicada
        Carta carta = cartasBonus[indice];
        
        // Aplicar el efecto de la carta al jugador
        AplicarEfecto(carta, jugador);

        // Eliminar la carta del inventario después de usarla
        cartasBonus.RemoveAt(indice);
        
        // Actualizar la UI para reflejar el cambio
        bonusUI.ActualizarUI(cartasBonus);
    }

    // ============================================
    // SECCIÓN 5: APLICAR EFECTOS DE CARTAS
    // ============================================
    
    /// <summary>
    /// Aplica el efecto de una carta bonus al jugador.
    /// 
    /// ⚠️ SISTEMA SIMPLIFICADO Y LIMITADO:
    /// Este método usa detección de texto (Contains) en lugar del campo "accion".
    /// Solo implementa 2 efectos básicos.
    /// 
    /// CartaManager tiene un sistema mucho más robusto con:
    /// - Campo "accion" específico (Avanza1, Retrocede2, etc.)
    /// - 17+ efectos diferentes implementados
    /// - Lógica más compleja y completa
    /// </summary>
    /// <param name="carta">Carta cuyo efecto se va a aplicar</param>
    /// <param name="jugador">Jugador que recibe el efecto</param>
    private void AplicarEfecto(Carta carta, MovePlayer jugador)
    {
        // ====== EFECTO 1: AVANZAR ======
        
        // Detectar si el texto de la pregunta contiene "Avanzas"
        if (carta.pregunta.Contains("Avanzas"))
        {
            // Mover al jugador 2 casillas hacia adelante
            jugador.StartCoroutine(jugador.JumpMultipleTimes(2));
        }
        
        // ====== EFECTO 2: RETROCEDER ======
        
        // Detectar si el texto de la pregunta contiene "Retrocedes"
        else if (carta.pregunta.Contains("Retrocedes"))
        {
            // Mover al jugador 3 casillas hacia atrás
            jugador.StartCoroutine(jugador.Retroceder(3));
        }
        
        // ⚠️ PROBLEMA: No hay caso por defecto (else)
        // Si la carta no contiene estos textos, no hace nada
    }
}

// ============================================
// COMPARACIÓN CON CartaManager
// ============================================
/*
 * ESTE SCRIPT (PlayerBonusManager):
 * ✓ Más simple y fácil de entender
 * ✓ Código mínimo para funcionalidad básica
 * ✗ Sistema de efectos limitado (solo 2 tipos)
 * ✗ Detección de efectos frágil (basada en texto)
 * ✗ No hay sistema de reemplazo cuando está lleno
 * ✗ No hay categorización de cartas (benefits/penalty)
 * ✗ No integra con JSON
 * 
 * CartaManager (VERSION ACTUAL):
 * ✓ Sistema completo con 17+ efectos diferentes
 * ✓ Usa campo "accion" específico (robusto)
 * ✓ Sistema de reemplazo cuando el inventario está lleno
 * ✓ Carga cartas desde JSON externo
 * ✓ Categoriza cartas (benefits/penalty)
 * ✓ Integra con todas las mecánicas del juego
 * ✗ Más complejo de entender
 * 
 * RECOMENDACIÓN:
 * - Si CartaManager existe en el proyecto, este script probablemente
 *   puede eliminarse o está siendo usado para pruebas.
 * - Si quieres usar este script, deberías expandir AplicarEfecto()
 *   para incluir todos los tipos de cartas del juego.
 */

// ============================================
// EJEMPLO DE MEJORA SUGERIDA
// ============================================
/*
 * Para hacer este script más robusto, AplicarEfecto debería cambiar a:
 * 
 * private void AplicarEfecto(Carta carta, MovePlayer jugador)
 * {
 *     // Usar el campo "accion" en lugar de buscar en el texto
 *     switch (carta.accion)
 *     {
 *         case "Avanza1":
 *             jugador.StartCoroutine(jugador.JumpMultipleTimes(1));
 *             break;
 *         case "Avanza2":
 *             jugador.StartCoroutine(jugador.JumpMultipleTimes(2));
 *             break;
 *         case "Retrocede3":
 *             jugador.StartCoroutine(jugador.Retroceder(3));
 *             break;
 *         // ... más casos según las cartas del juego
 *         default:
 *             Debug.LogWarning($"Efecto no reconocido: {carta.accion}");
 *             break;
 *     }
 * }
 * 
 * Este enfoque es más mantenible y menos propenso a errores.
 */