using UnityEngine;
using UnityEngine.UI;
using TMPro;



public class QuestionUI : MonoBehaviour
{
    public TextMeshProUGUI questionText;
    public Button[] answerButtons;

    public void ShowQuestion(Question question)
    {
        questionText.text = question.question;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i < question.answers.Length)
            {
                answerButtons[i].gameObject.SetActive(true);
                answerButtons[i].GetComponentInChildren<Text>().text = question.answers[i];
            }
            else
            {
                answerButtons[i].gameObject.SetActive(false);
            }
        }
    }
}
