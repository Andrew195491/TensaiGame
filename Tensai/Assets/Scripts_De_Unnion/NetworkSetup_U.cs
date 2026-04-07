using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controlador de interfaz para iniciar/unirse a partidas de red.
/// Gestiona el lobby y la configuración inicial de red.
/// </summary>
public class NetworkSetup_U : MonoBehaviour
{
    [Header("UI del Lobby")]
    public GameObject lobbyPanel;
    public GameObject gamePanel;
    public Button hostButton;
    public Button clientButton;
    public TMP_InputField ipInputField;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI playerCountText;
    
    [Header("Configuración de Red")]
    public string defaultIP = "127.0.0.1";
    public ushort defaultPort = 7777;
    
    private NetworkManager networkManager;
    private UnityTransport transport;
    
    private void Awake()
    {
        networkManager = NetworkManager.Singleton;
        
        if (networkManager == null)
        {
            Debug.LogError("❌ NetworkManager no encontrado en la escena");
            return;
        }
        
        transport = networkManager.GetComponent<UnityTransport>();
        
        // Configurar botones
        if (hostButton != null)
            hostButton.onClick.AddListener(StartHost);
            
        if (clientButton != null)
            clientButton.onClick.AddListener(StartClient);
        
        // Mostrar panel del lobby al inicio
        ShowLobby();
        
        // Suscribirse a eventos de conexión
        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }
    
    private void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
    
    // ============================================
    // INICIO DE PARTIDA
    // ============================================
    
    public void StartHost()
    {
        if (networkManager == null) return;
        
        ConfigureTransport();
        
        bool success = networkManager.StartHost();
        
        if (success)
        {
            UpdateStatus("🎮 Hosting... Esperando jugadores");
            Debug.Log("✅ Host iniciado correctamente");
        }
        else
        {
            UpdateStatus("❌ Error al iniciar host");
            Debug.LogError("❌ No se pudo iniciar el host");
        }
    }
    
    public void StartClient()
    {
        if (networkManager == null) return;
        
        // Obtener IP del campo de texto
        string serverIP = ipInputField != null && !string.IsNullOrEmpty(ipInputField.text) 
            ? ipInputField.text 
            : defaultIP;
        
        ConfigureTransport(serverIP);
        
        bool success = networkManager.StartClient();
        
        if (success)
        {
            UpdateStatus($"🔌 Conectando a {serverIP}...");
            Debug.Log($"✅ Cliente intentando conectar a {serverIP}");
        }
        else
        {
            UpdateStatus("❌ Error al conectar");
            Debug.LogError("❌ No se pudo iniciar el cliente");
        }
    }
    
    public void StartServer()
    {
        if (networkManager == null) return;
        
        ConfigureTransport();
        
        bool success = networkManager.StartServer();
        
        if (success)
        {
            UpdateStatus("🖥️ Servidor dedicado iniciado");
            Debug.Log("✅ Servidor dedicado iniciado");
        }
        else
        {
            UpdateStatus("❌ Error al iniciar servidor");
            Debug.LogError("❌ No se pudo iniciar el servidor");
        }
    }
    
    // ============================================
    // CONFIGURACIÓN DE TRANSPORTE
    // ============================================
    
    private void ConfigureTransport(string ip = null)
    {
        if (transport == null) return;
        
        transport.ConnectionData.Address = ip ?? defaultIP;
        transport.ConnectionData.Port = defaultPort;
        transport.ConnectionData.ServerListenAddress = "0.0.0.0";
        
        Debug.Log($"🌐 Configurado: {transport.ConnectionData.Address}:{transport.ConnectionData.Port}");
    }
    
    // ============================================
    // EVENTOS DE CONEXIÓN
    // ============================================
    
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"✅ Cliente conectado: {clientId}");
        
        if (NetworkManager.Singleton.IsServer)
        {
            int playerCount = NetworkManager.Singleton.ConnectedClients.Count;
            UpdatePlayerCount(playerCount);
            
            if (playerCount >= 2)
            {
                UpdateStatus($"✅ {playerCount} jugadores conectados - Iniciando partida...");
            }
            else
            {
                UpdateStatus($"⏳ Esperando más jugadores ({playerCount}/2 mínimo)");
            }
        }
        
        // Si soy el cliente que acaba de conectarse
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            ShowGame();
        }
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"❌ Cliente desconectado: {clientId}");
        
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            int playerCount = NetworkManager.Singleton.ConnectedClients.Count;
            UpdatePlayerCount(playerCount);
            UpdateStatus($"⚠️ Jugador desconectado. {playerCount} jugadores restantes");
        }
    }
    
    // ============================================
    // UTILIDADES UI
    // ============================================
    
    private void ShowLobby()
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        if (gamePanel != null) gamePanel.SetActive(false);
    }
    
    private void ShowGame()
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (gamePanel != null) gamePanel.SetActive(true);
    }
    
    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"📢 Status: {message}");
    }
    
    private void UpdatePlayerCount(int count)
    {
        if (playerCountText != null)
        {
            playerCountText.text = $"Jugadores: {count}";
        }
    }
    
    // ============================================
    // DESCONEXIÓN
    // ============================================
    
    public void Disconnect()
    {
        if (networkManager != null && networkManager.IsListening)
        {
            networkManager.Shutdown();
            ShowLobby();
            UpdateStatus("🔌 Desconectado");
            Debug.Log("🔌 Desconectado de la red");
        }
    }
    
    // ============================================
    // MÉTODOS PÚBLICOS PARA UI
    // ============================================
    
    public void OnHostButtonClick() => StartHost();
    public void OnClientButtonClick() => StartClient();
    public void OnDisconnectButtonClick() => Disconnect();
}