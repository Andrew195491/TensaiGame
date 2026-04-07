using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HandIndicator_U : MonoBehaviour
{
    [Header("Referencias")]
    public Image handImage; // Asigna una imagen de mano en el Inspector
    public RectTransform targetButton; // El botón del dado
    
    [Header("Configuración")]
    public float delayBeforeShow = 5f;
    public float animationSpeed = 1f;
    public float moveDistance = 20f;
    
    private Coroutine currentAnimation;
    
    void Start()
    {
        if (handImage) handImage.gameObject.SetActive(false);
    }
    
    public void ShowHandIndicator(RectTransform target)
    {
        if (currentAnimation != null) StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(ShowHandCoroutine(target));
    }
    
    public void HideHandIndicator()
    {
        if (currentAnimation != null) StopCoroutine(currentAnimation);
        if (handImage) handImage.gameObject.SetActive(false);
    }
    
    private IEnumerator ShowHandCoroutine(RectTransform target)
    {
        Debug.Log("🖐 Hand coroutine START");   // <-- Aquí sí

        yield return new WaitForSeconds(delayBeforeShow);
            
        if (handImage && target)
        {
            handImage.gameObject.SetActive(true);
            handImage.rectTransform.position = target.position;

            // Animación de pulsación
            while (true)
            {
                float t = Mathf.PingPong(Time.time * animationSpeed, 1f);
                Vector3 offset = new Vector3(0, -moveDistance * t, 0);
                handImage.rectTransform.position = target.position + offset;

                Debug.Log("🖐 Hand moving");  // <-- Aquí también está OK

                yield return null;
            }
        }
    }
}