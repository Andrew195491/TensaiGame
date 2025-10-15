using UnityEngine;
using UnityEngine.EventSystems;

public class ThirdPersonCameraHybrid : MonoBehaviour
{
    public Transform target;              // El objeto a seguir (ej: Player)
    public float distance = 5f;           // Distancia de la cámara al jugador
    public float height = 2f;             // Altura de la cámara
    public float rotationSpeed = 0.2f;    // Velocidad de rotación

    private float currentX = 0f;
    private float currentY = 15f;

    private Vector2 lastInputPos;
    private bool isDragging = false;

    public static bool IsCameraDragging { get; private set; } = false;

    void Update()
    {
        // 📱 Touch en móvil
        if (Input.touchCount == 1 && !IsPointerOverUI())
        {
            Touch touch = Input.GetTouch(0);
            Vector2 delta = touch.deltaPosition;

            if (touch.phase == TouchPhase.Began)
            {
                IsCameraDragging = true;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                IsCameraDragging = false;
            }

            if (touch.phase == TouchPhase.Moved)
            {
                currentX += delta.x * rotationSpeed;
                currentY -= delta.y * rotationSpeed;
                currentY = Mathf.Clamp(currentY, 5f, 60f);
            }
        }

        // 🖱 Mouse en PC
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
        {
            isDragging = true;
            IsCameraDragging = true;
            lastInputPos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            IsCameraDragging = false;
        }

        if (isDragging)
        {
            Vector2 delta = (Vector2)Input.mousePosition - lastInputPos;
            currentX += delta.x * rotationSpeed;
            currentY -= delta.y * rotationSpeed;
            currentY = Mathf.Clamp(currentY, 5f, 60f);
            lastInputPos = Input.mousePosition;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 direction = new Vector3(0, height, -distance);
        transform.position = target.position + rotation * direction;
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
