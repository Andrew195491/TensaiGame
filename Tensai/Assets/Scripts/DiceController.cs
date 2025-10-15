using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class DiceController : MonoBehaviour
{
    public TextMeshProUGUI diceText;
    public Button diceButton;
    public MovePlayer player;

    public int minNumber = 1;
    public int maxNumber = 6;
    public float rollDuration = 1f;
    public float interval = 0.05f;

    private bool isRolling = false;
    private bool dadoBloqueado = false;

    void Start()
    {
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
        diceButton.interactable = !bloquear;
    }

    IEnumerator RollDiceCoroutine()
    {
        isRolling = true;
        diceButton.interactable = false;

        float elapsed = 0f;
        int numero = 1;

        while (elapsed < rollDuration)
        {
            numero = Random.Range(minNumber, maxNumber + 1);
            diceText.text = numero.ToString();
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        diceText.text = numero.ToString();
        yield return StartCoroutine(player.JumpMultipleTimes(numero));

        isRolling = false;
    }
}
