using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System;

/// <summary>
/// Controlador unificado del dado para jugador y bots.
/// Gestiona la animación de tirada, el resultado aleatorio, 
/// el movimiento del jugador y la tirada visual de los bots.
/// Compatible con sistemas de bloqueo externos (como CartaManager_U).
/// </summary>
public class DiceController_U : MonoBehaviour
{
    // ============================================
    // SECCIÓN 1: ELEMENTOS DE LA INTERFAZ
    // ============================================

    [Header("UI Jugador")]
    [Tooltip("Texto que muestra el número del dado durante y después de la tirada.")]
    public TextMeshProUGUI diceText;

    [Tooltip("Botón que el jugador presiona para tirar el dado.")]
    public Button diceButton;

    [Header("Referencia del jugador (para movimiento automático opcional)")]
    public MovePlayer_U player;

    // ============================================
    // SECCIÓN 2: CONFIGURACIÓN DEL DADO
    // ============================================

    [Header("Parámetros de tirada")]
    [Tooltip("Valor mínimo del dado.")]
    public int minNumber = 1;

    [Tooltip("Valor máximo del dado.")]
    public int maxNumber = 6;

    [Tooltip("Duración de la animación de tirada (jugador y bot)")]
    public float rollDuration = 1f;

    [Tooltip("Intervalo entre cambios de número (más pequeño = animación más rápida)")]
    public float interval = 0.05f;

    // ============================================
    // SECCIÓN 3: CONFIGURACIÓN VISUAL DEL BOT
    // ============================================

    [Header("Visual del dado del bot")]
    [Tooltip("Altura del dado flotante respecto al peón del bot.")]
    public float botDiceYOffset = 2f;

    [Tooltip("Tamaño de fuente del texto flotante 3D del dado del bot.")]
    public float botDiceFontSize = 3f;

    // ============================================
    // SECCIÓN 4: VARIABLES DE ESTADO
    // ============================================

    private bool isRolling = false;
    private bool dadoBloqueado = false;

    /// <summary>
    /// Evento disparado cuando el dado termina de girar (devuelve el número final).
    /// </summary>
    public Action<int> OnRolled;

    // ============================================
    // SECCIÓN 5: INICIALIZACIÓN
    // ============================================

    void Start()
    {
        if (diceButton != null)
            diceButton.onClick.AddListener(RollDice);
    }

    // ============================================
    // SECCIÓN 6: TIRADA DEL JUGADOR (UI)
    // ============================================

    void RollDice()
    {
        if (!isRolling && !dadoBloqueado)
            StartCoroutine(RollDiceCoroutineUI());
    }

    /// <summary>
    /// Corrutina que realiza la tirada del jugador con animación visual en la UI.
    /// </summary>
    IEnumerator RollDiceCoroutineUI()
    {
        isRolling = true;
        if (diceButton != null) diceButton.interactable = false;

        float elapsed = 0f;
        int numero = minNumber;

        // Animación: cambio rápido de números
        while (elapsed < rollDuration)
        {
            numero = UnityEngine.Random.Range(minNumber, maxNumber + 1);
            if (diceText != null) diceText.text = numero.ToString();
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        // Resultado final
        if (diceText != null) diceText.text = numero.ToString();

        // Notificar resultado
        OnRolled?.Invoke(numero);

        // Mover jugador automáticamente si está asignado
        if (player != null)
            yield return StartCoroutine(player.JumpMultipleTimes(numero));

        isRolling = false;
    }

    // ============================================
    // SECCIÓN 7: TIRADA DEL BOT (3D flotante)
    // ============================================

    /// <summary>
    /// Realiza una tirada de dado para el bot con un texto 3D flotante sobre su peón.
    /// Incluye delays opcionales antes y después de la animación.
    /// </summary>
    public IEnumerator RollForBot(Transform anchor, float preDelay, float postDelay, Action<int> onRolled)
    {
        if (anchor == null) yield break;
        if (isRolling) yield break;

        if (preDelay > 0f) yield return new WaitForSeconds(preDelay);
        isRolling = true;

        // Crear texto TMP flotante sobre el bot
        var go = new GameObject("BotDiceFloating");
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = botDiceFontSize;
        tmp.text = "-";

        var follower = go.AddComponent<FollowAnchorBillboard_U>();
        follower.target = anchor;
        follower.offset = new Vector3(0f, botDiceYOffset, 0f);

        float elapsed = 0f;
        int numero = minNumber;

        // Animación de tirada (cambio de número)
        while (elapsed < rollDuration)
        {
            numero = UnityEngine.Random.Range(minNumber, maxNumber + 1);
            tmp.text = numero.ToString();
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        tmp.text = numero.ToString();

        if (postDelay > 0f) yield return new WaitForSeconds(postDelay);

        onRolled?.Invoke(numero);

        Destroy(go);
        isRolling = false;
    }

    // ============================================
    // SECCIÓN 8: BLOQUEO DEL DADO
    // ============================================

    /// <summary>
    /// Bloquea o desbloquea el dado para prevenir tiradas.
    /// </summary>
    public void BloquearDado(bool bloquear)
    {
        dadoBloqueado = bloquear;
        if (diceButton != null)
            diceButton.interactable = !bloquear;
    }
}

// ============================================
// CLASE AUXILIAR: FOLLOW ANCHOR BILLBOARD
// ============================================

/// <summary>
/// Hace que un objeto (como el dado flotante del bot)
/// siga a un transform objetivo y mire hacia la cámara principal.
/// </summary>
public class FollowAnchorBillboard_U : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 2f, 0);
    private Camera cam;

    void Awake()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = target.position + offset;

        if (cam != null)
        {
            var dir = transform.position - cam.transform.position;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}
