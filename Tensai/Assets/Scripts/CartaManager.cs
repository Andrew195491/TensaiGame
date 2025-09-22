using UnityEngine;
using System.Collections.Generic;

public class CartaManager : MonoBehaviour
{
    public List<Carta> historia;
    public List<Carta> geografia;
    public List<Carta> ciencia;

    // Cartas especiales
    //public List<Carta> benefits;
    //public List<Carta> penalty;

    public CartaUI cartaUI;

    public static CartaManager instancia;
    public DiceController dadoController; // Arrástralo desde el inspector


    // Lista de cartas especiales
    [Header("Cartas especiales - Beneficios")]
    public List<Carta> benefits = new List<Carta>
    {
        new Carta { pregunta = "Casilla de beneficio: Avanzas 2 casillas.", accion = "Avanza2" },
        new Carta { pregunta = "Casilla de beneficio: Repite turno.", accion = "RepiteTurno" },
        new Carta { pregunta = "Casilla de beneficio: Intercambia posición con otro jugador.", accion = "Intercambia" },
        // ... hasta 10 cartas
    };


    [Header("Cartas especiales - Penalidades")]
    public List<Carta> penalty = new List<Carta>
    {
        new Carta { pregunta = "Casilla de penalidad: Retrocedes 3 casillas.", accion = "Retrocede3" },
        new Carta { pregunta = "Casilla de penalidad: Pierdes el siguiente turno.", accion = "PierdeTurno" },
        new Carta { pregunta = "Casilla de penalidad: Regresas a la salida más cercana.", accion = "IrSalida" },
        // ... hasta 10 cartas
    };


    // Almacenamiento de cartas
    [Header("Almacenamiento de cartas especiales")]
    public Carta[] storage = new Carta[3];



    void Awake()
    {
        instancia = this;
    }


    public bool AgregarCartaAlStorage(Carta carta)
    {
        for (int i = 0; i < storage.Length; i++)
        {
            if (storage[i] == null) // Espacio vacío
            {
                storage[i] = carta;
                Debug.Log($"✅ Carta agregada al storage en posición {i}: {carta.pregunta}");
                return true;
            }
        }

        Debug.Log("⚠️ Storage lleno, debes usar una carta antes de agregar otra.");
        return false; // No se pudo guardar
    }


    public void UsarCartaDelStorage(int index, MovePlayer jugador)
    {
        if (index < 0 || index >= storage.Length || storage[index] == null)
        {
            Debug.Log("❌ No hay carta en esa posición.");
            return;
        }

        Carta carta = storage[index];

        if (benefits.Contains(carta))
        {
            EjecutarBeneficio(carta, jugador);
        }
        else if (penalty.Contains(carta))
        {
            EjecutarPenalidad(carta, jugador);
        }

        storage[index] = null; // Liberamos el slot
    }





    // ✅ ESTE MÉTODO DEBE EXISTIR
    private Carta ObtenerCartaAleatoria(Tile.Categoria categoria)
    {
        List<Carta> lista = categoria switch
        {
            Tile.Categoria.Historia => historia,
            Tile.Categoria.Geografia => geografia,
            Tile.Categoria.Ciencia => ciencia,
            Tile.Categoria.neutral => null, // No hay cartas para neutral
            Tile.Categoria.Benefits => benefits,
            Tile.Categoria.Penalty => penalty,
            _ => null
        };

        if (lista == null || lista.Count == 0)
            return null;

        int index = Random.Range(0, lista.Count);
        return lista[index];
    }


    // Ejecucion de cartas especiales
    public void EjecutarAccionEspecial(Tile.Categoria categoria, MovePlayer jugador)
    {
        if (cartaUI == null) return;
        Carta cartaSeleccionada = null;


        if (categoria == Tile.Categoria.Penalty)
        {
            cartaSeleccionada = ObtenerCartaPenalidadAleatoria();
            if (cartaSeleccionada != null)
            {
     
            }
        }

        switch (categoria)
        {
            // casos de casillas neutrales
            case Tile.Categoria.neutral:
                cartaUI.MostrarMensajeEspecial("Casilla neutral: No pasa nada.", () =>
                {
                    Debug.Log("Neutral: turno terminado");
                });
                break;

            // casos de casillas de beneficios
            /**/
            case Tile.Categoria.Benefits:
                //if (categoria == Tile.Categoria.Benefits)
                //{
                    cartaSeleccionada = ObtenerCartaBeneficioAleatoria();
                    if (cartaSeleccionada != null)
                    {
                        Debug.Log("test carta beneficio");
                        if (cartaSeleccionada.accion == "Avanza2")
                    {
                        cartaUI.MostrarMensajeEspecial(cartaSeleccionada.pregunta, () =>
                        {
                            jugador.StartCoroutine(jugador.JumpMultipleTimes(2));
                            Debug.Log("Casilla de beneficio: Avanzas 2 casillas.");

                        });
                    }
                    else if (!AgregarCartaAlStorage(cartaSeleccionada))
                    {
                        Debug.Log("⚠️ Storage lleno, debes usar una carta antes.");
                    }
                    }
                //}
                /*cartaUI.MostrarMensajeEspecial("Casilla de Beneficios: Avança 2 casillas.", () =>
                {
                    jugador.StartCoroutine(jugador.JumpMultipleTimes(2));
                    Debug.Log("Casilla de beneficio: Avanzas 2 casillas.");
                });
                */
            break;


            // casos de casillas de penalidad
            case Tile.Categoria.Penalty:
                cartaUI.MostrarMensajeEspecial("Casilla de penalidad: Retrocedes 3 casillas.", () =>
                {
                    jugador.StartCoroutine(jugador.Retroceder(3));
                    Debug.Log("Casilla de penalidad: Retrocedes 3 casillas.");
                });
                break;
                /**/

        }
    }
    /**/


        // ✅ AQUÍ LO USAMOS
    public void MostrarCarta(Tile.Categoria categoria, System.Action onRespuestaIncorrecta = null)
    {
        Carta carta = ObtenerCartaAleatoria(categoria);

        if (carta != null && cartaUI != null)
        {
            // Bloquear dado mientras se responde
            if (dadoController != null)
                dadoController.BloquearDado(true);

            cartaUI.MostrarCarta(carta, (int respuestaSeleccionada) =>
            {
                bool esCorrecta = respuestaSeleccionada == carta.respuestaCorrecta;
                Debug.Log(esCorrecta ? "✅ Respuesta correcta" : "❌ Respuesta incorrecta");

                if (!esCorrecta && onRespuestaIncorrecta != null)
                    onRespuestaIncorrecta.Invoke();

                // ✅ Activar dado después de responder
                if (dadoController != null)
                    dadoController.BloquearDado(false);
            });
        }
        else
        {
            Debug.LogWarning($"No hay carta o UI para la categoría {categoria}");
        }
    }


    public Carta ObtenerCartaBeneficioAleatoria()
    {
        if (benefits.Count == 0) return null;
        int index = Random.Range(0, benefits.Count);
        return benefits[index];
    }



    public Carta ObtenerCartaPenalidadAleatoria()
    {
        if (penalty.Count == 0) return null;
        int index = Random.Range(0, penalty.Count);
        return penalty[index];
    }




    public void EjecutarBeneficio(Carta carta, MovePlayer jugador)
    {
        if (carta == null) return;

        // Mostramos el mensaje en cartaUI
        cartaUI.MostrarMensajeEspecial(carta.pregunta, () =>
        {
            if (carta.pregunta.Contains("Avanzas 1"))
            {
                jugador.StartCoroutine(jugador.JumpMultipleTimes(1));
                Debug.Log("✅ Avanzas 1 casilla.");
            }
            else if (carta.pregunta.Contains("Avanzas 2"))
            {
                jugador.StartCoroutine(jugador.JumpMultipleTimes(2));
                Debug.Log("✅ Avanzas 2 casillas.");
            }
            else if (carta.pregunta.Contains("Repite turno"))
            {
                // Aquí tendrías que programar tu lógica para repetir turno
                Debug.Log("🔄 Repite turno.");
            }
            else if (carta.pregunta.Contains("Intercambia"))
            {
                // Aquí lógica de intercambio con otro jugador
                Debug.Log("🔁 Intercambia posición con otro jugador.");
            }
        });
    }

    public void EjecutarPenalidad(Carta carta, MovePlayer jugador)
    {
        if (carta == null) return;

        cartaUI.MostrarMensajeEspecial(carta.pregunta, () =>
        {
            if (carta.pregunta.Contains("Retrocedes 1"))
            {
                jugador.StartCoroutine(jugador.Retroceder(1));
                Debug.Log("⚠️ Retrocedes 1 casilla.");
            }
            else if (carta.pregunta.Contains("Retrocedes 3"))
            {
                jugador.StartCoroutine(jugador.Retroceder(3));
                Debug.Log("⚠️ Retrocedes 3 casillas.");
            }
            else if (carta.pregunta.Contains("Pierdes el siguiente turno"))
            {
                // Aquí tu lógica para bloquear el dado un turno
                Debug.Log("⛔ Pierdes el siguiente turno.");
            }
            else if (carta.pregunta.Contains("IrSalida"))
            {
                // Aquí lógica de moverte a la salida más cercana
                Debug.Log("⬅️ Vas a la salida más cercana.");
            }
        });
    }




}
