using System;

[Serializable]
public class Question
{
    public int id;
    public string question;
    public string[] answers;
    public int correctIndex;
}

[Serializable]
public class QuestionList
{
    public Question[] questions;
}
