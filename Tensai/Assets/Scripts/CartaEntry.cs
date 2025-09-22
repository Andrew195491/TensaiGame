using System;

public enum TipoCarta { Pregunta, Efecto }
public enum TipoEfecto { MoverRelativo, RepetirTirada, SaltarTurnos /* + añade más si quieres */ }

[Serializable]
public class CartaEntry
{
    public TipoCarta tipo = TipoCarta.Efecto;

    // (opcional) nombre visible
    public string nombre;

    // Si es efecto:
    public TipoEfecto efecto;
    public int pasos;   // para MoverRelativo (+/-)
    public int turnos;  // para SaltarTurnos
}
