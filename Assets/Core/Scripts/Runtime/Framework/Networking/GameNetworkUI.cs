using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Manages the network connection UI, providing controls for hosting and joining game sessions.
    /// Synchronizes UI state with the GameNetworkManager's connection state.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class GameNetworkUI : MonoBehaviour
    {
        #region Fields & Properties

        [Tooltip("The visual tree asset template for the network UI.")]
        [SerializeField] private VisualTreeAsset networkUITemplate;

        private UIDocument m_UIDocument;
        private VisualElement m_Root;
        private VisualElement m_ConnectionPanel;
        private Button m_HostButton;
        private Button m_ClientButton;

        private GameNetworkManager m_CachedManager;
        private bool m_UIHidden = false;

        /// <summary>
        /// Gets the GameNetworkManager instance, lazily cached for performance.
        /// </summary>
        private GameNetworkManager Manager
        {
            get
            {
                if (m_CachedManager == null)
                {
                    m_CachedManager = GameNetworkManager.Instance;
                }
                return m_CachedManager;
            }
        }

        #endregion

        #region Unity Methods

        /// <summary>
        /// Initializes the UI system and validates required components.
        /// </summary>
        private void Awake()
        {
            // Validate UIDocument component
            m_UIDocument = GetComponent<UIDocument>();
            if (m_UIDocument == null)
            {
                Debug.LogError("GameNetworkUI requires a UIDocument component.");
                return;
            }

            // Validate template assignment
            if (networkUITemplate == null)
            {
                Debug.LogError("Network UI Template not assigned in the Inspector.");
                return;
            }

            // Validate root element
            m_Root = m_UIDocument.rootVisualElement;
            if (m_Root == null)
            {
                Debug.LogError("UIDocument is missing a rootVisualElement.");
                return;
            }

            CreateUI();
        }

        private void Start()
        {
            // Dùng coroutine để đợi 1 frame, đảm bảo GameNetworkManager.Awake()
            // đã chạy và callbacks đã đăng ký xong trước khi gọi StartHost
            StartCoroutine(AutoStartHostRoutine());
        }

        private IEnumerator AutoStartHostRoutine()
        {
            // Đợi 1 frame cho tất cả Awake() chạy xong
            yield return null;

            if (Manager == null)
            {
                Debug.LogError("[GameNetworkUI] GameNetworkManager.Instance is null after waiting.");
                yield break;
            }

            // Nếu đã connected rồi (do DontDestroyOnLoad từ Scene trước)
            if (Manager.NetworkState.ConnectionState == GameNetworkManager.ConnectionStates.Connected)
            {
                Debug.Log("[GameNetworkUI] Đã connected từ Scene trước. Ẩn connection UI.");
                HideConnectionUI();
            }
            else
            {
                Debug.Log("[GameNetworkUI] Auto-starting Host...");
                Manager.StartHostConnection();
            }
        }

        /// <summary>
        /// Updates the UI state each frame to reflect the current network connection state.
        /// </summary>
        private void Update()
        {
            if (Manager == null) return;

            // Safety: nếu đã connected nhưng UI vẫn hiện → ẩn đi
            if (!m_UIHidden &&
                Manager.NetworkState != null &&
                Manager.NetworkState.ConnectionState == GameNetworkManager.ConnectionStates.Connected)
            {
                HideConnectionUI();
            }

            // Cập nhật UI bình thường
            if (m_Root != null && m_ConnectionPanel != null && !m_UIHidden)
            {
                UpdateUI();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Queries and caches references to UI elements, then sets up callbacks and initial state.
        /// </summary>
        private void CreateUI()
        {
            m_Root = m_UIDocument.rootVisualElement;
            m_ConnectionPanel = m_Root.Q<VisualElement>("connection-panel");
            m_HostButton = m_Root.Q<Button>("host-button");
            m_ClientButton = m_Root.Q<Button>("client-button");

            SetupUICallbacks();
            UpdateUI();
        }

        /// <summary>
        /// Registers click event handlers for the host and client buttons.
        /// </summary>
        private void SetupUICallbacks()
        {
            if (m_HostButton != null)
            {
                m_HostButton.clicked += () =>
                {
                    if (Manager != null)
                    {
                        // Toggle between disconnect and host based on current state
                        if (Manager.NetworkState.ConnectionState == GameNetworkManager.ConnectionStates.Connected)
                        {
                            Manager.Disconnect();
                        }
                        else
                        {
                            Manager.StartHostConnection();
                        }
                    }
                };
            }

            if (m_ClientButton != null)
            {
                m_ClientButton.clicked += () =>
                {
                    if (Manager != null)
                    {
                        Manager.StartClientConnection();
                    }
                };
            }
        }

        /// <summary>
        /// Updates button states and text based on the current network connection state.
        /// </summary>
        private void UpdateUI()
        {
            if (Manager?.NetworkState == null) return;
            var state = Manager.NetworkState;

            // Settings can only be changed when not connected or after connection failure
            bool canChangeSettings = state.ConnectionState == GameNetworkManager.ConnectionStates.None ||
                                     state.ConnectionState == GameNetworkManager.ConnectionStates.Failed;

            // Update host button based on connection state
            if (m_HostButton != null)
            {
                switch (state.ConnectionState)
                {
                    case GameNetworkManager.ConnectionStates.None:
                    case GameNetworkManager.ConnectionStates.Failed:
                        m_HostButton.text = "Start Host";
                        m_HostButton.SetEnabled(true);
                        break;
                    case GameNetworkManager.ConnectionStates.Connecting:
                        m_HostButton.text = "Connecting...";
                        m_HostButton.SetEnabled(false);
                        break;
                    case GameNetworkManager.ConnectionStates.Connected:
                        m_HostButton.text = "Disconnect";
                        m_HostButton.SetEnabled(true);
                        break;
                }
            }

            // Hide client button when connected, show when settings can be changed
            if (m_ClientButton != null)
            {
                bool showClientButton = canChangeSettings;
                m_ClientButton.SetEnabled(showClientButton);
                m_ClientButton.style.display = showClientButton ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>
        /// Ẩn toàn bộ connection UI khi đã connected thành công.
        /// </summary>
        private void HideConnectionUI()
        {
            m_UIHidden = true;
            Debug.Log("[GameNetworkUI] Ẩn connection UI - đã connected.");

            if (m_ConnectionPanel != null)
            {
                m_ConnectionPanel.style.display = DisplayStyle.None;
            }
            if (m_Root != null)
            {
                m_Root.style.opacity = 0f;
                m_Root.style.display = DisplayStyle.None;
            }
        }

        #endregion
    }
}
