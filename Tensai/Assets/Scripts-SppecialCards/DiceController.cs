using UnityEngine; // Necesario para MonoBehaviour
using UnityEngine.UI; // Necesario para Button
using System.Collections; // Necesario para IEnumerator y Corrutinas
using TMPro;  // Necesario para TextMeshProUGUI

/// <summary>
/// Controla la mecánica del dado en el juego de mesa.
/// Gestiona la animación de tirada, el resultado aleatorio y el movimiento del jugador.
/// Incluye sistema de bloqueo para prevenir tiradas durante eventos especiales.
/// </summary>
public class DiceController : MonoBehaviour
{
    // ============================================
    // SECCIÓN 1: ELEMENTOS DE LA INTERFAZ
    // ============================================
    
    /// <summary>
    /// Texto que muestra el número del dado durante y después de la tirada.
    /// Se actualiza rápidamente durante la animación para simular el giro del dado.
    /// </summary>
    public TextMeshProUGUI diceText;
    
    /// <summary>
    /// Botón que el jugador presiona para tirar el dado.
    /// Se desactiva durante la tirada y cuando el dado está bloqueado.
    /// </summary>
    public Button diceButton;
    
    /// <summary>
    /// Referencia al script del jugador para ejecutar el movimiento
    /// según el resultado de la tirada del dado.
    /// </summary>
    public MovePlayer player;

    // ============================================
    // SECCIÓN 2: CONFIGURACIÓN DEL DADO
    // ============================================
    
    /// <summary>
    /// Valor mínimo que puede salir en el dado (típicamente 1).
    /// </summary>
    public int minNumber = 1;
    
    /// <summary>
    /// Valor máximo que puede salir en el dado (típicamente 6).
    /// </summary>
    public int maxNumber = 6;
    
    /// <summary>
    /// Duración total de la animación de tirada del dado en segundos.
    /// Durante este tiempo, los números cambiarán rápidamente.
    /// </summary>
    public float rollDuration = 1f;
    
    /// <summary>
    /// Intervalo de tiempo entre cada cambio de número durante la animación.
    /// Valores más pequeños = animación más rápida y fluida.
    /// </summary>
    public float interval = 0.05f;

    // ============================================
    // SECCIÓN 3: VARIABLES DE ESTADO
    // ============================================
    
    /// <summary>
    /// Indica si el dado está actualmente en proceso de tirada.
    /// Previene tiradas múltiples simultáneas.
    /// </summary>
    private bool isRolling = false;
    
    /// <summary>
    /// Indica si el dado está bloqueado por eventos externos.
    /// Cuando está bloqueado, el jugador no puede tirar el dado.
    /// Usado por CartaManager para controlar el flujo del juego.
    /// </summary>
    private bool dadoBloqueado = false;

    // ============================================
    // SECCIÓN 4: INICIALIZACIÓN
    // ============================================
    
    void Start()
    {
        // Configurar el listener del botón para que ejecute RollDice al hacer clic
        diceButton.onClick.AddListener(RollDice);
    }

    // ============================================
    // SECCIÓN 5: MÉTODO DE TIRADA DEL DADO
    // ============================================
    
    /// <summary>
    /// Método público que se ejecuta cuando el jugador hace clic en el botón del dado.
    /// Verifica que no haya una tirada en progreso y que el dado no esté bloqueado
    /// antes de iniciar la corrutina de animación.
    /// </summary>
    void RollDice()
    {
        // Solo tirar el dado si:
        // 1. No hay una tirada en progreso (isRolling == false)
        // 2. El dado no está bloqueado (dadoBloqueado == false)
        if (!isRolling && !dadoBloqueado)
            StartCoroutine(RollDiceCoroutine());
    }

    // ============================================
    // SECCIÓN 6: SISTEMA DE BLOQUEO DEL DADO
    // ============================================
    
    /// <summary>
    /// Bloquea o desbloquea el dado según el parámetro recibido.
    /// Usado por otros sistemas (como CartaManager) para controlar cuándo
    /// el jugador puede tirar el dado.
    /// </summary>
    /// <param name="bloquear">
    /// true = bloquear el dado (el jugador no puede tirar)
    /// false = desbloquear el dado (el jugador puede tirar)
    /// </param>
    public void BloquearDado(bool bloquear)
    {
        // Actualizar el estado de bloqueo
        dadoBloqueado = bloquear;
        
        // Hacer el botón interactivo o no según el estado de bloqueo
        // Si está bloqueado: botón deshabilitado (gris, no clickeable)
        // Si está desbloqueado: botón habilitado (normal, clickeable)
        diceButton.interactable = !bloquear;
    }

    // ============================================
    // SECCIÓN 7: CORRUTINA DE ANIMACIÓN Y TIRADA
    // ============================================
    
    /// <summary>
    /// Corrutina que ejecuta la animación de tirada del dado.
    /// 
    /// FLUJO:
    /// 1. Bloquea el dado para prevenir tiradas múltiples
    /// 2. Muestra números aleatorios rápidamente (efecto de "girando")
    /// 3. Se detiene en un número final aleatorio
    /// 4. Mueve al jugador según el resultado
    /// 5. Desbloquea el dado para la siguiente tirada
    /// </summary>
    IEnumerator RollDiceCoroutine()
    {
        // ====== FASE 1: PREPARACIÓN ======
        
        // Marcar que la tirada está en progreso
        isRolling = true;
        
        // Deshabilitar el botón para prevenir clics durante la animación
        diceButton.interactable = false;

        // ====== FASE 2: ANIMACIÓN DE TIRADA ======
        
        // Variable para rastrear el tiempo transcurrido
        float elapsed = 0f;
        
        // Variable que almacenará el número mostrado
        int numero = 1;

        // Bucle de animación: cambiar números rápidamente durante rollDuration segundos
        while (elapsed < rollDuration)
        {
            // Generar un número aleatorio entre minNumber y maxNumber (inclusivo)
            numero = Random.Range(minNumber, maxNumber + 1);
            
            // Actualizar el texto mostrado con el nuevo número
            diceText.text = numero.ToString();
            
            // Esperar un intervalo corto antes del siguiente cambio
            yield return new WaitForSeconds(interval);
            
            // Incrementar el tiempo transcurrido
            elapsed += interval;
        }

        // ====== FASE 3: RESULTADO FINAL ======
        
        // Mostrar el número final una vez más (asegurar que sea visible)
        diceText.text = numero.ToString();
        
        // ====== FASE 4: MOVER AL JUGADOR ======
        
        // Esperar a que el jugador complete su movimiento
        // JumpMultipleTimes mueve al jugador 'numero' casillas
        yield return StartCoroutine(player.JumpMultipleTimes(numero));

        // ====== FASE 5: FINALIZACIÓN ======
        
        // Marcar que la tirada ha terminado
        isRolling = false;
        
        // Nota: El botón se reactivará solo si dadoBloqueado == false
        // Esto lo maneja automáticamente el próximo intento de tirada
        // o puede ser controlado por BloquearDado() desde CartaManager
    }
}

// ============================================
// NOTAS DE DISEÑO Y USO
// ============================================
/*
 * FLUJO COMPLETO DE UNA TIRADA:
 * 
 * 1. Jugador hace clic en el botón del dado
 * 2. RollDice() verifica que es seguro tirar
 * 3. Se inicia RollDiceCoroutine()
 * 4. Durante ~1 segundo: números cambian rápidamente (efecto visual)
 * 5. Se determina el número final
 * 6. El jugador se mueve automáticamente ese número de casillas
 * 7. Cuando termina el movimiento, el dado queda listo para la próxima tirada
 * 
 * SISTEMA DE BLOQUEO:
 * 
 * El dado puede bloquearse desde scripts externos (típicamente CartaManager):
 * - Bloqueado durante cartas de trivia (evita tirar mientras se responde)
 * - Bloqueado por penalidades (perder turno)
 * - Bloqueado durante animaciones especiales
 * 
 * Ejemplo de uso desde otro script:
 *   diceController.BloquearDado(true);  // Bloquear
 *   // ... hacer algo ...
 *   diceController.BloquearDado(false); // Desbloquear
 * 
 * PERSONALIZACIÓN:
 * 
 * - minNumber/maxNumber: Cambiar rango del dado (ej: 1-10, 2-8)
 * - rollDuration: Hacer la animación más larga o corta
 * - interval: Controlar velocidad del cambio de números
 * 
 * DIFERENCIA ENTRE isRolling Y dadoBloqueado:
 * 
 * - isRolling: Estado temporal durante la tirada (interno)
 * - dadoBloqueado: Estado persistente controlado externamente
 *   (ej: perder turno mantiene el bloqueo hasta que se libere)
 */