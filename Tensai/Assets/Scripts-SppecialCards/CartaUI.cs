using UnityEngine; // Necesario para MonoBehaviour
using UnityEngine.UI;  // Necesario para Button
using TMPro;  // Necesario para TextMeshProUGUI


/// <summary>
/// Gestiona la interfaz de usuario para mostrar cartas de trivia y mensajes especiales.
/// Maneja tres modos diferentes: preguntas de trivia, mensajes informativos, y decisiones de almacenamiento.
/// </summary>
public class CartaUI : MonoBehaviour
{
    // ============================================
    // SECCIÓN 1: ELEMENTOS DE LA INTERFAZ
    // ============================================
    
    // Panel principal que contiene todos los elementos de la UI de cartas
    public GameObject panel;
    
    // Texto que muestra la pregunta o mensaje principal
    public TextMeshProUGUI textoPregunta;
    
    // Textos para las tres opciones de respuesta
    public TextMeshProUGUI textoRespuesta1;
    public TextMeshProUGUI textoRespuesta2;
    public TextMeshProUGUI textoRespuesta3;

    // Botones correspondientes a cada respuesta
    public Button boton1;
    public Button boton2;
    public Button boton3;

    // Referencia al controlador del dado para bloquearlo/desbloquearlo
    public DiceController dado;

    // Variable que almacena cuál es la respuesta correcta de la carta actual
    private int respuestaCorrectaActual = 1;

    // ============================================
    // SECCIÓN 2: INICIALIZACIÓN
    // ============================================
    
    void Start()
    {
        // Configurar listeners iniciales de los botones
        // Estos se sobrescribirán cuando se muestre una carta específica
        boton1.onClick.AddListener(() => EvaluarRespuesta(1));
        boton2.onClick.AddListener(() => EvaluarRespuesta(2));
        boton3.onClick.AddListener(() => EvaluarRespuesta(3));

        // Ocultar el panel al inicio del juego
        panel.SetActive(false);
    }

    // ============================================
    // SECCIÓN 3: MOSTRAR CARTA DE TRIVIA
    // ============================================
    
    /// <summary>
    /// Muestra una carta de trivia con pregunta y tres opciones de respuesta.
    /// Configura los botones para que ejecuten un callback cuando se seleccione una respuesta.
    /// </summary>
    /// <param name="carta">Carta con la pregunta y respuestas a mostrar</param>
    /// <param name="onRespuesta">Callback que recibe el número de respuesta seleccionada (1, 2 o 3)</param>
    public void MostrarCarta(Carta carta, System.Action<int> onRespuesta)
    {
        // Mostrar el panel
        panel.SetActive(true);
        
        // Configurar los textos con el contenido de la carta
        textoPregunta.text = carta.pregunta;
        textoRespuesta1.text = carta.respuesta1;
        textoRespuesta2.text = carta.respuesta2;
        textoRespuesta3.text = carta.respuesta3;

        // Limpiar listeners anteriores para evitar comportamientos duplicados
        boton1.onClick.RemoveAllListeners();
        boton2.onClick.RemoveAllListeners();
        boton3.onClick.RemoveAllListeners();

        // Configurar cada botón para:
        // 1. Ocultar el panel
        // 2. Ejecutar el callback con el número de respuesta seleccionada
        boton1.onClick.AddListener(() => { panel.SetActive(false); onRespuesta(1); });
        boton2.onClick.AddListener(() => { panel.SetActive(false); onRespuesta(2); });
        boton3.onClick.AddListener(() => { panel.SetActive(false); onRespuesta(3); });
    }

    // ============================================
    // SECCIÓN 4: MOSTRAR MENSAJE ESPECIAL
    // ============================================
    
    /// <summary>
    /// Muestra un mensaje informativo sin opciones de respuesta.
    /// Usado para casillas neutrales, beneficios inmediatos o penalidades.
    /// Solo muestra un botón "Aceptar" para cerrar el mensaje.
    /// </summary>
    /// <param name="mensaje">Texto del mensaje a mostrar</param>
    /// <param name="onCerrar">Callback que se ejecuta cuando el usuario cierra el mensaje</param>
    public void MostrarMensajeEspecial(string mensaje, System.Action onCerrar)
    {
        // Mostrar el panel
        panel.SetActive(true);

        // Configurar solo el mensaje principal, sin respuestas múltiples
        textoPregunta.text = mensaje;
        textoRespuesta1.text = "";
        textoRespuesta2.text = "";
        textoRespuesta3.text = "";

        // Ocultar los botones 2 y 3 ya que no se necesitan respuestas
        boton2.gameObject.SetActive(false);
        boton3.gameObject.SetActive(false);

        // Convertir el botón 1 en un botón de "Aceptar"
        boton1.GetComponentInChildren<TextMeshProUGUI>().text = "Aceptar";
        boton1.onClick.RemoveAllListeners();
        boton1.onClick.AddListener(() =>
        {
            // Ocultar el panel
            panel.SetActive(false);
            
            // Restaurar visibilidad de los botones para la próxima carta
            boton2.gameObject.SetActive(true);
            boton3.gameObject.SetActive(true);
            
            // Ejecutar callback si existe
            onCerrar?.Invoke();
        });
    }

    // ============================================
    // SECCIÓN 5: EVALUACIÓN DE RESPUESTAS
    // ============================================
    
    /// <summary>
    /// Evalúa si la respuesta seleccionada es correcta.
    /// Este método se usa en el modo básico (no con callbacks personalizados).
    /// </summary>
    /// <param name="respuestaSeleccionada">Número de la respuesta elegida (1, 2 o 3)</param>
    void EvaluarRespuesta(int respuestaSeleccionada)
    {
        // Comparar la respuesta seleccionada con la respuesta correcta
        if (respuestaSeleccionada == respuestaCorrectaActual)
        {
            Debug.Log("✅ ¡Respuesta correcta!");
        }
        else
        {
            Debug.Log("❌ Respuesta incorrecta.");
        }

        // Ocultar el panel de la carta
        panel.SetActive(false);
        
        // Desbloquear el dado para que el jugador pueda continuar
        if (dado != null)
            dado.BloquearDado(false);
    }

    // ============================================
    // SECCIÓN 6: DECISIÓN DE ALMACENAMIENTO
    // ============================================
    
    /// <summary>
    /// Muestra opciones para decidir qué hacer con una carta de beneficio obtenida.
    /// El jugador puede elegir entre guardarla en su inventario o descartarla.
    /// Esta interfaz aparece cuando el jugador cae en una casilla de beneficios.
    /// </summary>
    /// <param name="carta">Carta de beneficio obtenida</param>
    /// <param name="jugador">Referencia al jugador (para futuras implementaciones)</param>
    /// <param name="onDecisionMade">Callback que se ejecuta después de tomar la decisión</param>
    public void MostrarDecisionAlmacenar(Carta carta, MovePlayer jugador, System.Action onDecisionMade)
    {
        // Mostrar el panel
        panel.SetActive(true);

        // 1. Configurar el texto explicativo
        // Muestra el nombre/descripción de la carta y pregunta si quiere guardarla
        textoPregunta.text = $"¡Has obtenido un beneficio!\n<b>{carta.pregunta}</b>\n\n¿Quieres guardarla en tu inventario?";
        
        // Limpiar textos de respuestas (no se usan en este modo)
        textoRespuesta1.text = "";
        textoRespuesta2.text = "";
        textoRespuesta3.text = "";

        // 2. Ocultar el botón 3 (no se necesita)
        boton3.gameObject.SetActive(false);
        
        // 3. Configurar el botón 1 como "Guardar"
        boton1.gameObject.SetActive(true);
        boton1.GetComponentInChildren<TextMeshProUGUI>().text = "Guardar";
        boton1.onClick.RemoveAllListeners();
        boton1.onClick.AddListener(() =>
        {
            // Intentar agregar la carta al inventario
            // El CartaManager gestionará automáticamente si el inventario está lleno
            // (mostrará el panel de reemplazo si es necesario)
            CartaManager.instancia.IntentarAgregarCarta(carta);
            
            // Cerrar el panel y ejecutar callback
            CerrarPanelDecision(onDecisionMade);
        });

        // 4. Configurar el botón 2 como "Cancelar"
        boton2.gameObject.SetActive(true);
        boton2.GetComponentInChildren<TextMeshProUGUI>().text = "Cancelar";
        boton2.onClick.RemoveAllListeners();
        boton2.onClick.AddListener(() =>
        {
            // El jugador decide no guardar la carta (la descarta)
            Debug.Log("Carta de beneficio descartada.");
            
            // Cerrar el panel y ejecutar callback
            CerrarPanelDecision(onDecisionMade);
        });
    }

    // ============================================
    // SECCIÓN 7: CERRAR PANEL DE DECISIÓN
    // ============================================
    
    /// <summary>
    /// Cierra el panel de decisión y restaura el estado normal de los botones.
    /// Se asegura de que todos los botones estén visibles para la próxima interacción.
    /// </summary>
    /// <param name="onDecisionMade">Callback que se ejecuta después de cerrar el panel</param>
    private void CerrarPanelDecision(System.Action onDecisionMade)
    {
        // Ocultar el panel
        panel.SetActive(false);
        
        // Restaurar la visibilidad del botón 3 para futuras interacciones
        boton3.gameObject.SetActive(true);
        
        // Ejecutar callback si existe (típicamente desbloquea el dado)
        onDecisionMade?.Invoke();
    }
}

// ============================================
// NOTAS DE DISEÑO Y USO
// ============================================
/*
 * MODOS DE OPERACIÓN DE ESTA UI:
 * 
 * 1. MODO TRIVIA (MostrarCarta):
 *    - Muestra pregunta con 3 opciones
 *    - Los 3 botones están activos
 *    - Ejecuta callback con el número de respuesta elegida
 * 
 * 2. MODO MENSAJE (MostrarMensajeEspecial):
 *    - Muestra solo un mensaje informativo
 *    - Solo botón 1 visible como "Aceptar"
 *    - Usado para casillas neutrales o efectos inmediatos
 * 
 * 3. MODO DECISIÓN (MostrarDecisionAlmacenar):
 *    - Muestra carta de beneficio obtenida
 *    - Botones 1 y 2 como "Guardar" y "Cancelar"
 *    - Permite al jugador decidir si quiere la carta
 * 
 * FLUJO TÍPICO:
 * 1. CartaManager llama a uno de estos métodos según el contexto
 * 2. El panel se muestra y bloquea la interacción del jugador
 * 3. El jugador hace su elección
 * 4. Se ejecuta el callback correspondiente
 * 5. El panel se oculta y el juego continúa
 * 
 * CONSIDERACIONES:
 * - Los listeners de botones se limpian antes de asignar nuevos para evitar
 *   comportamientos duplicados o erróneos
 * - La visibilidad de botones se restaura después de cada uso para mantener
 *   consistencia en futuras interacciones
 * - El uso de callbacks (System.Action) permite flexibilidad en cómo se
 *   responde a las acciones del usuario sin acoplar la UI a lógica específica
 */