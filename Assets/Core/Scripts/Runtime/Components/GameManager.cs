using UnityEngine;
using Unity.Netcode;
using System.Collections;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Blocks.Sessions.Common;
using Unity.Services.Multiplayer;
using System.Collections.Generic;
using Cursor = UnityEngine.Cursor;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Manages the game session, UI, and local player lifecycle rules.
    /// Provides a centralized system for handling player spawning, respawning, and session management.
    /// Supports both standard NetworkManager and Unity Multiplayer Services integration.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Fields & Properties

        /// <summary>
        /// Gets the singleton instance of the GameManager.
        /// </summary>
        public static GameManager Instance { get; private set; }

        [Tooltip("If true, uses Unity Multiplayer Services for session management. If false, uses NetworkManager callbacks.")]
        [SerializeField] private bool isUsingMultiplayerServices = false;

        [Header("Session Setup")]
        [Tooltip("Configuration settings for the multiplayer session.")]
        [SerializeField] private SessionSettings sessionSettings;

        [Tooltip("UI Document that displays session/lobby interface.")]
        [SerializeField] private UIDocument sessionUI;

        [Tooltip("Duration in seconds for the session UI fade-out animation.")]
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("Game Rules")]
        [Tooltip("Time in seconds before the local player respawns.")]
        [SerializeField] private float respawnDelay = 5.0f;

        [Tooltip("If true, the local player respawns automatically.")]
        [SerializeField] private bool autoRespawn = true;

        [Header("Spawning")]
        [Tooltip("List of Transforms to use as spawn points.")]
        [SerializeField] private List<Transform> spawnPoints;

        [Header("Events")]
        [Tooltip("Event raised when a player's stat is depleted (e.g., health reaches zero).")]
        [SerializeField] private StatDepletedEvent onStatDepleted;

        [Tooltip("Event raised to update respawn status UI (countdown timer, messages).")]
        [SerializeField] private RespawnStatusEvent onRespawnStatus;

        [Header("Sound Effects")]
        [Tooltip("Sound effect played during respawn countdown timer ticks.")]
        [SerializeField] private SoundDef respawnTimerSFX;

        private SessionObserver m_SessionObserver;
        private const string k_PlayerNameKey = "_player_name";

        #endregion

        #region Unity Methods

        /// <summary>
        /// Called when the GameManager is destroyed.
        /// Cleans up all event subscriptions and observers to prevent memory leaks.
        /// </summary>
        private void OnDestroy()
        {
            // Cleanup SceneManager callback
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // Cleanup NetworkManager callbacks
            if (GameNetworkManager.Instance != null)
            {
                GameNetworkManager.Instance.OnClientConnectedCallback -= ClientConnected;
                GameNetworkManager.Instance.OnClientDisconnectCallback -= ClientDisconnected;
            }

            // Cleanup SessionObserver
            if (m_SessionObserver != null)
            {
                m_SessionObserver.SessionAdded -= OnSessionAdded;
                m_SessionObserver.Dispose();
                m_SessionObserver = null;
            }
        }

        /// <summary>
        /// Initializes the singleton instance and sets up session observer if using Multiplayer Services.
        /// Prevents duplicate GameManager instances using the singleton pattern with DontDestroyOnLoad.
        /// </summary>
        private void Awake()
        {
            // Enforce singleton pattern - Instance CŨ được giữ lại, instance MỚI tự hủy
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[GameManager] Đã có GameManager tồn tại. Hủy instance mới, giữ instance cũ.");
                // Copy sessionUI reference từ Scene mới sang Instance cũ nếu có
                if (sessionUI != null)
                {
                    Instance.sessionUI = sessionUI;
                }
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Đăng ký sự kiện chuyển scene
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Validate required references
            if (sessionUI == null)
            {
                Debug.LogError("[GameManager] SessionUI is not assigned.", this);
            }
            if (sessionSettings == null)
            {
                Debug.LogError("[GameManager] SessionSettings is not assigned.", this);
            }
            if (sessionUI == null || sessionSettings == null)
            {
                return;
            }

            // Initialize Multiplayer Services session observer if enabled
            if (isUsingMultiplayerServices)
            {
                m_SessionObserver = new SessionObserver(sessionSettings.sessionType);
                m_SessionObserver.SessionAdded += OnSessionAdded;
            }
        }

        /// <summary>
        /// Subscribes to NetworkManager callbacks when using standard NetworkManager mode.
        /// Skipped if using Multiplayer Services.
        /// </summary>
        private void Start()
        {
            if (isUsingMultiplayerServices) return;

            // Dùng coroutine để đợi NetworkManager sẵn sàng
            StartCoroutine(WaitAndSubscribeCallbacks());
        }

        /// <summary>
        /// Coroutine đợi NetworkManager có sẵn rồi đăng ký callbacks.
        /// Dùng GameNetworkManager.Instance thay vì NetworkManager.Singleton
        /// (vì GameNetworkManager che khuất Awake() nên Singleton có thể null).
        /// </summary>
        private IEnumerator WaitAndSubscribeCallbacks()
        {
            // Đợi cho đến khi có GameNetworkManager instance
            while (GameNetworkManager.Instance == null)
            {
                yield return null;
            }

            var netManager = GameNetworkManager.Instance;

            netManager.OnClientConnectedCallback += ClientConnected;
            netManager.OnClientDisconnectCallback += ClientDisconnected;

            Debug.Log("[GameManager] Đã đăng ký callbacks với GameNetworkManager.");

            // Nếu đã connected rồi (callback bị miss), gọi ClientConnected thủ công
            if (netManager.IsConnectedClient && netManager.LocalClient != null)
            {
                Debug.Log("[GameManager] Đã connected rồi. Gọi ClientConnected thủ công.");
                ClientConnected(netManager.LocalClientId);
            }
        }

        /// <summary>
        /// Khi Scene mới được load, cập nhật spawn points và re-teleport local player.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[GameManager] Scene loaded: {scene.name}. Cập nhật spawn points.");

            // Cập nhật spawn points từ Scene mới
            UpdateSpawnPoints();

            // Nếu đã connected, re-teleport player đến spawn point mới
            if (!isUsingMultiplayerServices &&
                NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsConnectedClient &&
                NetworkManager.Singleton.LocalClient != null &&
                NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                StartCoroutine(ReTeleportPlayerAfterSceneLoad());
            }
        }

        /// <summary>
        /// Coroutine đợi 1 frame rồi teleport player đến spawn point mới.
        /// </summary>
        private IEnumerator ReTeleportPlayerAfterSceneLoad()
        {
            // Đợi vài frame để scene ổn định
            yield return new WaitForEndOfFrame();
            yield return new WaitForFixedUpdate();

            if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null)
                yield break;

            var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (localPlayer == null) yield break;

            ulong clientId = NetworkManager.Singleton.LocalClientId;

            if (localPlayer.TryGetComponent<CoreMovement>(out var movement))
            {
                // Tạm tắt vật lý
                if (localPlayer.TryGetComponent<Rigidbody>(out var rb))
                    rb.isKinematic = true;

                Vector3 spawnPos = GetSpawnPosition(clientId);
                int index = GetSpawnIndex(clientId);

                if (index >= 0 && spawnPoints != null && spawnPoints.Count > index)
                    movement.transform.rotation = spawnPoints[index].rotation;

                movement.SetPosition(spawnPos + Vector3.up * 0.5f);
                movement.ResetMovementForces();

                yield return new WaitForFixedUpdate();

                if (rb != null)
                    rb.isKinematic = false;

                Debug.Log($"[GameManager] Re-teleported player to spawn position: {spawnPos}");
            }
        }

        #endregion

        #region NetworkManager Callbacks

        /// <summary>
        /// Handles client disconnection events.
        /// Unregisters from stat depletion events when the local client disconnects.
        /// </summary>
        /// <param name="clientId">The ID of the client that disconnected.</param>
        private void ClientDisconnected(ulong clientId)
        {
            // Only process local client disconnection
            if (clientId != NetworkManager.Singleton.LocalClientId) return;

            if (onStatDepleted != null)
            {
                onStatDepleted.UnregisterListener(HandleStatDepleted);
            }
        }

        /// <summary>
        /// Handles client connection events.
        /// Sets up the local player when they connect, including registering event listeners,
        /// hiding the session UI, setting player name, spawning at initial position, and restoring health.
        /// </summary>
        /// <param name="clientId">The ID of the client that connected.</param>
        private void ClientConnected(ulong clientId)
        {
            // Chỉ xử lý khi local client kết nối
            if (clientId != NetworkManager.Singleton.LocalClientId) return;

            // Chạy Coroutine để thiết lập nhân vật an toàn
            StartCoroutine(SetupPlayerRoutine(clientId));
        }
        private IEnumerator SetupPlayerRoutine(ulong clientId)
        {
            // 1. Đăng ký sự kiện (giữ nguyên logic cũ)
            if (onStatDepleted != null)
                onStatDepleted.RegisterListener(HandleStatDepleted);

            // 2. UI và Cursor
            StartCoroutine(FadeOutAndDisable());
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Đợi 2 frame để đảm bảo map và vật lý đã sẵn sàng
            yield return new WaitForEndOfFrame();
            yield return new WaitForFixedUpdate();

            if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null) yield break;

            var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (localPlayer == null) yield break;

            // 3. Đặt tên (giữ nguyên logic cũ)
            var playerState = localPlayer.GetComponent<CorePlayerState>();
            if (playerState != null)
                playerState.SetPlayerName("Player" + clientId);

            // 4. THIẾT LẬP VỊ TRÍ AN TOÀN (Sửa lỗi rơi map ở đây)
            if (localPlayer.TryGetComponent<CoreMovement>(out var movement))
            {
                // Tạm thời tắt vật lý để dịch chuyển không bị lỗi
                if (localPlayer.TryGetComponent<Rigidbody>(out var rb))
                    rb.isKinematic = true;

                Vector3 spawnPos = GetSpawnPosition(clientId);
                int index = GetSpawnIndex(clientId);

                if (index >= 0 && spawnPoints != null && spawnPoints.Count > index)
                    movement.transform.rotation = spawnPoints[index].rotation;

                // Đưa nhân vật lên cao hơn điểm spawn 0.5m để tránh dính vào sàn
                movement.SetPosition(spawnPos + Vector3.up * 0.5f);
                movement.ResetMovementForces();

                // Đợi thêm 1 frame vật lý nữa rồi mới bật lại trọng lực
                yield return new WaitForFixedUpdate();

                if (rb != null)
                    rb.isKinematic = false;
            }

            // 5. Hồi máu (giữ nguyên logic cũ)
            if (localPlayer.TryGetComponent<CoreStatsHandler>(out var coreStats))
                coreStats.ModifyStat(StatKeys.Health, 100, clientId, ModificationSource.Regeneration);
        }

        #endregion

        #region Multiplayer Services Callbacks

        /// <summary>
        /// Handles session added events from Unity Multiplayer Services.
        /// Performs the same player setup as <see cref="ClientConnected"/> but for Multiplayer Services workflow.
        /// </summary>
        /// <param name="session">The session that was added.</param>
        private void OnSessionAdded(ISession session)
        {
            if (session == null)
            {
                Debug.LogError("[GameManager] OnSessionAdded called with null session.", this);
                return;
            }

            // Subscribe to session removal events
            session.RemovedFromSession += OnRemovedFromSession;

            // Register for stat depletion events
            if (onStatDepleted != null)
            {
                onStatDepleted.RegisterListener(HandleStatDepleted);
            }

            // Hide session UI and lock cursor for gameplay
            StartCoroutine(FadeOutAndDisable());
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Set player name from session properties
            SetupLocalPlayerName(session);

            if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null)
            {
                Debug.LogWarning("[GameManager] NetworkManager.Singleton or LocalClient is null during session setup.", this);
                return;
            }

            var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (localPlayer == null)
            {
                Debug.LogWarning("[GameManager] LocalClient.PlayerObject is null. Cannot setup player.", this);
                return;
            }

            // Set spawn position and rotation based on client ID
            if (localPlayer.TryGetComponent<CoreMovement>(out var movement))
            {
                Vector3 spawnPos = GetSpawnPosition(NetworkManager.Singleton.LocalClientId);
                int index = GetSpawnIndex(NetworkManager.Singleton.LocalClientId);
                if (index >= 0 && spawnPoints != null && spawnPoints.Count > index)
                {
                    movement.transform.rotation = spawnPoints[index].rotation;
                }

                movement.SetPosition(spawnPos);
                movement.ResetMovementForces();
            }
            else
            {
                Debug.LogWarning("[GameManager] CoreMovement component not found on local player. Cannot set spawn position.", this);
            }

            // Restore full health on spawn
            if (localPlayer.TryGetComponent<CoreStatsHandler>(out var coreStats))
            {
                coreStats.ModifyStat(StatKeys.Health, 100, NetworkManager.Singleton.LocalClientId, ModificationSource.Regeneration);
            }
            else
            {
                Debug.LogWarning("[GameManager] CoreStatsHandler component not found on local player. Cannot restore health.", this);
            }
        }

        /// <summary>
        /// Handles removal from a Multiplayer Services session.
        /// Unregisters from stat depletion events when removed from the session.
        /// </summary>
        private void OnRemovedFromSession()
        {
            if (onStatDepleted != null)
            {
                onStatDepleted.UnregisterListener(HandleStatDepleted);
            }
        }

        #endregion

        #region Player Lifecycle

        /// <summary>
        /// Handles death logic for the local player when health is depleted.
        /// Sets player state to Eliminated and initiates respawn routine if auto-respawn is enabled.
        /// Respects <see cref="CorePlayerManager.AutoHandleLifecycle"/> to avoid duplicate lifecycle management.
        /// </summary>
        /// <param name="payload">The stat depletion event payload containing player ID and stat information.</param>
        private void HandleStatDepleted(StatDepletedPayload payload)
        {
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null)
            {
                Debug.LogWarning("[GameManager] Cannot handle stat depletion: NetworkManager.Singleton or LocalClient is null.", this);
                return;
            }

            // Only handle local player's stat depletion
            if (payload.playerId != NetworkManager.Singleton.LocalClientId) return;

            // Only process health depletion (death)
            if (payload.statID == StatKeys.Health)
            {
                var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
                if (localPlayer == null)
                {
                    Debug.LogWarning("[GameManager] Cannot handle player death: LocalClient.PlayerObject is null.", this);
                    return;
                }

                if (localPlayer.TryGetComponent<CorePlayerState>(out var playerState))
                {
                    // Only set state if CorePlayerManager isn't handling lifecycle automatically
                    // This prevents duplicate lifecycle management
                    if (localPlayer.TryGetComponent<CorePlayerManager>(out var playerManager))
                    {
                        if (!playerManager.AutoHandleLifecycle)
                        {
                            playerState.SetLifeState(PlayerLifeState.Eliminated);
                        }
                    }
                    else
                    {
                        // No CorePlayerManager, handle it ourselves
                        playerState.SetLifeState(PlayerLifeState.Eliminated);
                    }

                    // Start respawn countdown if auto-respawn is enabled
                    if (autoRespawn)
                    {
                        StartCoroutine(RespawnRoutine(playerState));
                    }
                }
                else
                {
                    Debug.LogWarning("[GameManager] CorePlayerState component not found on local player. Cannot handle death.", this);
                }
            }
        }

        /// <summary>
        /// Coroutine that handles the respawn countdown and player revival.
        /// Displays countdown UI, then respawns the player at a random spawn point with full health.
        /// </summary>
        /// <param name="playerState">The player state component to respawn.</param>
        /// <returns>Enumerator for coroutine execution.</returns>
        private IEnumerator RespawnRoutine(CorePlayerState playerState)
        {
            if (playerState == null)
            {
                Debug.LogError("[GameManager] RespawnRoutine called with null playerState.", this);
                yield break;
            }

            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[GameManager] Cannot respawn: NetworkManager.Singleton is null.", this);
                yield break;
            }

            float timer = respawnDelay;
            ulong localId = NetworkManager.Singleton.LocalClientId;

            // Countdown loop: update UI every second
            while (timer > 0)
            {
                if (onRespawnStatus != null)
                {
                    onRespawnStatus.Raise(new RespawnStatusPayload { playerId = localId, message = "RESPAWNING IN", subtext = Mathf.CeilToInt(timer).ToString(), showSubtext = true });
                    PlayRespawnTimerSFX();
                }

                yield return new WaitForSeconds(1.0f);
                timer -= 1.0f;
            }

            // Clear respawn UI
            if (onRespawnStatus != null)
            {
                onRespawnStatus.Raise(new RespawnStatusPayload { playerId = localId, message = "", subtext = "", showSubtext = false });
            }

            // Respawn at random spawn point (different from initial spawn which uses client ID modulo)
            var coreMovement = playerState.GetComponent<CoreMovement>();
            if (coreMovement != null)
            {
                int spawnIndex = GetRandomSpawnIndex();
                if (spawnIndex >= 0 && spawnPoints != null && spawnPoints.Count > spawnIndex)
                {
                    coreMovement.transform.rotation = spawnPoints[spawnIndex].rotation;
                    coreMovement.SetPosition(spawnPoints[spawnIndex].position);
                }
                else
                {
                    coreMovement.SetPosition(Vector3.zero);
                }

                coreMovement.ResetMovementForces();
                PlayRespawnTimerSFX();
            }
            else
            {
                Debug.LogWarning("[GameManager] CoreMovement component not found during respawn. Cannot reposition player.", this);
            }

            // Restore full health
            var coreStats = playerState.GetComponent<CoreStatsHandler>();
            if (coreStats != null)
            {
                coreStats.ModifyStat(StatKeys.Health, 100, localId, ModificationSource.Regeneration);
            }
            else
            {
                Debug.LogWarning("[GameManager] CoreStatsHandler component not found during respawn. Cannot restore health.", this);
            }

            // Update player state to respawned
            playerState.SetLifeState(PlayerLifeState.Respawned);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Plays the respawn timer sound effect locally (not networked).
        /// Uses spatial blend of 0 in the SoundDef, so position has no effect on panning or attenuation.
        /// </summary>
        private void PlayRespawnTimerSFX()
        {
            if (respawnTimerSFX != null)
            {
                CoreDirector.RequestAudio(respawnTimerSFX)
                     .WithPosition(Vector3.zero)
                     .Play();
            }
        }

        /// <summary>
        /// Calculates the spawn index based on ClientID modulo spawn point count.
        /// Used for initial spawning to distribute players evenly across spawn points.
        /// </summary>
        /// <param name="clientId">The client ID to calculate the spawn index for.</param>
        /// <returns>The spawn point index, or -1 if no spawn points are configured.</returns>
        private int GetSpawnIndex(ulong clientId)
        {
            if (spawnPoints == null || spawnPoints.Count == 0) return -1;
            return (int)(clientId % (ulong)spawnPoints.Count);
        }

        /// <summary>
        /// Returns a random spawn index for respawning.
        /// Used for respawning to add variety and prevent spawn camping.
        /// </summary>
        /// <returns>A random spawn point index, or -1 if no spawn points are configured.</returns>
        private int GetRandomSpawnIndex()
        {
            if (spawnPoints == null || spawnPoints.Count == 0) return -1;
            return Random.Range(0, spawnPoints.Count);
        }

        /// <summary>
        /// Returns the world position for the given ClientID's assigned spawn point.
        /// Defaults to Vector3.zero if no spawn points are set.
        /// </summary>
        /// <param name="clientId">The client ID to get the spawn position for.</param>
        /// <returns>The world position of the assigned spawn point.</returns>
        private Vector3 GetSpawnPosition(ulong clientId)
        {
            UpdateSpawnPoints();
            int index = GetSpawnIndex(clientId);
            if (index == -1)
            {
                Debug.LogWarning("[GameManager] No spawn points configured. Using Vector3.zero as spawn position.", this);
                return Vector3.zero;
            }

            if (spawnPoints[index] == null)
            {
                Debug.LogError($"[GameManager] Spawn point at index {index} is null. Using Vector3.zero as spawn position.", this);
                return Vector3.zero;
            }

            return spawnPoints[index].position;
        }
        private void UpdateSpawnPoints()
        {
            // Tìm tất cả các object có Tag là "SpawnPoint" trong Scene hiện tại
            var points = GameObject.FindGameObjectsWithTag("SpawnPoint");
            spawnPoints.Clear();
            foreach (var p in points)
            {
                spawnPoints.Add(p.transform);
            }
        }
        /// <summary>
        /// Sets up the local player's name from the Multiplayer Services session properties.
        /// Retrieves the player name from session data and assigns it to <see cref="CorePlayerState"/>.
        /// </summary>
        /// <param name="session">The active multiplayer session.</param>
        private void SetupLocalPlayerName(ISession session)
        {
            if (session == null || session.CurrentPlayer == null)
            {
                Debug.LogWarning("[GameManager] Cannot setup player name: session or CurrentPlayer is null.", this);
                return;
            }

            var player = session.CurrentPlayer;
            string playerName = "Player";
            if (player.Properties.TryGetValue(k_PlayerNameKey, out var nameProp))
            {
                playerName = nameProp.Value;
            }

            if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null)
            {
                Debug.LogWarning("[GameManager] Cannot setup player name: NetworkManager.Singleton or LocalClient is null.", this);
                return;
            }

            if (NetworkManager.Singleton.LocalClient.PlayerObject == null)
            {
                Debug.LogWarning("[GameManager] Cannot setup player name: LocalClient.PlayerObject is null.", this);
                return;
            }

            var playerState = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<CorePlayerState>();
            if (playerState != null)
            {
                playerState.SetPlayerName(playerName);
            }
            else
            {
                Debug.LogWarning("[GameManager] CorePlayerState component not found on local player. Cannot set player name.", this);
            }
        }

        /// <summary>
        /// Coroutine that smoothly fades out and hides the session UI.
        /// Interpolates opacity from current value to 0 over the fade duration, then hides the UI completely.
        /// </summary>
        /// <returns>Enumerator for coroutine execution.</returns>
        private IEnumerator FadeOutAndDisable()
        {
            if (sessionUI == null) yield break;

            VisualElement root = sessionUI.rootVisualElement;
            float startOpacity = root.resolvedStyle.opacity;

            // Handle edge case where opacity is already near zero
            if (startOpacity < 0.01f) startOpacity = 1f;

            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / fadeDuration);
                root.style.opacity = Mathf.Lerp(startOpacity, 0f, t);
                yield return null;
            }

            // Ensure fully faded and hidden
            root.style.opacity = 0f;
            root.style.display = DisplayStyle.None;
        }

        #endregion
    }
}
