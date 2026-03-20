using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Manages the village quest with SEQUENTIAL steps.
    /// Each quest step must be completed in order before the next NPC becomes available.
    /// After all steps: Gióng transforms and transitions to Map1 for battle.
    /// </summary>
    public class VillageQuestManager : MonoBehaviour
    {
        #region Singleton

        public static VillageQuestManager Instance { get; private set; }

        #endregion

        #region Fields

        [Header("Quest Settings")]
        [Tooltip("List of all VillagerNPC components in order they must be visited.")]
        [SerializeField] private List<VillagerNPC> allVillagers = new List<VillagerNPC>();

        [Header("Transformation Settings")]
        [Tooltip("Scene name to load after transformation (Map1).")]
        [SerializeField] private string battleSceneName = "Map1";

        [Tooltip("Time to wait during transformation sequence.")]
        [SerializeField] private float transformationDuration = 4f;

        [Header("UI Documents")]
        [Tooltip("UIDocument for the quest tracker HUD.")]
        [SerializeField] private UIDocument questTrackerUIDocument;

        [Tooltip("UIDocument for the transformation/win screen.")]
        [SerializeField] private UIDocument winScreenUIDocument;

        [Header("References")]
        [Tooltip("Reference to the DialogueUI for showing notifications.")]
        [SerializeField] private DialogueUI dialogueUI;

        // Internal tracking
        private bool m_IsTrackerVisible = false;
        private int m_CurrentQuestStep = 0;
        private HashSet<string> m_CompletedVillagers = new HashSet<string>();
        private bool m_QuestCompleted;

        // Quest step descriptions
        private string[] m_StepDescriptions = new string[]
        {
            "Nói chuyện với Mẹ",
            "Nói chuyện với Sứ Giả",
            "Tìm gặp Thầy Ông Nội",
            "Gặp Anh Thanh niên nhận cơm",
            "Gặp Bé Gái nhận đồ ăn",
            "Gặp Bé Trai nhận đồ ăn",
        };

        // UI elements
        private VisualElement m_QuestTrackerRoot;
        private VisualElement m_QuestTrackerContainer;
        private Label m_QuestTitleLabel;
        private Label m_QuestStepLabel;
        private Label m_QuestProgressLabel;
        private VisualElement m_StepListContainer;
        private List<VisualElement> m_StepRows = new List<VisualElement>();
        private List<Label> m_StepIcons = new List<Label>();
        private List<Label> m_StepLabels = new List<Label>();

        private VisualElement m_WinScreenRoot;

        #endregion

        #region Properties

        public int TotalSteps => allVillagers.Count;
        public int CurrentStep => m_CurrentQuestStep;
        public int CompletedCount => m_CompletedVillagers.Count;
        public bool IsQuestCompleted => m_QuestCompleted;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Auto-find villagers if list is empty
            if (allVillagers.Count == 0)
            {
                allVillagers.AddRange(FindObjectsOfType<VillagerNPC>());
                // Sort by questStep
                allVillagers.Sort((a, b) => a.QuestStep.CompareTo(b.QuestStep));
                Debug.Log($"[QuestManager] Auto-found {allVillagers.Count} villagers.");
            }

            // Update step descriptions from actual villager names
            if (allVillagers.Count > 0)
            {
                m_StepDescriptions = new string[allVillagers.Count];
                for (int i = 0; i < allVillagers.Count; i++)
                {
                    m_StepDescriptions[i] = $"Nói chuyện với {allVillagers[i].VillagerName}";
                }
            }

            StartCoroutine(InitializeUI());
        }
      

private void Update()
    {
        // Kiểm tra nếu phím Tab đang được GIỮ (Hold)
        if (Keyboard.current != null && Keyboard.current.tabKey.isPressed)
        {
            if (m_QuestTrackerContainer != null && m_QuestTrackerContainer.style.display == DisplayStyle.None)
            {
                m_QuestTrackerContainer.style.display = DisplayStyle.Flex;
                UpdateQuestTrackerUI();
            }
        }
        else // Nếu KHÔNG nhấn hoặc THẢ ra
        {
            if (m_QuestTrackerContainer != null && m_QuestTrackerContainer.style.display == DisplayStyle.Flex)
            {
                m_QuestTrackerContainer.style.display = DisplayStyle.None;
            }
        }
    }
    
        #endregion

        #region Public Methods

        /// <summary>
        /// Returns the current quest step index (0-based).
        /// VillagerNPC checks this to know if it should activate.
        /// </summary>
        public int GetCurrentQuestStep()
        {
            return m_CurrentQuestStep;
        }

        /// <summary>
        /// Called by VillagerNPC when dialogue is completed.
        /// Advances to the next quest step.
        /// </summary>
        public void OnVillagerDialogueCompleted(string villagerName)
        {
            if (m_QuestCompleted) return;

            if (m_CompletedVillagers.Add(villagerName))
            {
                Debug.Log($"[QuestManager] Step {m_CurrentQuestStep + 1}/{TotalSteps} completed: {villagerName}");

                m_CurrentQuestStep++;

                // Update UI
                UpdateQuestTrackerUI();

                // Show notification
                if (dialogueUI != null)
                {
                    if (m_CurrentQuestStep >= TotalSteps)
                    {
                        dialogueUI.ShowQuestNotification("★ Tất cả dân làng đã giúp đỡ Gióng!", 4f);
                    }
                    else
                    {
                        string nextStep = m_CurrentQuestStep < m_StepDescriptions.Length
                            ? m_StepDescriptions[m_CurrentQuestStep]
                            : "???";
                        dialogueUI.ShowQuestNotification(
                            $"✓ Hoàn thành! Tiếp theo: {nextStep}",
                            3f);
                    }
                }
                if (BGMManager.Instance != null)
                {
                    BGMManager.Instance.RestoreBGMWithDelay();
                }
                // Check if all steps done
                if (m_CurrentQuestStep >= TotalSteps)
                {
                    StartCoroutine(TransformationSequence());
                }
            }
        }

        #endregion

        #region Transformation Sequence

        /// <summary>
        /// The transformation cutscene: Gióng grows up, puts on armor, transitions to battle.
        /// </summary>
        private IEnumerator TransformationSequence()
        {
            m_QuestCompleted = true;
            yield return new WaitForSeconds(1.5f);

            // Hiện màn hình thông báo biến hình
            if (winScreenUIDocument != null)
            {
                BuildTransformationUI();
            }

            yield return new WaitForSeconds(transformationDuration);

            // Chuyển sang Map1 (Unity sẽ tự xóa sạch Object Map cũ)
            Debug.Log($"[QuestManager] Đang nạp Map mới: {battleSceneName}");
            SceneManager.LoadScene(battleSceneName);
        }

        #endregion

        #region UI

        private IEnumerator InitializeUI()
        {
            yield return null;

            if (questTrackerUIDocument != null)
            {
                m_QuestTrackerRoot = questTrackerUIDocument.rootVisualElement;
                BuildQuestTrackerUI();

                // Sau khi build xong, ẩn nó đi luôn
                if (m_QuestTrackerContainer != null)
                {
                    m_QuestTrackerContainer.style.display = DisplayStyle.None;
                    m_IsTrackerVisible = false;
                }
            }
        }
        public void RefreshQuestUI()
        {
            if (m_QuestTrackerRoot == null && questTrackerUIDocument != null)
            {
                m_QuestTrackerRoot = questTrackerUIDocument.rootVisualElement;
            }

            if (m_QuestTrackerRoot != null)
            {
                // Xóa cái cũ đi để vẽ cái mới, tránh bị chồng đè
                m_QuestTrackerRoot.Clear();
                m_StepRows.Clear();
                m_StepIcons.Clear();
                m_StepLabels.Clear();

                BuildQuestTrackerUI();
                Debug.Log("[QuestManager] HUD nhiệm vụ đã được vẽ lại!");
            }
        }
        private void BuildQuestTrackerUI()
        {
            if (m_QuestTrackerRoot == null) return;

            m_QuestTrackerContainer = new VisualElement();
            m_QuestTrackerContainer.name = "quest-tracker";
            m_QuestTrackerContainer.style.position = Position.Absolute;
            m_QuestTrackerContainer.style.top = 20;
            m_QuestTrackerContainer.style.right = 20;
            m_QuestTrackerContainer.style.width = 300;
            m_QuestTrackerContainer.style.paddingTop = 15;
            m_QuestTrackerContainer.style.paddingBottom = 15;
            m_QuestTrackerContainer.style.paddingLeft = 18;
            m_QuestTrackerContainer.style.paddingRight = 18;
            m_QuestTrackerContainer.style.backgroundColor = new Color(0.05f, 0.05f, 0.12f, 0.8f);
            m_QuestTrackerContainer.style.borderTopLeftRadius = 10;
            m_QuestTrackerContainer.style.borderTopRightRadius = 10;
            m_QuestTrackerContainer.style.borderBottomLeftRadius = 10;
            m_QuestTrackerContainer.style.borderBottomRightRadius = 10;
            m_QuestTrackerContainer.style.borderTopWidth = 2;
            m_QuestTrackerContainer.style.borderBottomWidth = 2;
            m_QuestTrackerContainer.style.borderLeftWidth = 2;
            m_QuestTrackerContainer.style.borderRightWidth = 2;
            m_QuestTrackerContainer.style.borderTopColor = new Color(0.95f, 0.75f, 0.2f, 0.5f);
            m_QuestTrackerContainer.style.borderBottomColor = new Color(0.95f, 0.75f, 0.2f, 0.5f);
            m_QuestTrackerContainer.style.borderLeftColor = new Color(0.95f, 0.75f, 0.2f, 0.5f);
            m_QuestTrackerContainer.style.borderRightColor = new Color(0.95f, 0.75f, 0.2f, 0.5f);

            // Title
            m_QuestTitleLabel = new Label("Sứ Mệnh Thánh Gióng");
            m_QuestTitleLabel.style.fontSize = 16;
            m_QuestTitleLabel.style.color = new Color(0.95f, 0.75f, 0.2f);
            m_QuestTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            m_QuestTitleLabel.style.marginBottom = 5;
            m_QuestTrackerContainer.Add(m_QuestTitleLabel);

            // Current step instruction
            m_QuestStepLabel = new Label("");
            m_QuestStepLabel.style.fontSize = 13;
            m_QuestStepLabel.style.color = new Color(0.5f, 0.85f, 1f);
            m_QuestStepLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            m_QuestStepLabel.style.marginBottom = 8;
            m_QuestStepLabel.style.whiteSpace = WhiteSpace.Normal;
            m_QuestTrackerContainer.Add(m_QuestStepLabel);

            // Divider
            var divider = new VisualElement();
            divider.style.height = 1;
            divider.style.backgroundColor = new Color(0.95f, 0.75f, 0.2f, 0.3f);
            divider.style.marginBottom = 8;
            m_QuestTrackerContainer.Add(divider);

            // Progress
            m_QuestProgressLabel = new Label($"Tiến trình: 0/{TotalSteps}");
            m_QuestProgressLabel.style.fontSize = 12;
            m_QuestProgressLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            m_QuestProgressLabel.style.marginBottom = 8;
            m_QuestTrackerContainer.Add(m_QuestProgressLabel);

            // Step list
            m_StepListContainer = new VisualElement();

            for (int i = 0; i < m_StepDescriptions.Length; i++)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 4;

                var icon = new Label(i == 0 ? "▸" : "○");
                icon.style.fontSize = 12;
                icon.style.color = i == 0 ? new Color(0.5f, 0.85f, 1f) : new Color(0.4f, 0.4f, 0.4f);
                icon.style.marginRight = 8;
                icon.style.width = 16;

                var label = new Label(m_StepDescriptions[i]);
                label.style.fontSize = 13;
                label.style.color = i == 0 ? Color.white : new Color(0.5f, 0.5f, 0.5f);

                row.Add(icon);
                row.Add(label);
                m_StepListContainer.Add(row);

                m_StepRows.Add(row);
                m_StepIcons.Add(icon);
                m_StepLabels.Add(label);
            }

            m_QuestTrackerContainer.Add(m_StepListContainer);
            m_QuestTrackerRoot.Add(m_QuestTrackerContainer);

            // Set initial state
            UpdateQuestTrackerUI();
        }

        private void UpdateQuestTrackerUI()
        {
            if (m_QuestProgressLabel != null)
            {
                m_QuestProgressLabel.text = $"Tiến trình: {CompletedCount}/{TotalSteps}";
            }

            // Update current step label
            if (m_QuestStepLabel != null)
            {
                if (m_CurrentQuestStep < m_StepDescriptions.Length)
                {
                    m_QuestStepLabel.text = $"► {m_StepDescriptions[m_CurrentQuestStep]}";
                }
                else
                {
                    m_QuestStepLabel.text = "★ Chuẩn bị biến hình!";
                    m_QuestStepLabel.style.color = new Color(0.95f, 0.75f, 0.2f);
                }
            }

            // Update step icons and colors
            for (int i = 0; i < m_StepIcons.Count; i++)
            {
                if (i < m_CurrentQuestStep)
                {
                    // Completed
                    m_StepIcons[i].text = "✓";
                    m_StepIcons[i].style.color = new Color(0.3f, 0.85f, 0.55f);
                    m_StepLabels[i].style.color = new Color(0.3f, 0.85f, 0.55f);
                }
                else if (i == m_CurrentQuestStep)
                {
                    // Current
                    m_StepIcons[i].text = "▸";
                    m_StepIcons[i].style.color = new Color(0.5f, 0.85f, 1f);
                    m_StepLabels[i].style.color = Color.white;
                }
                else
                {
                    // Locked
                    m_StepIcons[i].text = "○";
                    m_StepIcons[i].style.color = new Color(0.4f, 0.4f, 0.4f);
                    m_StepLabels[i].style.color = new Color(0.5f, 0.5f, 0.5f);
                }
            }
        }

        private void BuildTransformationUI()
        {
            m_WinScreenRoot = winScreenUIDocument.rootVisualElement;

            var overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.right = 0;
            overlay.style.top = 0;
            overlay.style.bottom = 0;
            overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.9f);
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;

            var contentBox = new VisualElement();
            contentBox.style.alignItems = Align.Center;
            contentBox.style.paddingTop = 40;
            contentBox.style.paddingBottom = 40;
            contentBox.style.paddingLeft = 60;
            contentBox.style.paddingRight = 60;

            //// Star
            //var star = new Label("★");
            //star.style.fontSize = 60;
            //star.style.color = new Color(0.95f, 0.75f, 0.2f);
            //star.style.marginBottom = 20;
            //contentBox.Add(star);

            //// Title
            //var title = new Label("GIÓNG VƯƠN MÌNH!");
            //title.style.fontSize = 40;
            //title.style.color = new Color(0.95f, 0.75f, 0.2f);
            //title.style.unityFontStyleAndWeight = FontStyle.Bold;
            //title.style.marginBottom = 15;
            //title.style.letterSpacing = 4;
            //contentBox.Add(title);

            //// Description
            //var desc = new Label(
            //    "Nhà vua đã gửi đến ngựa sắt, roi sắt và áo giáp sắt.\n" +
            //    "Gióng ăn hết thóc gạo của dân làng,\n" + 
            //    "vươn mình trở thành tráng sĩ oai phong!\n\n" +
            //    "Giờ là lúc ra trận đánh giặc Ân!");
            //desc.style.fontSize = 18;
            //desc.style.color = new Color(0.9f, 0.9f, 0.9f);
            //desc.style.unityTextAlign = TextAnchor.MiddleCenter;
            //desc.style.marginBottom = 25;
            //desc.style.whiteSpace = WhiteSpace.Normal;
            //contentBox.Add(desc);

            //// Loading text
            //var loading = new Label("Đang chuẩn bị chiến trường...");
            //loading.style.fontSize = 14;
            //loading.style.color = new Color(0.5f, 0.85f, 1f);
            //loading.style.unityFontStyleAndWeight = FontStyle.Italic;
            //contentBox.Add(loading);

            overlay.Add(contentBox);
            m_WinScreenRoot.Add(overlay);
        }

        #endregion
    }
}
