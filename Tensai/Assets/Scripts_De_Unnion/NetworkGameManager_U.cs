using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gestor de red que sincroniza el estado del juego entre todos los clientes.
/// Extiende GameManager_U para añadir funcionalidad multijugador.
/// </summary>
public class NetworkGameManager_U : NetworkBehaviour
{
    [Header("Referencias Locales")]
    public GameManager_U gameManager;
    
    [Header("Prefabs de Red")]
    public GameObject networkPlayerPrefab;
    
    [Header("Configuración de Partida")]
    public int maxPlayers = 4;
    
    // Estado sincronizado del juego
    private NetworkVariable<int> currentTurnIndex = new NetworkVariable<int>(0);
    private NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(GameState.WaitingForPlayers);
    
    // Lista de jugadores conectados
    private NetworkList<ulong> connectedPlayers;
    
    // Mapeo de ClientId a MovePlayer_U
    private Dictionary<ulong, MovePlayer_U> playerObjects = new Dictionary<ulong, MovePlayer_U>();
    
    public enum GameState
    {
        WaitingForPlayers,
        GameStarting,
        InGame,
        GameEnded
    }
    
    private void Awake()
    {
        connectedPlayers = new NetworkList<ulong>();
    }
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            
            gameState.Value = GameState.WaitingForPlayers;
        }
        
        // Suscribirse a cambios de estado
        gameState.OnValueChanged += OnGameStateChanged;
        currentTurnIndex.OnValueChanged += OnTurnChanged;
    }
    
    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
        
        gameState.OnValueChanged -= OnGameStateChanged;
        currentTurnIndex.OnValueChanged -= OnTurnChanged;
    }
    
    // ============================================
    // GESTIÓN DE CONEXIONES
    // ============================================
    
    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        
        Debug.Log($"🌐 Cliente {clientId} conectado");
        connectedPlayers.Add(clientId);
        
        // Crear objeto del jugador
        SpawnPlayerServerRpc(clientId);
        
        // Si hay suficientes jugadores, iniciar el juego
        if (connectedPlayers.Count >= 2 && gameState.Value == GameState.WaitingForPlayers)
        {
            StartCoroutine(StartGameCountdown());
        }
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        
        Debug.Log($"🌐 Cliente {clientId} desconectado");
        
        if (connectedPlayers.Contains(clientId))
        {
            connectedPlayers.Remove(clientId);
        }
        
        if (playerObjects.ContainsKey(clientId))
        {
            var playerObj = playerObjects[clientId];
            if (playerObj != null)
            {
                Destroy(playerObj.gameObject);
            }
            playerObjects.Remove(clientId);
        }
    }
    
    // ============================================
    // SPAWN DE JUGADORES
    // ============================================
    
    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerServerRpc(ulong clientId)
    {
        if (!IsServer) return;
        
        // Obtener posición inicial
        int playerIndex = connectedPlayers.Count - 1;
        Vector3 spawnPosition = GetSpawnPosition(playerIndex);
        
        // Instanciar prefab de red
        GameObject playerObj = Instantiate(networkPlayerPrefab, spawnPosition, Quaternion.identity);
        NetworkObject netObj = playerObj.GetComponent<NetworkObject>();
        
        if (netObj != null)
        {
            netObj.SpawnAsPlayerObject(clientId);
            
            // Configurar componente MovePlayer_U
            MovePlayer_U movePlayer = playerObj.GetComponent<MovePlayer_U>();
            if (movePlayer == null)
            {
                movePlayer = playerObj.AddComponent<MovePlayer_U>();
            }
            
            movePlayer.dado = gameManager.dadoUI;
            movePlayer.currentIndex = 0;
            movePlayer.TeleportAIndiceSeguro(0);
            
            playerObjects[clientId] = movePlayer;
            
            Debug.Log($"✅ Jugador spawneado para cliente {clientId}");
        }
    }
    
    private Vector3 GetSpawnPosition(int index)
    {
        // Usa las posiciones del tablero
        Transform board = GameObject.Find("Board")?.transform;
        if (board != null && board.childCount > 0)
        {
            Transform firstTile = board.GetChild(0);
            Vector3 basePos = firstTile.position;
            
            // Offset para cada jugador
            Vector3[] offsets = new Vector3[]
            {
                Vector3.zero,
                new Vector3(0.35f, 0f, 0.35f),
                new Vector3(-0.35f, 0f, 0.35f),
                new Vector3(0.35f, 0f, -0.35f)
            };
            
            return basePos + offsets[Mathf.Min(index, offsets.Length - 1)];
        }
        
        return Vector3.zero;
    }
    
    // ============================================
    // INICIO DE PARTIDA
    // ============================================
    
    private IEnumerator StartGameCountdown()
    {
        gameState.Value = GameState.GameStarting;
        
        // Countdown de 3 segundos
        yield return new WaitForSeconds(3f);
        
        gameState.Value = GameState.InGame;
        currentTurnIndex.Value = 0;
        
        // Iniciar el primer turno
        StartTurnServerRpc(0);
    }
    
    private void OnGameStateChanged(GameState previous, GameState current)
    {
        Debug.Log($"📢 Estado del juego: {previous} → {current}");
        
        switch (current)
        {
            case GameState.WaitingForPlayers:
                // Mostrar UI de espera
                break;
                
            case GameState.GameStarting:
                // Mostrar countdown
                break;
                
            case GameState.InGame:
                // Ocultar UI de espera, mostrar UI de juego
                break;
                
            case GameState.GameEnded:
                // Mostrar pantalla de victoria
                break;
        }
    }
    
    // ============================================
    // GESTIÓN DE TURNOS
    // ============================================
    
    [ServerRpc(RequireOwnership = false)]
    private void StartTurnServerRpc(int turnIndex)
    {
        if (!IsServer || gameState.Value != GameState.InGame) return;
        
        ulong currentPlayerId = connectedPlayers[turnIndex];
        Debug.Log($"🎲 Turno del jugador {currentPlayerId}");
        
        // Notificar a todos los clientes
        NotifyTurnStartClientRpc(currentPlayerId);
    }
    
    [ClientRpc]
    private void NotifyTurnStartClientRpc(ulong playerId)
    {
        bool isMyTurn = playerId == NetworkManager.Singleton.LocalClientId;
        
        if (isMyTurn)
        {
            Debug.Log("✅ Es tu turno");
            EnableDiceForLocalPlayer();
        }
        else
        {
            Debug.Log($"⏳ Turno del jugador {playerId}");
            DisableDiceForLocalPlayer();
        }
    }
    
    private void OnTurnChanged(int previous, int current)
    {
        Debug.Log($"🔄 Turno cambiado: {previous} → {current}");
    }
    
    // ============================================
    // TIRADA DE DADO
    // ============================================
    
    [ServerRpc(RequireOwnership = false)]
    public void RollDiceServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        
        // Verificar que es el turno del jugador
        ulong currentPlayerId = connectedPlayers[currentTurnIndex.Value];
        if (playerId != currentPlayerId)
        {
            Debug.LogWarning($"⚠️ Jugador {playerId} intentó tirar fuera de turno");
            return;
        }
        
        // Generar resultado del dado
        int diceResult = Random.Range(1, 7);
        Debug.Log($"🎲 Jugador {playerId} sacó {diceResult}");
        
        // Notificar resultado a todos
        BroadcastDiceResultClientRpc(playerId, diceResult);
    }
    
    [ClientRpc]
    private void BroadcastDiceResultClientRpc(ulong playerId, int result)
    {
        Debug.Log($"🎲 Resultado del dado: {result} (Jugador {playerId})");
        
        // Aplicar movimiento
        if (playerObjects.TryGetValue(playerId, out MovePlayer_U player))
        {
            StartCoroutine(HandlePlayerMove(player, result, playerId));
        }
    }
    
    private IEnumerator HandlePlayerMove(MovePlayer_U player, int steps, ulong playerId)
    {
        yield return StartCoroutine(player.JumpMultipleTimes(steps));
        
        // Solo el servidor procesa la resolución de casilla
        if (IsServer)
        {
            ResolverCasillaServerRpc(playerId);
        }
    }
    
    // ============================================
    // RESOLUCIÓN DE CASILLAS
    // ============================================
    
    [ServerRpc(RequireOwnership = false)]
    private void ResolverCasillaServerRpc(ulong playerId)
    {
        if (!IsServer) return;
        
        if (!playerObjects.TryGetValue(playerId, out MovePlayer_U player))
        {
            Debug.LogError($"❌ No se encontró el jugador {playerId}");
            return;
        }
        
        Tile_U tile = player.GetCurrentTile();
        if (tile == null)
        {
            AvanzarTurnoServerRpc();
            return;
        }
        
        // Notificar tipo de casilla a todos
        NotifyTileTypeClientRpc(playerId, (int)tile.tipo, (int)tile.categoria);
    }
    
    [ClientRpc]
    private void NotifyTileTypeClientRpc(ulong playerId, int tileType, int category)
    {
        Tile_U.TipoCasilla tipo = (Tile_U.TipoCasilla)tileType;
        Tile_U.Categoria cat = (Tile_U.Categoria)category;
        
        Debug.Log($"📍 Jugador {playerId} cayó en casilla: {tipo}");
        
        bool isLocalPlayer = playerId == NetworkManager.Singleton.LocalClientId;
        
        switch (tipo)
        {
            case Tile_U.TipoCasilla.Pregunta:
                if (gameManager.cartaManager != null)
                {
                    StartCoroutine(HandlePreguntaCoroutine(playerId, cat, isLocalPlayer));
                }
                break;
                
            case Tile_U.TipoCasilla.Beneficio:
                if (gameManager.cartaManager != null && isLocalPlayer)
                {
                    StartCoroutine(HandleBeneficioCoroutine(playerId));
                }
                break;
                
            case Tile_U.TipoCasilla.Penalidad:
                if (gameManager.cartaManager != null && isLocalPlayer)
                {
                    StartCoroutine(HandlePenalidadCoroutine(playerId));
                }
                break;
                
            case Tile_U.TipoCasilla.Neutral:
                if (IsServer)
                {
                    AvanzarTurnoServerRpc();
                }
                break;
        }
    }
    
    private IEnumerator HandlePreguntaCoroutine(ulong playerId, Tile_U.Categoria cat, bool isLocal)
    {
        bool? resultado = null;
        
        gameManager.cartaManager.HacerPregunta(
            cat, 
            isLocal,
            0.65f,
            (bool correcta) => resultado = correcta
        );
        
        while (resultado == null)
        {
            yield return null;
        }
        
        if (isLocal)
        {
            SubmitAnswerServerRpc(playerId, resultado.Value);
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void SubmitAnswerServerRpc(ulong playerId, bool correcta)
    {
        if (!IsServer) return;
        
        Debug.Log($"📝 Jugador {playerId} respondió: {(correcta ? "✅" : "❌")}");
        
        if (!correcta && playerObjects.TryGetValue(playerId, out MovePlayer_U player))
        {
            // Retroceder al jugador
            int pasos = player.currentIndex >= 3 ? 3 : player.currentIndex;
            RetrocederJugadorClientRpc(playerId, pasos);
        }
        else
        {
            AvanzarTurnoServerRpc();
        }
    }
    
    [ClientRpc]
    private void RetrocederJugadorClientRpc(ulong playerId, int pasos)
    {
        if (playerObjects.TryGetValue(playerId, out MovePlayer_U player))
        {
            StartCoroutine(RetrocederYAvanzarTurno(player, pasos));
        }
    }
    
    private IEnumerator RetrocederYAvanzarTurno(MovePlayer_U player, int pasos)
    {
        yield return StartCoroutine(player.Retroceder(pasos));
        
        if (IsServer)
        {
            AvanzarTurnoServerRpc();
        }
    }
    
    private IEnumerator HandleBeneficioCoroutine(ulong playerId)
    {
        if (!playerObjects.TryGetValue(playerId, out MovePlayer_U player))
        {
            yield break;
        }
        
        yield return StartCoroutine(gameManager.cartaManager.EjecutarBeneficioCasilla(player, true));
        
        if (IsServer)
        {
            AvanzarTurnoServerRpc();
        }
    }
    
    private IEnumerator HandlePenalidadCoroutine(ulong playerId)
    {
        if (!playerObjects.TryGetValue(playerId, out MovePlayer_U player))
        {
            yield break;
        }
        
        yield return StartCoroutine(gameManager.cartaManager.EjecutarPenalidadCasilla(player, true));
        
        if (IsServer)
        {
            AvanzarTurnoServerRpc();
        }
    }
    
    // ============================================
    // AVANCE DE TURNO
    // ============================================
    
    [ServerRpc(RequireOwnership = false)]
    private void AvanzarTurnoServerRpc()
    {
        if (!IsServer) return;
        
        currentTurnIndex.Value = (currentTurnIndex.Value + 1) % connectedPlayers.Count;
        StartTurnServerRpc(currentTurnIndex.Value);
    }
    
    // ============================================
    // UTILIDADES UI
    // ============================================
    
    private void EnableDiceForLocalPlayer()
    {
        if (gameManager.dadoUI != null)
        {
            gameManager.dadoUI.BloquearDado(false);
            
            // Vincular evento del dado
            gameManager.dadoUI.OnRolled = (result) =>
            {
                RollDiceServerRpc(NetworkManager.Singleton.LocalClientId);
            };
        }
    }
    
    private void DisableDiceForLocalPlayer()
    {
        if (gameManager.dadoUI != null)
        {
            gameManager.dadoUI.BloquearDado(true);
            gameManager.dadoUI.OnRolled = null;
        }
    }
}