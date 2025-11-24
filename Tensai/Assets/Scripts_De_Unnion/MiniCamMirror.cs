using UnityEngine;

public class MiniCamMirror : MonoBehaviour
{
    [Tooltip("Cámara principal que se va a clonar.")]
    public Camera mainCam;

    [Tooltip("Copiar también FOV y planos de recorte.")]
    public bool copyProjection = true;

    void LateUpdate()
    {
        if (mainCam == null) return;

        // Copia transform (posición y rotación) 1:1
        transform.SetPositionAndRotation(mainCam.transform.position, mainCam.transform.rotation);

        if (copyProjection)
        {
            var mini = GetComponent<Camera>();
            if (mini != null)
            {
                mini.fieldOfView      = mainCam.fieldOfView;
                mini.nearClipPlane    = mainCam.nearClipPlane;
                mini.farClipPlane     = mainCam.farClipPlane;
                mini.orthographic     = mainCam.orthographic;
                mini.orthographicSize = mainCam.orthographicSize;
            }
        }
    }
}
