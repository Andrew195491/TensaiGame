using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Cámara híbrida orbital en tercera persona (PC + móvil).
/// - Órbita con touch/mouse
/// - Evita iniciar drag sobre UI, pero permite continuar el drag aunque pases por encima de UI
/// - Seguimiento suave del objetivo
/// - Transición FocusTo
/// </summary>
public class ThirdPersonCameraHybrid_U : MonoBehaviour
{
    [Header("Target y posición base")]
    public Transform target;
    public float distance = 5f;
    public float height = 2f;
    public float focusBlendTime = 0.4f;

    [Header("Órbita manual")]
    public bool allowUserOrbit = true;
    public float rotationSpeed = 0.2f;
    public float minPitch = 5f;
    public float maxPitch = 60f;

    [Header("Seguimiento horizontal")]
    public bool followHorizontalOnly = true;

    [Header("Transición entre objetivos")]
    public float targetTravelSpeed = 8f;
    public float targetArriveThreshold = 0.05f;

    // --- estado ---
    private float currentX = 0f;
    private float currentY = 15f;

    private Vector3 focusPoint;
    private bool isTraveling = false;

    private Vector2 lastInputPos;
    private bool isDragging = false;

    public bool IsTraveling => isTraveling;

    /// Indica si el usuario está girando la cámara (útil para bloquear otras UIs)
    public static bool IsCameraDragging { get; private set; } = false;

    void Awake()
    {
        if (target != null) focusPoint = target.position;

        var e = transform.eulerAngles;
        currentX = e.y;
        currentY = Mathf.Clamp(e.x, minPitch, maxPitch);
    }

    void Update()
    {
        if (!allowUserOrbit) return;

        // 1) Intentar COMENZAR drag: solo si NO arrancamos sobre UI
        if (!isDragging && TryBeginDrag())
        {
            lastInputPos = GetPointerPosition();
            isDragging = true;
            IsCameraDragging = true;
        }

        // 2) Intentar TERMINAR drag
        if (isDragging && TryEndDrag())
        {
            isDragging = false;
            IsCameraDragging = false;
        }

        // 3) Mientras haya drag, seguir leyendo delta AUNQUE el dedo pase por UI
        if (isDragging && TryGetPointerDelta(out Vector2 delta))
        {
            currentX += delta.x * rotationSpeed;
            currentY -= delta.y * rotationSpeed;
            currentY = Mathf.Clamp(currentY, minPitch, maxPitch);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // actualizar foco (blend cuando no hay viaje FocusTo)
        if (!isTraveling)
        {
            Vector3 desired = target.position;
            if (followHorizontalOnly) desired.y = focusPoint.y;

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

        // aplicar rotación/posición
        Quaternion rot = Quaternion.Euler(currentY, currentX, 0f);
        Vector3 offset = rot * new Vector3(0f, height, -distance);
        transform.position = focusPoint + offset;
        transform.LookAt(focusPoint + Vector3.up * 1.5f);
    }

    // -------- API pública --------

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

    public void SetUserControl(bool enabled) => allowUserOrbit = enabled;

    // -------- Helpers de input --------

    private Vector2 GetPointerPosition()
    {
        if (Input.touchCount > 0) return Input.GetTouch(0).position;
        return (Vector2)Input.mousePosition;
    }

    private bool IsPointerOverUIAny()
    {
        // mouse
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return true;

        // touch
        if (EventSystem.current != null && Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            if (EventSystem.current.IsPointerOverGameObject(t.fingerId))
                return true;
        }
        return false;
    }

    /// Solo permite iniciar el drag si el primer toque/click NO está sobre UI
    private bool TryBeginDrag()
    {
        if (IsPointerOverUIAny()) return false;

        if (Input.touchCount == 1)
            return Input.GetTouch(0).phase == TouchPhase.Began;

        return Input.GetMouseButtonDown(0);
    }

    private bool TryEndDrag()
    {
        if (Input.touchCount == 0)
            return isDragging && !Input.GetMouseButton(0);

        if (Input.touchCount == 1)
        {
            var t = Input.GetTouch(0);
            return t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
        }
        return isDragging;
    }

    /// Si YA estamos arrastrando, NO bloqueamos por UI (permite pasar por encima de cartas)
    private bool TryGetPointerDelta(out Vector2 delta)
    {
        delta = Vector2.zero;

        // Solo bloqueamos por UI si NO hay drag activo
        if (!isDragging && IsPointerOverUIAny()) return false;

        if (Input.touchCount == 1)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Moved)
            {
                delta = t.deltaPosition;
                return true;
            }
            lastInputPos = t.position;
            return false;
        }

        if (Input.GetMouseButton(0))
        {
            Vector2 cur = (Vector2)Input.mousePosition;
            delta = cur - lastInputPos;
            lastInputPos = cur;
            return delta.sqrMagnitude > 0f;
        }

        return false;
    }
}
