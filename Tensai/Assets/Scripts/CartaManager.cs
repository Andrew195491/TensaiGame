using UnityEngine;
using System.Collections.Generic;

public class CartaManager : MonoBehaviour
{
    [Header("Cartas de preguntas")]
    public List<Carta> historia;
    public List<Carta> geografia;
    public List<Carta> ciencia;

    [Header("Referencias UI")]
    public CartaUI cartaUI;
    public DiceController dadoController;
    public BonusUI bonusUI; // Para mostrar cartas almacenadas

    public static CartaManager instancia;

    [Header("Cartas especiales - Beneficios")]
    public List<Carta> benefits = new List<Carta>();

    [Header("Cartas especiales - Penalidades")]
    public List<Carta> penalty = new List<Carta>();

    [Header("Almacenamiento de cartas especiales")]
    public Carta[] storage = new Carta[2];

    void Awake()
    {
        instancia = this;
        InicializarCartasEspeciales();
    }

    void Start()
    {
        // Actualizar UI del storage al inicio
        Debug.Log(storage.Length);
        ActualizarUIStorage();
        
    }

    void InicializarCartasEspeciales()
    {
        // Inicializar cartas de beneficio si la lista está vacía
        if (benefits.Count == 0)
        {
            benefits.AddRange(new List<Carta>
            {
                new Carta { pregunta = "¡Beneficio! Avanzas 1 casilla extra", accion = "Avanza1" },
                new Carta { pregunta = "¡Beneficio! Avanzas 2 casillas extra", accion = "Avanza2" },
                new Carta { pregunta = "¡Beneficio! Repites tu turno", accion = "RepiteTurno" },
                new Carta { pregunta = "¡Beneficio! Intercambias posición con otro jugador", accion = "Intercambia" },
                new Carta { pregunta = "¡Beneficio! Avanzas 3 casillas extra", accion = "Avanza3" },
                new Carta { pregunta = "¡Beneficio! Inmune a penalidades por 1 turno", accion = "Inmunidad" },
                new Carta { pregunta = "¡Beneficio! Doble dado en próximo turno", accion = "DobleDado" },
                new Carta { pregunta = "¡Beneficio! Teletransporte a casilla aleatoria adelante", accion = "TeletransporteAdelante" },
                new Carta { pregunta = "¡Beneficio! Eliges el resultado del próximo dado", accion = "ElegirDado" },
                new Carta { pregunta = "¡Beneficio! Robas una carta especial de otro jugador", accion = "RobarCarta" }
            });
        }

        // Inicializar cartas de penalidad si la lista está vacía
        if (penalty.Count == 0)
        {
            penalty.AddRange(new List<Carta>
            {
                new Carta { pregunta = "¡Penalidad! Retrocedes 1 casilla", accion = "Retrocede1" },
                new Carta { pregunta = "¡Penalidad! Retrocedes 2 casillas", accion = "Retrocede2" },
                new Carta { pregunta = "¡Penalidad! Retrocedes 3 casillas", accion = "Retrocede3" },
                new Carta { pregunta = "¡Penalidad! Pierdes el siguiente turno", accion = "PierdeTurno" },
                new Carta { pregunta = "¡Penalidad! Regresas a la casilla de salida", accion = "IrSalida" },
                new Carta { pregunta = "¡Penalidad! Intercambias posición con el último jugador", accion = "IntercambiaUltimo" },
                new Carta { pregunta = "¡Penalidad! Pierdes todas tus cartas especiales", accion = "PerderCartas" },
                new Carta { pregunta = "¡Penalidad! Dados bloqueados por 2 turnos", accion = "BloquearDados" },
                new Carta { pregunta = "¡Penalidad! Teletransporte a casilla aleatoria atrás", accion = "TeletransporteAtras" },
                new Carta { pregunta = "¡Penalidad! Solo puedes moverte 1 casilla por 3 turnos", accion = "MovimientoLimitado" }
            });
        }
    }

    // ✅ Método principal para manejar casillas especiales
    public void EjecutarAccionEspecial(Tile.Categoria categoria, MovePlayer jugador)
    {
        if (cartaUI == null) return;

        switch (categoria)
        {
            case Tile.Categoria.neutral:
                ManejarCasillaNeutral(jugador);
                break;

            case Tile.Categoria.Benefits:
                ManejarCasillaBeneficios(jugador);
                break;

            case Tile.Categoria.Penalty:
                ManejarCasillaPenalidad(jugador);
                break;
        }
    }

    private void ManejarCasillaNeutral(MovePlayer jugador)
    {
        cartaUI.MostrarMensajeEspecial("Casilla Neutral: ¡Descansas un momento! No pasa nada.", () =>
        {
            Debug.Log("💤 Casilla neutral: El jugador descansa");
            // Reactivar el dado para el siguiente turno
            if (dadoController != null)
                dadoController.BloquearDado(false);
        });
    }

    // ✅ MODIFICADO: Mostrar decisión para beneficios
    private void ManejarCasillaBeneficios(MovePlayer jugador)
    {
        Carta cartaBeneficio = ObtenerCartaBeneficioAleatoria();
        if (cartaBeneficio != null)
        {
            // Mostrar panel de decisión: almacenar o usar inmediatamente
            cartaUI.MostrarDecisionBeneficio(cartaBeneficio, jugador, () =>
            {
                // Callback cuando se toma la decisión
                if (dadoController != null)
                    dadoController.BloquearDado(false);
            });
        }
    }

    private void ManejarCasillaPenalidad(MovePlayer jugador)
    {
        Carta cartaPenalidad = ObtenerCartaPenalidadAleatoria();
        if (cartaPenalidad != null)
        {
            // Las penalidades se aplican inmediatamente
            cartaUI.MostrarMensajeEspecial($"⚡ ¡Casilla de penalidad!\n{cartaPenalidad.pregunta}", () =>
            {
                EjecutarPenalidad(cartaPenalidad, jugador);
            });
        }
    }

    // ✅ Método para mostrar cartas normales (con preguntas)
    public void MostrarCarta(Tile.Categoria categoria, System.Action onRespuestaIncorrecta = null)
    {
        Carta carta = ObtenerCartaAleatoria(categoria);

        if (carta != null && cartaUI != null)
        {
            if (dadoController != null)
                dadoController.BloquearDado(true);

            cartaUI.MostrarCarta(carta, (int respuestaSeleccionada) =>
            {
                bool esCorrecta = respuestaSeleccionada == carta.respuestaCorrecta;
                Debug.Log(esCorrecta ? "✅ Respuesta correcta" : "❌ Respuesta incorrecta");

                if (!esCorrecta && onRespuestaIncorrecta != null)
                    onRespuestaIncorrecta.Invoke();

                if (dadoController != null)
                    dadoController.BloquearDado(false);
            });
        }
    }

    private Carta ObtenerCartaAleatoria(Tile.Categoria categoria)
    {
        List<Carta> lista = categoria switch
        {
            Tile.Categoria.Historia => historia,
            Tile.Categoria.Geografia => geografia,
            Tile.Categoria.Ciencia => ciencia,
            Tile.Categoria.Benefits => benefits,
            Tile.Categoria.Penalty => penalty,
            _ => null
        };

        if (lista == null || lista.Count == 0) return null;

        int index = Random.Range(0, lista.Count);
        return lista[index];
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

    // ✅ Sistema de almacenamiento mejorado
    public bool AgregarCartaAlStorage(Carta carta)
    {
        for (int i = 0; i < storage.Length; i++)
        {
            if (storage[i] == null)
            {
                storage[i] = carta;
                Debug.Log($"✅ Carta agregada al storage en posición {i}: {carta.pregunta}");
                ActualizarUIStorage(); // ✅ Actualizar UI automáticamente
                return true;
            }
        }

        Debug.Log("⚠️ Storage lleno, no se puede agregar la carta.");
        return false;
    }

    public void UsarCartaDelStorage(int index, MovePlayer jugador)
    {
        if (index < 0 || index >= storage.Length || storage[index] == null)
        {
            Debug.Log("❌ No hay carta en esa posición.");
            return;
        }

        Carta carta = storage[index];
        
        // Determinar si es beneficio o penalidad y ejecutar
        if (benefits.Contains(carta))
        {
            EjecutarBeneficio(carta, jugador);
        }
        else if (penalty.Contains(carta))
        {
            EjecutarPenalidad(carta, jugador);
        }

        // Liberar el slot
        storage[index] = null;
        ActualizarUIStorage();
        
        Debug.Log($"🎯 Carta usada: {carta.pregunta}");
    }

    // ✅ MÉTODO PÚBLICO para actualizar UI
    public void ActualizarUIStorage()
    {
        if (bonusUI != null)
        {
            List<Carta> cartasParaMostrar = new List<Carta>();
            foreach (var carta in storage)
            {
                if (carta != null) cartasParaMostrar.Add(carta);
            }
            bonusUI.ActualizarUI(cartasParaMostrar);
        }
    }

    // ✅ Ejecución de efectos de beneficios
    public void EjecutarBeneficio(Carta carta, MovePlayer jugador)
    {
        if (carta == null || jugador == null) return;

        Debug.Log($"🎉 Ejecutando beneficio: {carta.accion}");

        switch (carta.accion)
        {
            case "Avanza1":
                jugador.StartCoroutine(jugador.JumpMultipleTimes(1));
                break;
            case "Avanza2":
                jugador.StartCoroutine(jugador.JumpMultipleTimes(2));
                break;
            case "Avanza3":
                jugador.StartCoroutine(jugador.JumpMultipleTimes(3));
                break;
            case "RepiteTurno":
                // Permitir otro turno inmediatamente
                if (dadoController != null)
                    dadoController.BloquearDado(false);
                Debug.Log("🔄 ¡Repites turno!");
                break;
            case "Intercambia":
                Debug.Log("🔄 Intercambia posición con otro jugador (implementar lógica multijugador)");
                break;
            case "Inmunidad":
                // Implementar sistema de inmunidad
                Debug.Log("🛡️ Inmune a penalidades por 1 turno");
                break;
            case "DobleDado":
                Debug.Log("🎲🎲 Doble dado en próximo turno");
                break;
            case "TeletransporteAdelante":
                int saltoAdelante = Random.Range(5, 10);
                jugador.StartCoroutine(jugador.JumpMultipleTimes(saltoAdelante));
                Debug.Log($"🌀 Teletransporte {saltoAdelante} casillas adelante");
                break;
            case "ElegirDado":
                Debug.Log("🎯 Puedes elegir el resultado del próximo dado");
                break;
            case "RobarCarta":
                Debug.Log("💰 Robas una carta especial de otro jugador");
                break;
            default:
                Debug.Log($"⚠️ Acción de beneficio no reconocida: {carta.accion}");
                break;
        }
    }

    // ✅ Ejecución de efectos de penalidades
    public void EjecutarPenalidad(Carta carta, MovePlayer jugador)
    {
        if (carta == null || jugador == null) return;

        Debug.Log($"⚡ Ejecutando penalidad: {carta.accion}");

        switch (carta.accion)
        {
            case "Retrocede1":
                jugador.StartCoroutine(jugador.Retroceder(1));
                break;
            case "Retrocede2":
                jugador.StartCoroutine(jugador.Retroceder(2));
                break;
            case "Retrocede3":
                jugador.StartCoroutine(jugador.Retroceder(3));
                break;
            case "PierdeTurno":
                if (dadoController != null)
                {
                    // Bloquear dado por un turno extra
                    dadoController.BloquearDado(true);
                    Debug.Log("⏸️ Dado bloqueado - pierdes el siguiente turno");
                }
                break;
            case "IrSalida":
                // Mover al jugador a la casilla 0
                jugador.StartCoroutine(jugador.IrACasilla(0));
                Debug.Log("🏠 Regresando a la salida");
                break;
            case "IntercambiaUltimo":
                Debug.Log("🔄 Intercambias posición con el último jugador");
                break;
            case "PerderCartas":
                // Limpiar storage
                for (int i = 0; i < storage.Length; i++)
                {
                    storage[i] = null;
                }
                ActualizarUIStorage();
                Debug.Log("💸 Pierdes todas tus cartas especiales");
                break;
            case "BloquearDados":
                if (dadoController != null)
                {
                    dadoController.BloquearDado(true);
                    Debug.Log("🔒 Dados bloqueados por 2 turnos");
                }
                break;
            case "TeletransporteAtras":
                int saltoAtras = Random.Range(3, 8);
                jugador.StartCoroutine(jugador.Retroceder(saltoAtras));
                Debug.Log($"🌀 Teletransporte {saltoAtras} casillas atrás");
                break;
            case "MovimientoLimitado":
                Debug.Log("🐌 Solo puedes moverte 1 casilla por 3 turnos");
                break;
            default:
                Debug.Log($"⚠️ Acción de penalidad no reconocida: {carta.accion}");
                break;
        }
    }
}