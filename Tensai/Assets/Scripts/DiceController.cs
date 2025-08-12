using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using System;

public class DiceController : MonoBehaviour
{
    public TextMeshProUGUI diceText;
    public Button diceButton;

    public int minNumber = 1;
    public int maxNumber = 6;
    public float rollDuration = 1f;
    public float interval = 0.05f;

    private bool isRolling = false;
    private bool dadoBloqueado = false;

    public Action<int> OnRolled; // <- GameManager se suscribe

    void Start()
    {
        if (diceButton != null)
            diceButton.onClick.AddListener(RollDice);
    }

    void RollDice()
    {
        if (!isRolling && !dadoBloqueado)
            StartCoroutine(RollDiceCoroutine());
    }

    public void BloquearDado(bool bloquear)
    {
        dadoBloqueado = bloquear;
        if (diceButton != null) diceButton.interactable = !bloquear;
    }

    IEnumerator RollDiceCoroutine()
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

        // Avisar al GameManager (no reactivamos aquí; lo hace GM al inicio del turno del jugador)
        OnRolled?.Invoke(numero);

        isRolling = false;
    }
}
