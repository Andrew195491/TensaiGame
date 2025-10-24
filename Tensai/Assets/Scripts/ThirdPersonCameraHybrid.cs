using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Cámara orbital híbrida con:
///  - Arrastre como móvil/ratón.
///  - Seguimiento SOLO horizontal (no "saltos" verticales con la ficha).
///  - Transición suave al cambiar de objetivo y opción de esperar hasta llegar.
/// </summary>
public class ThirdPersonCameraHybrid : MonoBehaviour
{
    [Header("Follow Base")]
    public Transform target;              // objetivo actual
    public float distance = 5f;
    public float height = 2f;
    [Tooltip("Tiempo de blend mientras SEGUIMOS al mismo objetivo (si no hay travel en curso)")]
    public float focusBlendTime = 0.4f;

    [Header("Orbit input")]
    public bool allowUserOrbit = true;
    public float rotationSpeed = 0.2f;
    public float minPitch = 5f;
    public float maxPitch = 60f;

    [Header("Horizontal Follow")]
    [Tooltip("Si está activo, la cámara solo sigue horizontalmente; el foco mantiene su altura.")]
    public bool followHorizontalOnly = true;

    [Header("Target Travel (cambio de objetivo)")]
    [Tooltip("Velocidad de viaje del foco al cambiar de objetivo (m/s) ")]
    public float targetTravelSpeed = 8f;
    [Tooltip("Distancia a la que consideramos que ya hemos llegado al nuevo objetivo")]
    public float targetArriveThreshold = 0.05f;

    // estado de órbita
    private float currentX = 0f;
    private float currentY = 15f;

    // estado de foco
    private Vector3 focusPoint;
    private bool isTraveling = false;

    // arrastre
    private Vector2 lastInputPos;
    private bool isDragging = false;

    public bool IsTraveling => isTraveling;

    void Awake()
    {
        if (target != null)
            focusPoint = target.position;

        // Orientación inicial desde la cámara
        var e = transform.eulerAngles;
        currentX = e.y;
        currentY = Mathf.Clamp(e.x, minPitch, maxPitch);
    }

    void Update()
    {
        if (!allowUserOrbit) return;

        if (TryBeginDrag())
        {
            lastInputPos = GetPointerPosition();
            isDragging = true;
        }
        else if (TryEndDrag())
        {
            isDragging = false;
        }

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

        // Si estamos en viaje manual hacia el nuevo objetivo, el foco lo actualiza el coroutine
        if (!isTraveling)
        {
            Vector3 desired = target.position;
            if (followHorizontalOnly)
                desired.y = focusPoint.y; // mantener altura actual del foco

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

        // Posicionar cámara
        Quaternion rot = Quaternion.Euler(currentY, currentX, 0f);
        Vector3 offset = rot * new Vector3(0f, height, -distance);
        transform.position = focusPoint + offset;
        transform.LookAt(focusPoint + Vector3.up * 1.5f);
    }

    // =============== API pública ===============

    /// <summary>
    /// Transición suave al nuevo objetivo y espera hasta llegar.
    /// Mientras dura, no se aplica el blend automático de LateUpdate.
    /// </summary>
    public IEnumerator FocusTo(Transform newTarget, float? travelSpeedOverride = null, bool? keepHorizontalOnly = null)
    {
        if (newTarget == null)
            yield break;

        // fijamos el nuevo target inmediatamente (para que el offset calcule respecto a él)
        target = newTarget;

        isTraveling = true;
        float speed = travelSpeedOverride ?? targetTravelSpeed;
        bool lockY = keepHorizontalOnly ?? followHorizontalOnly;

        while (true)
        {
            Vector3 desired = newTarget.position;
            if (lockY) desired.y = focusPoint.y; // NO saltamos en Y

            focusPoint = Vector3.MoveTowards(focusPoint, desired, speed * Time.deltaTime);
            if ((focusPoint - desired).sqrMagnitude <= targetArriveThreshold * targetArriveThreshold)
            {
                focusPoint = desired; // snap final
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

    // =============== Input helpers ===============

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
