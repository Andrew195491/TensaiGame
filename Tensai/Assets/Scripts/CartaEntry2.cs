using System;

public enum TipoCarta { Pregunta, Efecto }
public enum TipoEfecto { MoverRelativo, RepetirTirada, SaltarTurnos /* + añade más si quieres */ }

[Serializable]
public class CartaEntry2
{
    public TipoCarta tipo = TipoCarta.Efecto;

    public string nombre;       // título visible de la carta de efecto
    public string descripcion;  // ⬅️ NUEVO: texto que mostraremos en la UI

    public TipoEfecto efecto;
    public int pasos;
    public int turnos;
}

