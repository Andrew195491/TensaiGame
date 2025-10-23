using UnityEngine; // Necesario para MonoBehaviour
using System.Collections.Generic; // Necesario para List<T>

/// <summary>
/// Gestor central de todas las cartas del juego.
/// Maneja las cartas de preguntas, cartas especiales, el inventario del jugador,
/// y ejecuta los efectos de beneficios y penalidades.
/// Implementa patrón Singleton para acceso global.
/// </summary>
public class CartaManager : MonoBehaviour
{
    // ============================================
    // SECCIÓN 1: CARTAS DE PREGUNTAS POR CATEGORÍA
    // ============================================
    
    [Header("Cartas de preguntas")]
    // Listas de cartas de trivia organizadas por temática
    public List<Carta> historia;    // Preguntas de historia
    public List<Carta> geografia;   // Preguntas de geografía
    public List<Carta> ciencia;     // Preguntas de ciencia

    // ============================================
    // SECCIÓN 2: REFERENCIAS A OTROS SISTEMAS
    // ============================================
    
    [Header("Referencias UI")]
    // Interfaz que muestra las cartas y preguntas al jugador
    public CartaUI cartaUI;
    // Controlador del dado para bloquear/desbloquear tiradas
    public DiceController dadoController;
    // Interfaz que muestra el inventario de cartas especiales
    public BonusUI bonusUI;
    // Interfaz para reemplazar cartas cuando el inventario está lleno
    public ReplacementUI replacementUI;

    // ============================================
    // SECCIÓN 3: CARTAS ESPECIALES DESDE JSON
    // ============================================
    
    [Header("Archivo JSON de Cartas Especiales")]
    // Archivo de texto que contiene las cartas especiales en formato JSON
    public TextAsset cartasEspecialesJSON;

    // Instancia singleton para acceso global desde otros scripts
    public static CartaManager instancia;

    [Header("Cartas especiales - Beneficios")]
    // Lista de cartas con efectos positivos cargadas desde el JSON
    public List<Carta> benefits = new List<Carta>();

    [Header("Cartas especiales - Penalidades")]
    // Lista de cartas con efectos negativos cargadas desde el JSON
    public List<Carta> penalty = new List<Carta>();

    // ============================================
    // SECCIÓN 4: SISTEMA DE INVENTARIO
    // ============================================
    
    [Header("Almacenamiento de cartas especiales")]
    // Inventario del jugador: cartas especiales guardadas para usar después
    public List<Carta> storage = new List<Carta>();
    // Capacidad máxima del inventario (3 cartas)
    public int maxStorage = 3;

    // ============================================
    // SECCIÓN 5: INICIALIZACIÓN
    // ============================================
    
    void Awake()
    {
        // Establecer como instancia singleton
        instancia = this;
        // Cargar las cartas especiales desde el archivo JSON
        CargarCartasDesdeJSON();
    }

    void Start()
    {
        // Limpiar el inventario al iniciar una nueva partida
        storage.Clear();
        Debug.Log($"Storage inicializado vacío. Count: {storage.Count}/{maxStorage}");
        // Actualizar la UI del inventario
        ActualizarUIStorage();
    }

    // ============================================
    // SECCIÓN 6: CARGA DE CARTAS DESDE JSON
    // ============================================
    
    /// <summary>
    /// Carga las cartas especiales desde el archivo JSON asignado en el Inspector.
    /// Parsea el JSON y divide las cartas en listas de beneficios y penalidades.
    /// </summary>
    void CargarCartasDesdeJSON()
    {
        // Verificar que el archivo JSON está asignado
        if (cartasEspecialesJSON == null)
        {
            Debug.LogError("❌ CRÍTICO: No se ha asignado el archivo JSON de cartas especiales en el Inspector!");
            Debug.LogError("❌ El juego no funcionará correctamente sin las cartas especiales.");
            return;
        }

        try
        {
            // Deserializar el JSON a objetos C# usando JsonUtility
            CartasEspecialesRoot root = JsonUtility.FromJson<CartasEspecialesRoot>(cartasEspecialesJSON.text);

            // Validar que el JSON tiene contenido
            if (root == null || root.Cards == null || root.Cards.Count == 0)
            {
                Debug.LogError("❌ CRÍTICO: El JSON está vacío o mal formado!");
                return;
            }

            // Obtener el primer elemento del array Cards (contiene benefits y penalty)
            CartaData cartaData = root.Cards[0];

            // Cargar la lista de cartas de beneficio
            if (cartaData.benefits != null && cartaData.benefits.Count > 0)
            {
                benefits = new List<Carta>(cartaData.benefits);
                Debug.Log($"✅ Cargadas {benefits.Count} cartas de beneficio desde JSON");
            }
            else
            {
                Debug.LogError("❌ No se encontraron cartas de beneficio en el JSON");
                benefits = new List<Carta>();
            }

            // Cargar la lista de cartas de penalidad
            if (cartaData.penalty != null && cartaData.penalty.Count > 0)
            {
                penalty = new List<Carta>(cartaData.penalty);
                Debug.Log($"✅ Cargadas {penalty.Count} cartas de penalidad desde JSON");
            }
            else
            {
                Debug.LogError("❌ No se encontraron cartas de penalidad en el JSON");
                penalty = new List<Carta>();
            }
        }
        catch (System.Exception e)
        {
            // Capturar cualquier error durante la carga y mostrar información detallada
            Debug.LogError($"❌ CRÍTICO: Error al cargar cartas desde JSON: {e.Message}");
            Debug.LogError($"❌ Stack trace: {e.StackTrace}");
        }
    }

    // ============================================
    // SECCIÓN 7: GESTIÓN DE CASILLAS ESPECIALES
    // ============================================
    
    /// <summary>
    /// Ejecuta la acción especial según el tipo de casilla en la que cayó el jugador.
    /// Punto de entrada principal para las casillas especiales del tablero.
    /// </summary>
    public void EjecutarAccionEspecial(Tile.Categoria categoria, MovePlayer jugador)
    {
        if (cartaUI == null) return;
        
        // Determinar qué hacer según el tipo de casilla
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

    /// <summary>
    /// Maneja la lógica de una casilla neutral (no pasa nada).
    /// </summary>
    private void ManejarCasillaNeutral(MovePlayer jugador)
    {
        cartaUI.MostrarMensajeEspecial("Casilla Neutral: ¡Descansas un momento! No pasa nada.", () =>
        {
            Debug.Log("💤 Casilla neutral: El jugador descansa");
            // Desbloquear el dado para que el jugador pueda continuar
            if (dadoController != null)
                dadoController.BloquearDado(false);
        });
    }

    /// <summary>
    /// Maneja la lógica de una casilla de beneficios.
    /// Otorga una carta especial positiva que puede almacenarse.
    /// </summary>
    private void ManejarCasillaBeneficios(MovePlayer jugador)
    {
        // Obtener una carta de beneficio aleatoria
        Carta cartaBeneficio = ObtenerCartaBeneficioAleatoria();
        if (cartaBeneficio != null)
        {
            // Mostrar decisión al jugador: ¿guardar o usar ahora?
            cartaUI.MostrarDecisionAlmacenar(cartaBeneficio, jugador, () =>
            {
                // Callback: desbloquear el dado después de tomar la decisión
                if (dadoController != null)
                    dadoController.BloquearDado(false);
            });
        }
    }
    
    /// <summary>
    /// Maneja la lógica de una casilla de penalidad.
    /// Aplica un efecto negativo inmediato al jugador.
    /// </summary>
    private void ManejarCasillaPenalidad(MovePlayer jugador)
    {
        // Obtener una carta de penalidad aleatoria
        Carta cartaPenalidad = ObtenerCartaPenalidadAleatoria();
        if (cartaPenalidad != null)
        {
            // Mostrar mensaje de penalidad
            cartaUI.MostrarMensajeEspecial($"⚡ ¡Casilla de penalidad!\n{cartaPenalidad.pregunta}", () =>
            {
                // Ejecutar el efecto negativo
                EjecutarPenalidad(cartaPenalidad, jugador);
            });
        }
    }

    // ============================================
    // SECCIÓN 8: MOSTRAR CARTAS DE TRIVIA
    // ============================================
    
    /// <summary>
    /// Muestra una carta de trivia al jugador según la categoría de la casilla.
    /// Bloquea el dado mientras se responde la pregunta.
    /// </summary>
    public void MostrarCarta(Tile.Categoria categoria, System.Action onRespuestaIncorrecta = null)
    {
        // Obtener una carta aleatoria de la categoría correspondiente
        Carta carta = ObtenerCartaAleatoria(categoria);
        if (carta != null && cartaUI != null)
        {
            // Bloquear el dado mientras se muestra la pregunta
            if (dadoController != null) dadoController.BloquearDado(true);
            
            // Mostrar la carta en la UI
            cartaUI.MostrarCarta(carta, (int respuestaSeleccionada) =>
            {
                // Verificar si la respuesta es correcta
                bool esCorrecta = respuestaSeleccionada == carta.respuestaCorrecta;
                Debug.Log(esCorrecta ? "✅ Respuesta correcta" : "❌ Respuesta incorrecta");
                
                // Si es incorrecta, ejecutar callback adicional si existe
                if (!esCorrecta && onRespuestaIncorrecta != null) 
                    onRespuestaIncorrecta.Invoke();
                
                // Desbloquear el dado después de responder
                if (dadoController != null) dadoController.BloquearDado(false);
            });
        }
    }

    // ============================================
    // SECCIÓN 9: OBTENCIÓN DE CARTAS ALEATORIAS
    // ============================================
    
    /// <summary>
    /// Obtiene una carta aleatoria de la lista correspondiente a la categoría.
    /// Usa switch expression para seleccionar la lista apropiada.
    /// </summary>
    private Carta ObtenerCartaAleatoria(Tile.Categoria categoria)
    {
        // Seleccionar la lista según la categoría usando switch expression
        List<Carta> lista = categoria switch
        {
            Tile.Categoria.Historia => historia,
            Tile.Categoria.Geografia => geografia,
            Tile.Categoria.Ciencia => ciencia,
            Tile.Categoria.Benefits => benefits,
            Tile.Categoria.Penalty => penalty,
            _ => null  // Categoría no reconocida
        };

        // Validar que la lista existe y tiene elementos
        if (lista == null || lista.Count == 0) return null;

        // Seleccionar un índice aleatorio
        int index = Random.Range(0, lista.Count);
        return lista[index];
    }

    /// <summary>
    /// Obtiene una carta de beneficio aleatoria de la lista de benefits.
    /// </summary>
    public Carta ObtenerCartaBeneficioAleatoria()
    {
        if (benefits.Count == 0)
        {
            Debug.LogWarning("⚠️ No hay cartas de beneficio disponibles!");
            return null;
        }
        int index = Random.Range(0, benefits.Count);
        return benefits[index];
    }

    /// <summary>
    /// Obtiene una carta de penalidad aleatoria de la lista de penalty.
    /// </summary>
    public Carta ObtenerCartaPenalidadAleatoria()
    {
        if (penalty.Count == 0)
        {
            Debug.LogWarning("⚠️ No hay cartas de penalidad disponibles!");
            return null;
        }
        int index = Random.Range(0, penalty.Count);
        return penalty[index];
    }

    // ============================================
    // SECCIÓN 10: GESTIÓN DEL INVENTARIO
    // ============================================
    
    /// <summary>
    /// Intenta agregar una carta al inventario del jugador.
    /// Si está lleno, muestra el panel de reemplazo.
    /// </summary>
    public void IntentarAgregarCarta(Carta nuevaCarta)
    {
        // Si hay espacio disponible, agregar directamente
        if (storage.Count < maxStorage)
        {
            storage.Add(nuevaCarta);
            Debug.Log($"✅ Carta agregada al storage: {nuevaCarta.pregunta} (Total: {storage.Count}/{maxStorage})");
            ActualizarUIStorage();
        }
        else
        {
            // Si está lleno, mostrar panel para elegir qué carta reemplazar
            Debug.Log("⚠️ Storage lleno! Mostrando panel para reemplazar.");
            if (replacementUI != null)
            {
                replacementUI.MostrarPanelReemplazo(storage, nuevaCarta);
            }
            else
            {
                Debug.LogError("¡ReplacementUI no está asignado en CartaManager!");
            }
        }
    }

    /// <summary>
    /// Reemplaza una carta existente en el inventario con una nueva.
    /// </summary>
    public void ReemplazarCartaEnStorage(int index, Carta nuevaCarta)
    {
        // Validar que el índice es válido
        if (index < 0 || index >= storage.Count)
        {
            Debug.LogError($"Índice de reemplazo inválido: {index}");
            return;
        }

        // Reemplazar la carta en la posición indicada
        Debug.Log($"🔄 Reemplazando '{storage[index].pregunta}' con '{nuevaCarta.pregunta}' en el slot {index}.");
        storage[index] = nuevaCarta;
        ActualizarUIStorage();
    }
    
    /// <summary>
    /// Usa una carta del inventario aplicando su efecto al jugador.
    /// Elimina la carta del inventario después de usarla.
    /// </summary>
    public void UsarCartaDelStorage(int index, MovePlayer jugador)
    {
        // Validar el índice
        if (index < 0 || index >= storage.Count)
        {
            Debug.Log("❌ Índice inválido o no hay carta en esa posición.");
            return;
        }
        
        // Obtener la carta
        Carta carta = storage[index];
        
        // Ejecutar el efecto según el tipo de carta
        if (EsBeneficio(carta)) 
            EjecutarBeneficio(carta, jugador);
        else if (EsPenalidad(carta)) 
            EjecutarPenalidad(carta, jugador);
        
        // Eliminar la carta del inventario
        storage.RemoveAt(index);
        ActualizarUIStorage();
        Debug.Log($"🎯 Carta usada: {carta.pregunta} (Restantes: {storage.Count}/{maxStorage})");
    }

    // ============================================
    // SECCIÓN 11: CLASIFICACIÓN DE CARTAS
    // ============================================
    
    /// <summary>
    /// Determina si una carta es de beneficio según su acción.
    /// </summary>
    private bool EsBeneficio(Carta carta)
    {
        return carta.accion.Contains("Avanza") || 
               carta.accion == "RepiteTurno" || 
               carta.accion == "Intercambia" || 
               carta.accion == "Inmunidad" || 
               carta.accion == "DobleDado" || 
               carta.accion == "TeletransporteAdelante" || 
               carta.accion == "ElegirDado" || 
               carta.accion == "RobarCarta";
    }

    /// <summary>
    /// Determina si una carta es de penalidad según su acción.
    /// </summary>
    private bool EsPenalidad(Carta carta)
    {
        return carta.accion.Contains("Retrocede") || 
               carta.accion == "PierdeTurno" || 
               carta.accion == "IrSalida" || 
               carta.accion == "IntercambiaUltimo" || 
               carta.accion == "PerderCartas" || 
               carta.accion == "BloquearDados" || 
               carta.accion == "TeletransporteAtras" || 
               carta.accion == "MovimientoLimitado";
    }

    /// <summary>
    /// Actualiza la interfaz del inventario de cartas.
    /// Llama al BonusUI para refrescar la visualización.
    /// </summary>
    public void ActualizarUIStorage()
    {
        if (bonusUI != null)
        {
            bonusUI.ActualizarUI(storage);
        }
    }

    // ============================================
    // SECCIÓN 12: EJECUCIÓN DE BENEFICIOS
    // ============================================
    
    /// <summary>
    /// Ejecuta el efecto de una carta de beneficio aplicándolo al jugador.
    /// Cada caso en el switch implementa una mecánica diferente.
    /// </summary>
    public void EjecutarBeneficio(Carta carta, MovePlayer jugador)
    {
        if (carta == null || jugador == null) return;

        Debug.Log($"🥳 Ejecutando beneficio: {carta.accion}");

        switch (carta.accion)
        {
            case "Avanza1":
                // Mover 1 casilla adelante
                jugador.StartCoroutine(jugador.JumpMultipleTimes(1));
                break;
            case "Avanza2":
                // Mover 2 casillas adelante
                jugador.StartCoroutine(jugador.JumpMultipleTimes(2));
                break;
            case "Avanza3":
                // Mover 3 casillas adelante
                jugador.StartCoroutine(jugador.JumpMultipleTimes(3));
                break;
            case "RepiteTurno":
                // Desbloquear el dado para tirar otra vez
                if (dadoController != null)
                    dadoController.BloquearDado(false);
                Debug.Log("🔁 ¡Repites turno!");
                break;
            case "Intercambia":
                // Intercambiar posición con otro jugador (requiere lógica multijugador)
                Debug.Log("🔄 Intercambia posición con otro jugador (implementar lógica multijugador)");
                break;
            case "Inmunidad":
                // Protección contra penalidades (requiere sistema de estados)
                Debug.Log("🛡️ Inmune a penalidades por 1 turno");
                break;
            case "DobleDado":
                // Tirar dos dados en el próximo turno (requiere modificación del DiceController)
                Debug.Log("🎲🎲 Doble dado en próximo turno");
                break;
            case "TeletransporteAdelante":
                // Salto grande aleatorio hacia adelante (5-9 casillas)
                int saltoAdelante = Random.Range(5, 10);
                jugador.StartCoroutine(jugador.JumpMultipleTimes(saltoAdelante));
                Debug.Log($"🚀 Teletransporte {saltoAdelante} casillas adelante");
                break;
            case "ElegirDado":
                // Permitir al jugador elegir el resultado del dado (requiere UI especial)
                Debug.Log("🎯 Puedes elegir el resultado del próximo dado");
                break;
            case "RobarCarta":
                // Robar una carta del inventario de otro jugador (multijugador)
                Debug.Log("💸 Robas una carta especial de otro jugador");
                break;
            default:
                // Acción no reconocida
                Debug.Log($"⚠️ Acción de beneficio no reconocida: {carta.accion}");
                break;
        }
    }

    // ============================================
    // SECCIÓN 13: EJECUCIÓN DE PENALIDADES
    // ============================================
    
    /// <summary>
    /// Ejecuta el efecto de una carta de penalidad aplicándolo al jugador.
    /// Cada caso implementa un castigo o efecto negativo diferente.
    /// </summary>
    public void EjecutarPenalidad(Carta carta, MovePlayer jugador)
    {
        if (carta == null || jugador == null) return;

        Debug.Log($"⚡ Ejecutando penalidad: {carta.accion}");

        switch (carta.accion)
        {
            case "Retrocede1":
                // Mover 1 casilla hacia atrás
                jugador.StartCoroutine(jugador.Retroceder(1));
                break;
            case "Retrocede2":
                // Mover 2 casillas hacia atrás
                jugador.StartCoroutine(jugador.Retroceder(2));
                break;
            case "Retrocede3":
                // Mover 3 casillas hacia atrás
                jugador.StartCoroutine(jugador.Retroceder(3));
                break;
            case "PierdeTurno":
                // Bloquear el dado para el siguiente turno
                if (dadoController != null)
                {
                    dadoController.BloquearDado(true);
                    Debug.Log("⏳ Dado bloqueado - pierdes el siguiente turno");
                }
                break;
            case "IrSalida":
                // Teletransportar al jugador a la casilla de inicio (0)
                jugador.StartCoroutine(jugador.IrACasilla(0));
                Debug.Log("🏠 Regresando a la salida");
                break;
            case "IntercambiaUltimo":
                // Intercambiar posición con el jugador en último lugar (multijugador)
                Debug.Log("🔄 Intercambias posición con el último jugador");
                break;
            case "PerderCartas":
                // Vaciar el inventario de cartas especiales
                storage.Clear();
                ActualizarUIStorage();
                Debug.Log("💸 Pierdes todas tus cartas especiales");
                break;
            case "BloquearDados":
                // Bloquear el dado por múltiples turnos
                if (dadoController != null)
                {
                    dadoController.BloquearDado(true);
                    Debug.Log("🔒 Dados bloqueados por 2 turnos");
                }
                break;
            case "TeletransporteAtras":
                // Salto grande aleatorio hacia atrás (3-7 casillas)
                int saltoAtras = Random.Range(3, 8);
                jugador.StartCoroutine(jugador.Retroceder(saltoAtras));
                Debug.Log($"🚀 Teletransporte {saltoAtras} casillas atrás");
                break;
            case "MovimientoLimitado":
                // Limitar movimiento a 1 casilla por varios turnos (requiere sistema de estados)
                Debug.Log("🐌 Solo puedes moverte 1 casilla por 3 turnos");
                break;
            default:
                // Acción no reconocida
                Debug.Log($"⚠️ Acción de penalidad no reconocida: {carta.accion}");
                break;
        }
    }
}