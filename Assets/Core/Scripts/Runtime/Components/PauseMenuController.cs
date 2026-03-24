using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;
using Cursor = UnityEngine.Cursor;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Controller cho Pause Menu - xử lý tạm dừng game, lưu game, và quay về menu chính.
    /// Attach script này vào một GameObject trong scene gameplay (cùng với UIDocument).
    /// Nhấn ESC để mở/đóng Pause Menu.
    /// 
    /// LƯU Ý:
    /// - Script này dùng DontDestroyOnLoad, chỉ cần đặt ở Scene gameplay đầu tiên.
    /// - Nếu cả 2 scene gameplay đều có PauseMenuController, instance mới sẽ tự hủy.
    /// - Khi quay về Menu, sẽ tự disconnect NetworkManager.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PauseMenuController : MonoBehaviour
    {
        #region Fields

        [Header("Scene Configuration")]
        [Tooltip("Tên scene Menu Chính để quay về. Phải trùng với tên trong Build Settings.")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Save Settings")]
        [Tooltip("Hiện thông báo lưu thành công trong bao lâu (giây).")]
        [SerializeField] private float saveMessageDuration = 2.0f;

        // Singleton
        public static PauseMenuController Instance { get; private set; }

        // UI References
        private UIDocument m_UIDocument;
        private VisualElement m_PauseOverlay;
        private Button m_ResumeButton;
        private Button m_MenuButton;
        private Label m_SaveStatus;

        // State
        private bool m_IsPaused = false;
        private Coroutine m_SaveStatusCoroutine;

        // Lưu trạng thái cursor trước khi pause
        private CursorLockMode m_PreviousCursorLockState;
        private bool m_PreviousCursorVisible;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Debug.Log("[PauseMenu] Đã có PauseMenuController. Hủy instance mới.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Đăng ký event chuyển scene để re-bind UI
            SceneManager.sceneLoaded += OnSceneLoaded;

            SetupUI();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Instance = null;
            }
        }

        private void Update()
        {
            // Nhấn ENTER để toggle pause menu (dùng New Input System)
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.enterKey.wasPressedThisFrame)
            {
                // Không cho pause ở scene menu/intro/loading
                if (IsInGameplayScene())
                {
                    TogglePause();
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Mở hoặc đóng Pause Menu.
        /// </summary>
        public void TogglePause()
        {
            if (m_IsPaused)
                ResumeGame();
            else
                PauseGame();
        }

        /// <summary>
        /// Tạm dừng game và hiện Pause Menu.
        /// </summary>
        public void PauseGame()
        {
            if (m_IsPaused) return;
            m_IsPaused = true;

            // Hiện UI
            if (m_PauseOverlay != null)
            {
                m_PauseOverlay.style.display = DisplayStyle.Flex;
            }

            // Lưu trạng thái cursor hiện tại
            m_PreviousCursorLockState = Cursor.lockState;
            m_PreviousCursorVisible = Cursor.visible;

            // Hiện cursor để bấm nút
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Dừng thời gian game (không ảnh hưởng UI)
            Time.timeScale = 0f;

            // Xóa thông báo save cũ
            ClearSaveStatus();

            // Thực hiện tự động lưu game ngay khi pause
            SaveGame();

            Debug.Log("[PauseMenu] Game đã tạm dừng.");
        }

        /// <summary>
        /// Tiếp tục game và ẩn Pause Menu.
        /// </summary>
        public void ResumeGame()
        {
            if (!m_IsPaused) return;
            m_IsPaused = false;

            // Ẩn UI
            if (m_PauseOverlay != null)
            {
                m_PauseOverlay.style.display = DisplayStyle.None;
            }

            // Khôi phục cursor về trạng thái trước
            Cursor.lockState = m_PreviousCursorLockState;
            Cursor.visible = m_PreviousCursorVisible;

            // Tiếp tục thời gian game
            Time.timeScale = 1f;

            Debug.Log("[PauseMenu] Game tiếp tục.");
        }

        /// <summary>
        /// Lưu game hiện tại vào PlayerPrefs.
        /// Lưu: tên scene hiện tại, vị trí player, máu hiện tại.
        /// </summary>
        public void SaveGame()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            PlayerPrefs.SetString("SavedScene", currentScene);

            // Tìm player object
            var playerObj = FindLocalPlayer();
            if (playerObj != null)
            {
                // Lưu vị trí
                Vector3 pos = playerObj.transform.position;
                PlayerPrefs.SetFloat("SavedPosX", pos.x);
                PlayerPrefs.SetFloat("SavedPosY", pos.y);
                PlayerPrefs.SetFloat("SavedPosZ", pos.z);

                // Lưu rotation Y
                PlayerPrefs.SetFloat("SavedRotY", playerObj.transform.eulerAngles.y);

                // Lưu máu nếu có CoreStatsHandler
                if (playerObj.TryGetComponent<CoreStatsHandler>(out var stats))
                {
                    float health = stats.GetCurrentValue(StatKeys.Health);
                    float maxHealth = stats.GetMaxValue(StatKeys.Health);
                    PlayerPrefs.SetFloat("SavedHealth", health);
                    PlayerPrefs.SetFloat("SavedMaxHealth", maxHealth);
                }

               // Debug.Log($"[PauseMenu] Đã lưu game: Scene={currentScene}, Pos={pos}");
            }
            else
            {
                Debug.LogWarning("[PauseMenu] Không tìm thấy player để lưu vị trí.");
            }

            // Lưu timestamp
            PlayerPrefs.SetString("SavedTime", System.DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
            PlayerPrefs.SetInt("HasSaveData", 1);
            PlayerPrefs.Save();

            // Lưu quá trình nhiệm vụ nếu đang ở scene Làng
            if (VillageQuestManager.Instance != null)
            {
                VillageQuestManager.Instance.SaveQuestProgress();
            }

            // Hiện thông báo
            ShowSaveStatus("✓ Đã lưu game thành công!");
        }

        /// <summary>
        /// Quay về Menu Chính.
        /// Disconnect network, reset time scale, load scene menu.
        /// </summary>
        public void BackToMainMenu()
        {
            if (m_IsPaused)
            {
                // Gọi Coroutine từ root GameObject hoặc GameObject không bị disable
                StartCoroutine(BackToMainMenuRoutine());
            }
        }

        private IEnumerator BackToMainMenuRoutine()
        {
            Debug.Log("[PauseMenu] Đang quay về Menu Chính...");

            // Khôi phục time scale ngay để coroutine không bị treo (nếu dùng yield return null)
            Time.timeScale = 1f;
            m_IsPaused = false;

            // Chờ 1 frame để UI processing thoát ra
            yield return null;

            // Khôi phục cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Disconnect network nếu đang connected
            if (GameNetworkManager.Instance != null && Unity.Netcode.NetworkManager.Singleton != null)
            {
                GameNetworkManager.Instance.Disconnect();
                
                // QUAN TRỌNG: Đợi Netcode for GameObjects shutdown hoàn toàn 
                // và dọn dẹp các NetworkObject trước khi Destroy NetworkManager
                // (tránh lỗi MissingReferenceException từ Unity.Multiplayer.Tools)
                yield return new WaitForSeconds(0.2f);
            }

            // Hủy các DontDestroyOnLoad objects (ngay cả chính PauseMenuController này)
            CleanupDontDestroyOnLoadObjects();

            // Load scene Menu Chính
            SceneManager.LoadScene(mainMenuSceneName);
        }

        /// <summary>
        /// Kiểm tra có save data không.
        /// </summary>
        public static bool HasSaveData()
        {
            return PlayerPrefs.GetInt("HasSaveData", 0) == 1;
        }

        /// <summary>
        /// Đọc tên scene đã save.
        /// </summary>
        public static string GetSavedSceneName()
        {
            return PlayerPrefs.GetString("SavedScene", "");
        }

        /// <summary>
        /// Load save data: chuyển scene và đặt player vào vị trí đã lưu.
        /// Gọi từ MainMenu khi người chơi nhấn "Tiếp tục".
        /// </summary>
        public static void LoadSavedGame()
        {
            if (!HasSaveData())
            {
                Debug.LogWarning("[PauseMenu] Không có save data để load.");
                return;
            }

            string savedScene = GetSavedSceneName();
            if (string.IsNullOrEmpty(savedScene))
            {
                Debug.LogError("[PauseMenu] Tên scene đã lưu bị rỗng.");
                return;
            }

            // Đánh dấu cần restore vị trí sau khi load scene
            PlayerPrefs.SetInt("NeedRestorePosition", 1);
            PlayerPrefs.Save();

            SceneManager.LoadScene(savedScene);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Setup UI references và callbacks.
        /// </summary>
        private void SetupUI()
        {
            m_UIDocument = GetComponent<UIDocument>();
            if (m_UIDocument == null)
            {
                Debug.LogError("[PauseMenu] Không tìm thấy UIDocument component.");
                return;
            }

            var root = m_UIDocument.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("[PauseMenu] rootVisualElement is null.");
                return;
            }

            BindUIElements(root);
        }

        /// <summary>
        /// Bind các UI elements và đăng ký callbacks.
        /// </summary>
        private void BindUIElements(VisualElement root)
        {
            m_PauseOverlay = root.Q<VisualElement>("pause-overlay");
            m_ResumeButton = root.Q<Button>("resume-button");
            m_MenuButton = root.Q<Button>("menu-button");
            m_SaveStatus = root.Q<Label>("save-status");

            // Đăng ký callbacks
            if (m_ResumeButton != null)
                m_ResumeButton.clicked += ResumeGame;

            if (m_MenuButton != null)
                m_MenuButton.clicked += BackToMainMenu;

            // Ẩn pause overlay mặc định
            if (m_PauseOverlay != null)
                m_PauseOverlay.style.display = DisplayStyle.None;

            ClearSaveStatus();
        }

        /// <summary>
        /// Khi scene mới được load, kiểm tra có cần restore position không.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Nếu đang ở scene menu thì ẩn pause overlay
            if (!IsInGameplayScene())
            {
                if (m_PauseOverlay != null)
                    m_PauseOverlay.style.display = DisplayStyle.None;
                m_IsPaused = false;
                Time.timeScale = 1f;
                return;
            }

            // Kiểm tra có cần restore vị trí không
            if (PlayerPrefs.GetInt("NeedRestorePosition", 0) == 1)
            {
                PlayerPrefs.SetInt("NeedRestorePosition", 0);
                PlayerPrefs.Save();
                
                // Restore nhiệm vụ
                if (VillageQuestManager.Instance != null)
                {
                    VillageQuestManager.Instance.LoadQuestProgress();
                }

                StartCoroutine(RestorePlayerPosition());
            }
        }

        /// <summary>
        /// Coroutine đợi player spawn rồi restore vị trí từ save data.
        /// </summary>
        private IEnumerator RestorePlayerPosition()
        {
            float timeout = 5f;
            GameObject playerObj = null;

            // Đợi player spawn thay vì chỉ đợi 10 frame do độ trễ của Netcode Multiplayer
            while (timeout > 0f)
            {
                playerObj = FindLocalPlayer();
                if (playerObj != null) break;

                yield return null;
                timeout -= Time.deltaTime;
            }

            if (playerObj == null)
            {
                Debug.LogWarning("[PauseMenu] Không tìm thấy player để restore vị trí.");
                yield break;
            }

            // Đợi thêm vài frame để GameManager teleport về default spawn point xong, rôi đè lại bằng Saved Pos
            for(int i = 0; i < 5; i++)
            {
                yield return null;
            }
            yield return new WaitForFixedUpdate();

            // Đọc vị trí đã lưu
            float x = PlayerPrefs.GetFloat("SavedPosX", 0);
            float y = PlayerPrefs.GetFloat("SavedPosY", 0);
            float z = PlayerPrefs.GetFloat("SavedPosZ", 0);
            float rotY = PlayerPrefs.GetFloat("SavedRotY", 0);

            Vector3 savedPos = new Vector3(x, y, z);

            // Di chuyển player
            if (playerObj.TryGetComponent<CoreMovement>(out var movement))
            {
                // Tắt vật lý tạm
                if (playerObj.TryGetComponent<Rigidbody>(out var rb))
                    rb.isKinematic = true;

                movement.SetPosition(savedPos + Vector3.up * 0.5f);
                movement.ResetMovementForces();
                playerObj.transform.rotation = Quaternion.Euler(0, rotY, 0);

                yield return new WaitForFixedUpdate();

                if (rb != null)
                    rb.isKinematic = false;
            }
            else
            {
                playerObj.transform.position = savedPos;
                playerObj.transform.rotation = Quaternion.Euler(0, rotY, 0);
            }

            // Restore máu
            float savedHealth = PlayerPrefs.GetFloat("SavedHealth", -1);
            if (savedHealth >= 0 && playerObj.TryGetComponent<CoreStatsHandler>(out var stats))
            {
                // Hồi máu về giá trị đã lưu
                float currentHealth = stats.GetCurrentValue(StatKeys.Health);
                float diff = savedHealth - currentHealth;
                if (Mathf.Abs(diff) > 0.1f)
                {
                    ulong clientId = 0;
                    if (GameNetworkManager.Instance != null)
                        clientId = GameNetworkManager.Instance.LocalClientId;
                    stats.ModifyStat(StatKeys.Health, diff, clientId, ModificationSource.Regeneration);
                }
            }

            Debug.Log($"[PauseMenu] Đã restore vị trí player: {savedPos}");

            // Cập nhật lại các vật thể trong Quest (Con diều, Model trên Player...)
            if (VillageQuestManager.Instance != null)
            {
                VillageQuestManager.Instance.FastForwardQuestsToCurrentStep(playerObj);
            }
        }

        /// <summary>
        /// Tìm local player object.
        /// </summary>
        private GameObject FindLocalPlayer()
        {
            // Thử tìm qua NetworkManager trước
            if (GameNetworkManager.Instance != null &&
                GameNetworkManager.Instance.IsConnectedClient &&
                GameNetworkManager.Instance.LocalClient != null &&
                GameNetworkManager.Instance.LocalClient.PlayerObject != null)
            {
                return GameNetworkManager.Instance.LocalClient.PlayerObject.gameObject;
            }

            // Fallback: tìm qua Tag
            var player = GameObject.FindGameObjectWithTag("Player");
            return player;
        }

        /// <summary>
        /// Kiểm tra có đang ở scene gameplay không (không phải menu/intro/loading).
        /// </summary>
        private bool IsInGameplayScene()
        {
            string sceneName = SceneManager.GetActiveScene().name.ToLower();
            // Danh sách các scene KHÔNG phải gameplay
            return !sceneName.Contains("menu") &&
                   !sceneName.Contains("intro") &&
                   !sceneName.Contains("loading") &&
                   !sceneName.Contains("credit") &&
                   !sceneName.Contains("mainmenu");
        }

        /// <summary>
        /// Dọn dẹp các DontDestroyOnLoad objects khi quay về menu.
        /// </summary>
        private void CleanupDontDestroyOnLoadObjects()
        {
            // Hủy GameManager
            if (GameManager.Instance != null)
            {
                Destroy(GameManager.Instance.gameObject);
            }

            // Hủy GameNetworkManager
            if (GameNetworkManager.Instance != null)
            {
                Destroy(GameNetworkManager.Instance.gameObject);
            }

            // Hủy chính mình
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Instance = null;
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Hiện thông báo trạng thái save.
        /// </summary>
        private void ShowSaveStatus(string message)
        {
            if (m_SaveStatus == null) return;

            m_SaveStatus.text = message;

            // Tự xóa sau vài giây
            if (m_SaveStatusCoroutine != null)
                StopCoroutine(m_SaveStatusCoroutine);
            m_SaveStatusCoroutine = StartCoroutine(ClearSaveStatusAfterDelay());
        }

        private IEnumerator ClearSaveStatusAfterDelay()
        {
            // Dùng WaitForSecondsRealtime vì TimeScale = 0 khi pause
            yield return new WaitForSecondsRealtime(saveMessageDuration);
            ClearSaveStatus();
        }

        private void ClearSaveStatus()
        {
            if (m_SaveStatus != null)
                m_SaveStatus.text = "";
        }

        #endregion
    }
}
