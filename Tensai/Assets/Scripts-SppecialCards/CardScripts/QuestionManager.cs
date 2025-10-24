using UnityEngine;
using System.Collections.Generic;

public class QuestionManager : MonoBehaviour
{
    public List<Question> questions;
    public QuestionUI questionUI;
}
    /*
        void Start()
        {
            LoadQuestions();
            DisplayRandomQuestion();
        }

        void LoadQuestions()
        {
            TextAsset jsonFile = Resources.Load<TextAsset>("Questions&Answers"); // sin extensión .json
            if (jsonFile != null)
            {
                QuestionList questionList = JsonUtility.FromJson<QuestionList>(jsonFile.text);
                questions = new List<Question>(questionList.questions);
            }
            else
            {
                Debug.LogError("No se encontró el archivo Questions&Answers.json en Resources.");
            }
        }

        void DisplayRandomQuestion()
        {
            if (questions == null || questions.Count == 0)
            {
                Debug.LogWarning("No hay preguntas cargadas.");
                return;
            }

            int randomIndex = Random.Range(0, questions.Count);
            Question randomQuestion = questions[randomIndex];

            questionUI.ShowQuestion(randomQuestion);

            /*
            Debug.Log("Pregunta: " + randomQuestion.question);
            for (int i = 0; i < randomQuestion.answers.Length; i++)
            {
                Debug.Log((i + 1) + ": " + randomQuestion.answers[i]);
            }*
            /*
}
}
*/