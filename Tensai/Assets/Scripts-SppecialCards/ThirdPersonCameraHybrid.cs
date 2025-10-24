using UnityEngine; // Necesario para MonoBehaviour
using UnityEngine.EventSystems;  // Necesario para EventSystem.current


/// <summary>
/// Sistema de cámara en tercera persona híbrido que funciona tanto en móvil (touch) como en PC (mouse).
/// Permite orbitar alrededor del jugador con controles táctiles o de ratón.
/// 
/// CARACTERÍSTICAS:
/// - Detección automática de plataforma (móvil/PC)
/// - Prevención de conflictos con UI
/// - Rotación suave y limitada verticalmente
/// - Sistema de órbita alrededor del objetivo
/// </summary>
public class ThirdPersonCameraHybrid : MonoBehaviour
{
    // ============================================
    // SECCIÓN 1: CONFIGURACIÓN DE LA CÁMARA
    // ============================================
    
    [Header("Configuración de Target")]
    /// <summary>
    /// El objeto que la cámara seguirá (normalmente el jugador).
    /// La cámara orbitará alrededor de este punto.
    /// </summary>
    public Transform target;
    
    [Header("Ajustes de Posición")]
    /// <summary>
    /// Distancia radial entre la cámara y el target.
    /// Valores mayores = cámara más alejada (vista más amplia)
    /// Valores menores = cámara más cercana (vista más detallada)
    /// </summary>
    public float distance = 5f;
    
    /// <summary>
    /// Altura vertical de la cámara respecto al target.
    /// Valores mayores = vista más elevada (vista de pájaro)
    /// Valores menores = vista más baja (a nivel del suelo)
    /// </summary>
    public float height = 2f;
    
    [Header("Ajustes de Control")]
    /// <summary>
    /// Sensibilidad para la rotación de la cámara.
    /// Valores mayores = movimientos más rápidos/bruscos
    /// Valores menores = movimientos más lentos/suaves
    /// Típicamente entre 0.1 (lento) y 0.5 (rápido)
    /// </summary>
    public float rotationSpeed = 0.2f;

    // ============================================
    // SECCIÓN 2: VARIABLES DE ESTADO Y ROTACIÓN
    // ============================================
    
    /// <summary>
    /// Ángulo actual de rotación horizontal (alrededor del eje Y global).
    /// Controla el giro izquierda/derecha alrededor del target.
    /// Valores en grados: 0° = norte, 90° = este, 180° = sur, 270° = oeste
    /// </summary>
    private float currentX = 0f;
    
    /// <summary>
    /// Ángulo actual de rotación vertical (alrededor del eje X local).
    /// Controla la elevación de la cámara (arriba/abajo).
    /// Valores en grados: 0° = horizontal, 90° = mirando hacia abajo
    /// Comienza en 15° para una vista ligeramente elevada.
    /// </summary>
    private float currentY = 15f;

    // ====== VARIABLES PARA CONTROL CON MOUSE ======
    
    /// <summary>
    /// Última posición registrada del input (mouse/touch) en píxeles de pantalla.
    /// Se usa para calcular el delta (movimiento) entre frames.
    /// </summary>
    private Vector2 lastInputPos;
    
    /// <summary>
    /// Indica si el usuario está arrastrando con el mouse.
    /// true = botón izquierdo del mouse presionado y arrastrando
    /// false = mouse no está siendo arrastrado
    /// </summary>
    private bool isDragging = false;

    // ============================================
    // SECCIÓN 3: PROPIEDAD PÚBLICA DE ESTADO
    // ============================================
    
    /// <summary>
    /// Propiedad estática que indica si la cámara está siendo rotada actualmente.
    /// 
    /// UTILIDAD:
    /// - Otros scripts pueden verificar ThirdPersonCameraHybrid.IsCameraDragging
    /// - Permite deshabilitar otras interacciones mientras se ajusta la cámara
    /// - Útil para evitar tirar dados o hacer clic en botones mientras se rota la vista
    /// 
    /// NOTA: Es estática porque típicamente solo hay una cámara principal en la escena.
    /// </summary>
    public static bool IsCameraDragging { get; private set; } = false;

    // ============================================
    // SECCIÓN 4: DETECCIÓN Y PROCESAMIENTO DE INPUT
    // ============================================
    
    void Update()
    {
        // ========================================
        // SUBSECCIÓN A: CONTROL TÁCTIL (MÓVIL) 📱
        // ========================================
        
        // Verificar:
        // 1. Exactamente 1 dedo tocando la pantalla
        // 2. El toque NO está sobre elementos de UI
        if (Input.touchCount == 1 && !IsPointerOverUI())
        {
            // Obtener información del primer touch
            Touch touch = Input.GetTouch(0);
            
            // Delta = movimiento del dedo en este frame (en píxeles)
            Vector2 delta = touch.deltaPosition;

            // ------ INICIO DEL TOUCH ------
            if (touch.phase == TouchPhase.Began)
            {
                // El usuario comenzó a tocar la pantalla
                // Activar el estado de "rotando cámara"
                IsCameraDragging = true;
            }
            
            // ------ FIN DEL TOUCH ------
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                // El usuario levantó el dedo o el touch fue cancelado
                // Desactivar el estado de "rotando cámara"
                IsCameraDragging = false;
            }

            // ------ MOVIMIENTO ACTIVO ------
            if (touch.phase == TouchPhase.Moved)
            {
                // Actualizar rotación horizontal (izquierda/derecha)
                // delta.x positivo = arrastrar hacia la derecha = girar cámara a la derecha
                currentX += delta.x * rotationSpeed;
                
                // Actualizar rotación vertical (arriba/abajo)
                // delta.y negativo porque en pantalla Y crece hacia abajo
                // pero queremos que arrastrar hacia arriba eleve la cámara
                currentY -= delta.y * rotationSpeed;
                
                // Limitar el ángulo vertical entre 5° y 60°
                // 5° = vista casi horizontal (no puede ver debajo del target)
                // 60° = vista elevada pero no cenital completa
                currentY = Mathf.Clamp(currentY, 5f, 60f);
            }
        }

        // ========================================
        // SUBSECCIÓN B: CONTROL CON MOUSE (PC) 🖱️
        // ========================================
        
        // ------ INICIO DEL ARRASTRE ------
        // Al presionar el botón izquierdo del mouse Y no estar sobre UI
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
        {
            // Activar estado de arrastre
            isDragging = true;
            IsCameraDragging = true;
            
            // Guardar posición inicial del mouse para calcular deltas
            lastInputPos = Input.mousePosition;
        }
        
        // ------ FIN DEL ARRASTRE ------
        // Al soltar el botón izquierdo del mouse
        else if (Input.GetMouseButtonUp(0))
        {
            // Desactivar estado de arrastre
            isDragging = false;
            IsCameraDragging = false;
        }

        // ------ DURANTE EL ARRASTRE ------
        if (isDragging)
        {
            // Calcular cuánto se movió el mouse desde el último frame
            Vector2 delta = (Vector2)Input.mousePosition - lastInputPos;
            
            // Actualizar rotación horizontal
            // Movimiento positivo en X = mouse hacia la derecha = girar cámara a la derecha
            currentX += delta.x * rotationSpeed;
            
            // Actualizar rotación vertical
            // Invertimos delta.y por la misma razón que en touch
            currentY -= delta.y * rotationSpeed;
            
            // Limitar ángulo vertical
            currentY = Mathf.Clamp(currentY, 5f, 60f);
            
            // Actualizar la última posición para el próximo frame
            lastInputPos = Input.mousePosition;
        }
    }

    // ============================================
    // SECCIÓN 5: ACTUALIZACIÓN DE POSICIÓN Y ROTACIÓN
    // ============================================
    
    /// <summary>
    /// Se ejecuta después de Update, garantizando que el movimiento del jugador
    /// ya se haya procesado. Esto evita que la cámara "vibre" o "retrase".
    /// 
    /// PROCESO:
    /// 1. Convierte ángulos a rotación quaternion
    /// 2. Calcula posición offset rotada
    /// 3. Posiciona la cámara alrededor del target
    /// 4. Hace que la cámara mire hacia el target
    /// </summary>
    void LateUpdate()
    {
        // Validar que hay un target asignado
        if (target == null) return;

        // ====== PASO 1: CREAR ROTACIÓN ======
        
        // Convertir ángulos Euler (currentY, currentX) a Quaternion
        // Quaternion.Euler(pitch, yaw, roll)
        // - currentY = pitch (inclinación vertical)
        // - currentX = yaw (rotación horizontal)
        // - 0 = roll (no hay inclinación lateral)
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        
        // ====== PASO 2: CALCULAR OFFSET DIRECCIONAL ======
        
        // Vector offset base en espacio local:
        // - X = 0: no hay desplazamiento lateral
        // - Y = height: elevación de la cámara
        // - Z = -distance: la cámara está "detrás" (Z negativo mira hacia adelante)
        Vector3 direction = new Vector3(0, height, -distance);
        
        // ====== PASO 3: POSICIONAR LA CÁMARA ======
        
        // Fórmula final: Posición del target + offset rotado
        // rotation * direction rota el vector offset según los ángulos actuales
        // Esto crea el efecto de órbita alrededor del target
        transform.position = target.position + rotation * direction;
        
        // ====== PASO 4: ORIENTAR LA CÁMARA HACIA EL TARGET ======
        
        // Hacer que la cámara mire hacia el target
        // Vector3.up * 1.5f eleva ligeramente el punto de enfoque
        // (en lugar de mirar exactamente al centro del target, mira un poco arriba)
        // Esto crea un encuadre más natural
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }

    // ============================================
    // SECCIÓN 6: DETECCIÓN DE INTERFAZ GRÁFICA
    // ============================================
    
    /// <summary>
    /// Verifica si el puntero/touch está sobre un elemento de la UI.
    /// 
    /// PROPÓSITO:
    /// Prevenir que la cámara gire cuando el usuario interactúa con botones,
    /// paneles u otros elementos de la interfaz.
    /// 
    /// EJEMPLO:
    /// - Usuario hace clic en el botón del dado
    /// - Sin esta verificación: la cámara también giraría
    /// - Con esta verificación: solo el botón recibe el input
    /// </summary>
    /// <returns>true si el puntero está sobre UI, false en caso contrario</returns>
    private bool IsPointerOverUI()
    {
        // EventSystem.current: Sistema de Unity que gestiona eventos de UI
        // IsPointerOverGameObject(): Devuelve true si el puntero está sobre UI
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}

// ============================================
// MATEMÁTICA DE LA ÓRBITA EXPLICADA
// ============================================
/*
 * SISTEMA DE COORDENADAS:
 * 
 * La cámara orbita alrededor del target usando coordenadas esféricas:
 * 
 *         Y (altura)
 *         ^
 *         |    / Cámara
 *         |   /
 *         |  / distance
 *         | /
 *         |/___currentX_____> X
 *        Target
 *       /
 *      / currentY (ángulo vertical)
 *     Z
 * 
 * TRANSFORMACIÓN:
 * 1. Definimos offset base: (0, height, -distance)
 * 2. Creamos rotación desde ángulos: Quaternion.Euler(currentY, currentX, 0)
 * 3. Rotamos el offset: rotation * direction
 * 4. Sumamos al target: target.position + offset_rotado
 * 
 * RESULTADO:
 * La cámara siempre mantiene la distancia y altura configuradas,
 * pero rota libremente alrededor del target según el input del usuario.
 */

// ============================================
// DIFERENCIAS ENTRE TOUCH Y MOUSE
// ============================================
/*
 * TOUCH (MÓVIL):
 * ✓ Detecta fases: Began, Moved, Ended, Canceled
 * ✓ Proporciona delta automáticamente (touch.deltaPosition)
 * ✓ Multi-touch nativo (pero este script usa solo 1 dedo)
 * ✓ No necesita tracking manual de posición
 * 
 * MOUSE (PC):
 * ✓ Usa eventos de botón: Down, Up
 * ✓ Requiere calcular delta manualmente (posición actual - anterior)
 * ✓ Necesita variable isDragging para tracking de estado
 * ✓ Debe guardar lastInputPos para calcular movimiento
 * 
 * UNIFICACIÓN:
 * Ambos sistemas actualizan las mismas variables (currentX, currentY),
 * por lo que el código de LateUpdate funciona idénticamente para ambos.
 */

// ============================================
// CASOS DE USO Y CONSIDERACIONES
// ============================================
/*
 * CUÁNDO USAR ESTA CÁMARA:
 * ✓ Juegos de mesa en 3D (como este proyecto)
 * ✓ Visualizadores de modelos 3D
 * ✓ Juegos de estrategia con vista de tablero
 * ✓ Aplicaciones que requieren inspección de objetos
 * 
 * LIMITACIONES:
 * - Solo rota alrededor del target (no se traslada)
 * - No tiene zoom (podría agregarse ajustando 'distance')
 * - No tiene colisión con geometría del nivel
 * 
 * MEJORAS POSIBLES:
 * 1. Agregar zoom con scroll del mouse o pinch en móvil
 * 2. Suavizado de movimiento (lerp/damping)
 * 3. Detección de colisiones con el entorno
 * 4. Múltiples targets o transición entre targets
 * 5. Shake effects para eventos especiales
 */