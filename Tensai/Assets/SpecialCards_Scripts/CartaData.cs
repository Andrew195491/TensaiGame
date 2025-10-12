using System.Collections.Generic;

// Clase que representa el wrapper del JSON con el array "Cards"
[System.Serializable]
public class CartasEspecialesRoot
{
    public List<CartaData> Cards;
}

// Esta clase representa cada elemento dentro del array "Cards"
[System.Serializable]
public class CartaData
{
    public List<Carta> benefits;
    public List<Carta> penalty;
}