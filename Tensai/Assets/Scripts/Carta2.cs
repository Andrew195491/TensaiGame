using UnityEngine;

[System.Serializable]
public class Carta2
{
    public string pregunta;
    public string respuesta1;
    public string respuesta2;
    public string respuesta3;
    [Range(1, 3)] public int respuestaCorrecta; // Indica cuál es la correcta (1, 2 o 3)
}
