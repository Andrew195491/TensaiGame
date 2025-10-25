using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using System;

public class DiceController2 : MonoBehaviour
{
    [Header("UI Jugador")]
    public TextMeshProUGUI diceText;
    public Button diceButton;

    [Header("Parámetros de tirada")]
    public int minNumber = 1;
    public int maxNumber = 6;
    [Tooltip("Duración de la animación de tirada (jugador y bot)")]
    public float rollDuration = 1f;
    [Tooltip("Intervalo entre cambios de número")]
    public float interval = 0.05f;

    [Header("Visual de bot")]
    [Tooltip("Altura del dado flotante respecto al bot")]
    public float botDiceYOffset = 2f;
    [Tooltip("Tamaño del texto del dado flotante (TMP 3D)")]
    public float botDiceFontSize = 3f;

    private bool isRolling = false;
    private bool dadoBloqueado = false;

    public Action<int> OnRolled; // GameManager se suscribe

    void Start()
    {
        if (diceButton != null)
            diceButton.onClick.AddListener(RollDice);
    }

    void RollDice()
    {
        if (!isRolling && !dadoBloqueado)
            StartCoroutine(RollDiceCoroutineUI());
    }

    public void BloquearDado(bool bloquear)
    {
        dadoBloqueado = bloquear;
        if (diceButton != null) diceButton.interactable = !bloquear;
    }

    // =========================
    // Jugador (UI overlay)
    // =========================
    IEnumerator RollDiceCoroutineUI()
    {
        isRolling = true;
        if (diceButton != null) diceButton.interactable = false;

        float elapsed = 0f;
        int numero = minNumber;

        while (elapsed < rollDuration)
        {
            numero = UnityEngine.Random.Range(minNumber, maxNumber + 1);
            if (diceText != null) diceText.text = numero.ToString();
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        if (diceText != null) diceText.text = numero.ToString();

        OnRolled?.Invoke(numero);

        isRolling = false;
    }

    // =========================
    // Bot (dado flotante sobre el peón)
    // =========================

    /// <summary>
    /// Realiza una tirada para el BOT con delays y dado flotante sobre el peón.
    /// Llama a OnRolled al terminar.
    /// </summary>
    public IEnumerator RollForBot(Transform anchor, float preDelay, float postDelay, Action<int> onRolled)
    {
        if (anchor == null) yield break;
        if (isRolling) yield break; // evitar reentradas

        // 1) delay previo
        if (preDelay > 0f) yield return new WaitForSeconds(preDelay);

        isRolling = true;

        // 2) crear objeto TMP 3D flotante
        var go = new GameObject("BotDiceFloating");
        var tmp = go.AddComponent<TextMeshPro>(); // TextMeshPro 3D (no UGUI)
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = botDiceFontSize;
        tmp.text = "-";

        var follower = go.AddComponent<FollowAnchorBillboard>();
        follower.target = anchor;
        follower.offset = new Vector3(0f, botDiceYOffset, 0f);

        // 3) animación de tirada (igual que UI, pero sobre el bot)
        float elapsed = 0f;
        int numero = minNumber;
        while (elapsed < rollDuration)
        {
            numero = UnityEngine.Random.Range(minNumber, maxNumber + 1);
            tmp.text = numero.ToString();
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
        tmp.text = numero.ToString();

        // 4) pequeño delay tras parar
        if (postDelay > 0f) yield return new WaitForSeconds(postDelay);

        // 5) notificar resultado y limpiar
        onRolled?.Invoke(numero);

        Destroy(go);
        isRolling = false;
    }
}

/// <summary>
/// Hace que el objeto siga a un transform y mire a la cámara (billboard).
/// Útil para el dado flotante del bot.
/// </summary>
public class FollowAnchorBillboard : MonoBehaviour
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
            // Mira a la cámara (billboard)
            var dir = transform.position - cam.transform.position;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}
