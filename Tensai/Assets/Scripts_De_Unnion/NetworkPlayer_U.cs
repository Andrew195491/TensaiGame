using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Componente de red para cada jugador.
/// Sincroniza posición, estado y propiedades entre clientes.
/// </summary>
public class NetworkPlayer_U : NetworkBehaviour
{
    [Header("Referencias")]
    public MovePlayer_U movePlayer;
    
    [Header("Identificación")]
    public NetworkVariable<ulong> playerId = new NetworkVariable<ulong>();
    public NetworkVariable<int> currentPosition = new NetworkVariable<int>(0);
    
    [Header("Visual")]
    public Material[] playerMaterials; // Materiales para diferenciar jugadores
    private Renderer playerRenderer;
    
    private void Awake()
    {
        playerRenderer = GetComponent<Renderer>();
        
        if (movePlayer == null)
        {
            movePlayer = GetComponent<MovePlayer_U>();
        }
    }
    
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Este es el jugador local
            Debug.Log("🎮 Jugador local spawneado");
            playerId.Value = OwnerClientId;
            
            // Configurar cámara para seguir a este jugador
            ConfigureLocalCamera();
        }
        else
        {
            // Jugador remoto
            Debug.Log($"👥 Jugador remoto spawneado: {OwnerClientId}");
        }
        
        // Aplicar material según el índice del jugador
        ApplyPlayerMaterial();
        
        // Suscribirse a cambios de posición
        currentPosition.OnValueChanged += OnPositionChanged;
    }
    
    public override void OnNetworkDespawn()
    {
        currentPosition.OnValueChanged -= OnPositionChanged;
    }
    
    private void ApplyPlayerMaterial()
    {
        if (playerRenderer != null && playerMaterials.Length > 0)
        {
            int materialIndex = (int)(OwnerClientId % (ulong)playerMaterials.Length);
            playerRenderer.material = playerMaterials[materialIndex];
        }
    }
    
    private void ConfigureLocalCamera()
    {
        // Buscar la cámara híbrida
        ThirdPersonCameraHybrid_U cam = FindObjectOfType<ThirdPersonCameraHybrid_U>();
        if (cam != null)
        {
            cam.SetTarget(transform, smooth: true);
            Debug.Log("📷 Cámara configurada para jugador local");
        }
    }
    
    private void OnPositionChanged(int previousValue, int newValue)
    {
        Debug.Log($"📍 Posición actualizada: {previousValue} → {newValue}");
        
        if (movePlayer != null)
        {
            movePlayer.currentIndex = newValue;
        }
    }
    
    // ============================================
    // SINCRONIZACIÓN DE MOVIMIENTO
    // ============================================
    
    /// <summary>
    /// Actualiza la posición en la red cuando el jugador se mueve
    /// </summary>
    public void SyncPosition(int newPosition)
    {
        if (IsOwner)
        {
            UpdatePositionServerRpc(newPosition);
        }
    }
    
    [ServerRpc]
    private void UpdatePositionServerRpc(int newPosition)
    {
        currentPosition.Value = newPosition;
    }
    
    // ============================================
    // UTILIDADES
    // ============================================
    
    public bool IsLocalPlayer => IsOwner;
    
    public int GetCurrentIndex()
    {
        return currentPosition.Value;
    }
}