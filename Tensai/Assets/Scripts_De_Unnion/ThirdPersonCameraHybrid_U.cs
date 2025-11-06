using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Cámara híbrida orbital en tercera persona (PC + móvil).
/// 
/// 🔹 Características combinadas:
/// - Control de órbita con touch o mouse
/// - Detección automática de UI (evita giros mientras tocas botones)
/// - Seguimiento suave del objetivo con blending configurable
/// - Opción de seguimiento solo horizontal (sin "saltos" verticales)
/// - Transiciones suaves entre objetivos (`FocusTo`)
/// - Permite habilitar o deshabilitar control manual de órbita
/// 
/// Ideal para juegos de mesa o de tablero 3D.
/// </summary>
public class ThirdPersonCameraHybrid_U : MonoBehaviour
{
    // ============================================
    // SECCIÓN 1: CONFIGURACIÓN BASE
    // ============================================

    [Header("Target y posición base")]
    [Tooltip("El objeto que la cámara seguirá (por ejemplo, el jugador).")]
    public Transform target;
    [Tooltip("Distancia de la cámara al target.")]
    public float distance = 5f;
    [Tooltip("Altura de la cámara respecto al target.")]
    public float height = 2f;
    [Tooltip("Tiempo de blend mientras seguimos al mismo objetivo.")]
    public float focusBlendTime = 0.4f;

    [Header("Órbita manual")]
    [Tooltip("Permitir que el usuario gire la cámara manualmente.")]
    public bool allowUserOrbit = true;
    [Tooltip("Sensibilidad del movimiento de cámara.")]
    public float rotationSpeed = 0.2f;
    [Tooltip("Ángulo mínimo vertical (5 = casi horizontal).")]
    public float minPitch = 5f;
    [Tooltip("Ángulo máximo vertical (60 = vista elevada).")]
    public float maxPitch = 60f;

    [Header("Seguimiento horizontal")]
    [Tooltip("Si está activo, el foco solo sigue horizontalmente (no salta en altura).")]
    public bool followHorizontalOnly = true;

    [Header("Transición entre objetivos")]
    [Tooltip("Velocidad de desplazamiento del foco al cambiar de objetivo (m/s).")]
    public float targetTravelSpeed = 8f;
    [Tooltip("Distancia para considerar que se ha llegado al nuevo objetivo.")]
    public float targetArriveThreshold = 0.05f;

    // ============================================
    // SECCIÓN 2: VARIABLES DE ESTADO
    // ============================================

    private float currentX = 0f;
    private float currentY = 15f;

    private Vector3 focusPoint;
    private bool isTraveling = false;

    private Vector2 lastInputPos;
    private bool isDragging = false;

    public bool IsTraveling => isTraveling;

    /// <summary>
    /// Indica globalmente si el usuario está girando la cámara.
    /// Permite que otros scripts bloqueen interacciones (por ejemplo, tirar dados).
    /// </summary>
    public static bool IsCameraDragging { get; private set; } = false;

    // ============================================
    // SECCIÓN 3: INICIALIZACIÓN
    // ============================================

    void Awake()
    {
        if (target != null)
            focusPoint = target.position;

        var e = transform.eulerAngles;
        currentX = e.y;
        currentY = Mathf.Clamp(e.x, minPitch, maxPitch);
    }

    // ============================================
    // SECCIÓN 4: CONTROL DE INPUT (TOUCH + MOUSE)
    // ============================================

    void Update()
    {
        if (!allowUserOrbit) return;

        if (TryBeginDrag())
        {
            lastInputPos = GetPointerPosition();
            isDragging = true;
            IsCameraDragging = true;
        }
        else if (TryEndDrag())
        {
            isDragging = false;
            IsCameraDragging = false;
        }

        if (isDragging && TryGetPointerDelta(out Vector2 delta))
        {
            currentX += delta.x * rotationSpeed;
            currentY -= delta.y * rotationSpeed;
            currentY = Mathf.Clamp(currentY, minPitch, maxPitch);
        }
    }

    // ============================================
    // SECCIÓN 5: ACTUALIZACIÓN DE POSICIÓN
    // ============================================

    void LateUpdate()
    {
        if (target == null) return;

        // Actualizar foco (blend o viaje activo)
        if (!isTraveling)
        {
            Vector3 desired = target.position;
            if (followHorizontalOnly)
                desired.y = focusPoint.y;

            if (focusBlendTime > 0f)
            {
                float k = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, focusBlendTime));
                focusPoint = Vector3.Lerp(focusPoint, desired, Mathf.Clamp01(k));
            }
            else
            {
                focusPoint = desired;
            }
        }

        // Aplicar rotación y posición final
        Quaternion rot = Quaternion.Euler(currentY, currentX, 0f);
        Vector3 offset = rot * new Vector3(0f, height, -distance);
        transform.position = focusPoint + offset;

        transform.LookAt(focusPoint + Vector3.up * 1.5f);
    }

    // ============================================
    // SECCIÓN 6: API PÚBLICA
    // ============================================

    /// <summary>
    /// Transición suave al nuevo objetivo (bloquea blend automático hasta llegar).
    /// </summary>
    public IEnumerator FocusTo(Transform newTarget, float? travelSpeedOverride = null, bool? keepHorizontalOnly = null)
    {
        if (newTarget == null) yield break;

        target = newTarget;
        isTraveling = true;

        float speed = travelSpeedOverride ?? targetTravelSpeed;
        bool lockY = keepHorizontalOnly ?? followHorizontalOnly;

        while (true)
        {
            Vector3 desired = newTarget.position;
            if (lockY) desired.y = focusPoint.y;

            focusPoint = Vector3.MoveTowards(focusPoint, desired, speed * Time.deltaTime);
            if ((focusPoint - desired).sqrMagnitude <= targetArriveThreshold * targetArriveThreshold)
            {
                focusPoint = desired;
                break;
            }
            yield return null;
        }

        isTraveling = false;
    }

    /// <summary>
    /// Cambia el objetivo de la cámara (opcionalmente de forma instantánea).
    /// </summary>
    public void SetTarget(Transform newTarget, bool smooth = true)
    {
        if (newTarget == null) return;
        target = newTarget;

        if (!smooth)
        {
            Vector3 desired = newTarget.position;
            if (followHorizontalOnly) desired.y = focusPoint.y;
            focusPoint = desired;
        }
    }

    /// <summary>
    /// Habilita o deshabilita el control manual del usuario.
    /// </summary>
    public void SetUserControl(bool enabled) => allowUserOrbit = enabled;

    // ============================================
    // SECCIÓN 7: INPUT HELPERS
    // ============================================

    private Vector2 GetPointerPosition()
    {
        if (Input.touchCount > 0) return Input.GetTouch(0).position;
        return (Vector2)Input.mousePosition;
    }

    private bool IsPointerOverUIAny()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return true;
        if (EventSystem.current != null && Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            if (EventSystem.current.IsPointerOverGameObject(t.fingerId)) return true;
        }
        return false;
    }

    private bool TryBeginDrag()
    {
        if (IsPointerOverUIAny()) return false;
        if (Input.touchCount == 1) return Input.GetTouch(0).phase == TouchPhase.Began;
        return Input.GetMouseButtonDown(0);
    }

    private bool TryEndDrag()
    {
        if (Input.touchCount == 0) return isDragging && !Input.GetMouseButton(0);
        if (Input.touchCount == 1)
        {
            var t = Input.GetTouch(0);
            return t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
        }
        return isDragging;
    }

    private bool TryGetPointerDelta(out Vector2 delta)
    {
        delta = Vector2.zero;
        if (IsPointerOverUIAny()) return false;

        if (Input.touchCount == 1)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Moved)
            {
                delta = t.deltaPosition;
                return true;
            }
            lastInputPos = t.position; return false;
        }

        if (Input.GetMouseButton(0))
        {
            Vector2 cur = (Vector2)Input.mousePosition;
            delta = cur - lastInputPos;
            lastInputPos = cur;
            return delta.sqrMagnitude > 0.0f;
        }
        return false;
    }
}
