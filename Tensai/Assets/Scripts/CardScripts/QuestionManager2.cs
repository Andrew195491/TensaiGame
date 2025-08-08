using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;



public class QuestionManager2 : MonoBehaviour
{
    public Text questionText;
    public Text scoreText;
    public Text FinalScore;

    public Button[] replyButtons;

    public QtsData qtsData; // Reference to the scriptable object

    public GameObject Right;
    public GameObject Wrong;
    public GameObject GameFinished;

    private int currentQuestion = 0;
    private static int score = 0;


    void Start()
    {
        SetQuestion(currentQuestion);
        Right.gameObject.SetActive(false);
        Wrong.gameObject.SetActive(false);
        GameFinished.gameObject.SetActive(false);
    }


    void SetQuestion(int questionIndex)
    {
        questionText.text = qtsData.questions[questionIndex].questionText;

        // Remove previous listeners before adding new ones
        foreach (Button r in replyButtons)
        {
            r.onClick.RemoveAllListeners();
        }

        for (int i = 0; i < replyButtons.Length; i++)
        {
            replyButtons[i].GetComponentInChildren<Text>().text = qtsData.questions[questionIndex].replies[i];
            int replyIndex = i;
            replyButtons[i].onClick.AddListener(() =>
            {
                CheckReply(replyIndex);
            });
        }
    }
    void CheckReply(int replyIndex)
    {
        if (replyIndex == qtsData.questions[currentQuestion].correctReplyIndex)
        {
            score++;
            scoreText.text = "" + score;

            //Eanble Right reply panel
            Right.gameObject.SetActive(true);


            //Set Active false all reply buttons
            foreach (Button r in replyButtons)
            {
                r.interactable = false;
            }

            //Next Question
            StartCoroutine(Next());
        }
        else
        {
            //Wrong reply
            Wrong.gameObject.SetActive(true);

            //Set Active false all reply buttons
            foreach (Button r in replyButtons)
            {
                r.interactable = false;
            }
            //Next Question
            StartCoroutine(Next());
        }
    }

    IEnumerator Next()
    {
        yield return new WaitForSeconds(2);
        currentQuestion++;

        if (currentQuestion < qtsData.questions.Length)
        {
            // Reset the UI and enable all reply buttons
            Reset();
        }
        else
        {
            // Game over
            GameFinished.SetActive(true);

            // Calculate the score percentage
            float scorePercentage = (float)score / qtsData.questions.Length * 100;

            // Display the score percentage
            FinalScore.text = "You scored " + scorePercentage.ToString("F0") + "%";

            // Display the appropriate message based on the score percentage
            if (scorePercentage < 50)
            {
                FinalScore.text += "\nGame Over";
            }
            else if (scorePercentage < 60)
            {
                FinalScore.text += "\nKeep Trying";
            }
            else if (scorePercentage < 70)
            {
                FinalScore.text += "\nGood Job";
            }
            else if (scorePercentage < 80)
            {
                FinalScore.text += "\nWell Done!";
            }
            else
            {
                FinalScore.text += "\nYou're a genius!";
            }
        }
    }


    public void Reset()
    {
        // Hide both the "Well done" and "Wrong" panels
        Right.SetActive(false);
        Wrong.SetActive(false);
        
        // Enable all reply buttons
        foreach (Button r in replyButtons)
        {
            r.interactable = true;
        }

        // Set the next question
        SetQuestion (currentQuestion);
    }
}