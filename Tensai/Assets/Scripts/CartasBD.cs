using System;
using System.Collections.Generic;

[Serializable]
public class CartasDB
{
    // Preguntas por categoría
    public List<Carta> historia;
    public List<Carta> geografia;
    public List<Carta> ciencia;

    // NUEVO: listas para efectos con repetición
    public List<CartaEntry> beneficios;
    public List<CartaEntry> penalidades;
}
