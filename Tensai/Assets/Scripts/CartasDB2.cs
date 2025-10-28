// ============================================
// CartasDB2.cs
// ============================================
using System;
using System.Collections.Generic;

[Serializable]
public class CartasDB2
{
    public List<Carta2> historia;
    public List<Carta2> geografia;
    public List<Carta2> ciencia;
    public List<CartaEntry2> beneficios;
    public List<CartaEntry2> penalidades;
}

