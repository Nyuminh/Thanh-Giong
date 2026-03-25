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

        [Header("Quest Hint Settings")]
        [Tooltip("Only show action key hint when player is this close to current target.")]
        [SerializeField] private float interactionHintDistance = 8f;

        // Internal tracking
        private bool m_IsTrackerVisible = false;
        private int m_CurrentQuestStep = 0;
        private HashSet<string> m_CompletedVillagers = new HashSet<string>();
        private bool m_QuestCompleted;

        // Đăng ký các Item hoặc Target động
        private Dictionary<int, Transform> m_RegisteredTargets = new Dictionary<int, Transform>();

        // Quest step descriptions chuyển sang List để dễ bề gắn thêm Nhiệm vụ phụ (như lấy Item)
        private List<string> m_StepDescriptions = new List<string>()
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
        private VisualElement m_FullDetailsContainer; // Thêm container chi tiết
        private Label m_QuestTitleLabel;
        private Label m_QuestStepLabel;
        private Label m_QuestProgressLabel;
        private Label m_DistanceLabel; // Hiển thị khoảng cách
        private Label m_DirectionArrow; // Mũi tên chỉ hướng
        private ScrollView m_StepListContainer;
        private List<VisualElement> m_StepRows = new List<VisualElement>();
        private List<Label> m_StepIcons = new List<Label>();
        private List<Label> m_StepLabels = new List<Label>();

        // Danh sách nội dung đã bị gộp (dùng cho UI hiện lên)
        private List<string> m_UIDisplayNames = new List<string>();
        // Ánh xạ từ dòng UI (chỉ số m_UIDisplayNames) -> Danh sách các Steps bị gộp
        private List<List<int>> m_UIRowToStepsMap = new List<List<int>>();

        private VisualElement m_WinScreenRoot;

        #endregion

        #region Properties

        public int TotalSteps => Mathf.Max(allVillagers.Count, m_StepDescriptions.Count);
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
                // Sort by the first questStep
                allVillagers.Sort((a, b) => 
                {
                    int stepA = (a.QuestSteps != null && a.QuestSteps.Length > 0) ? a.QuestSteps[0] : 0;
                    int stepB = (b.QuestSteps != null && b.QuestSteps.Length > 0) ? b.QuestSteps[0] : 0;
                    return stepA.CompareTo(stepB);
                });
                Debug.Log($"[QuestManager] Auto-found {allVillagers.Count} villagers.");
            }

            // Update step descriptions from actual villager names
            if (allVillagers.Count > 0)
            {
                // Mở rộng danh sách nếu cần thiết
                while (m_StepDescriptions.Count < allVillagers.Count) m_StepDescriptions.Add("");

                for (int i = 0; i < allVillagers.Count; i++)
                {
                    if (allVillagers[i] != null)
                        m_StepDescriptions[i] = $"Nói chuyện với {allVillagers[i].VillagerName}";
                }
            }

            RebuildUIDisplayList();
            StartCoroutine(InitializeUI());
        }

        private void RebuildUIDisplayList()
        {
            m_UIDisplayNames.Clear();
            m_UIRowToStepsMap.Clear();

            if (m_StepDescriptions.Count == 0) return;

            string currentName = m_StepDescriptions[0];
            m_UIDisplayNames.Add(currentName);
            m_UIRowToStepsMap.Add(new List<int> { 0 });

            for (int i = 1; i < m_StepDescriptions.Count; i++)
            {
                if (m_StepDescriptions[i] == currentName && !string.IsNullOrEmpty(currentName))
                {
                    // Trùng tên với nhiệm vụ ngay trước đó -> Gộp vào 1 Group UI
                    m_UIRowToStepsMap[m_UIRowToStepsMap.Count - 1].Add(i);
                }
                else
                {
                    // Nhiệm vụ mới
                    currentName = m_StepDescriptions[i];
                    m_UIDisplayNames.Add(currentName);
                    m_UIRowToStepsMap.Add(new List<int> { i });
                }
            }
        }
      

    private void Update()
    {
        // Cập nhật la bàn / chỉ đường
        UpdateNavigationArrow();
        UpdateCurrentStepLabel();

        // Kiểm tra nếu phím Tab đang được GIỮ (Hold)
        if (Keyboard.current != null && Keyboard.current.tabKey.isPressed)
        {
            if (m_FullDetailsContainer != null && m_FullDetailsContainer.style.display == DisplayStyle.None)
            {
                m_FullDetailsContainer.style.display = DisplayStyle.Flex;
                UpdateQuestTrackerUI();

                // Bật tự do chuột để cuộn ScrollView
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        else // Nếu KHÔNG nhấn hoặc THẢ ra
        {
            if (m_FullDetailsContainer != null && m_FullDetailsContainer.style.display == DisplayStyle.Flex)
            {
                m_FullDetailsContainer.style.display = DisplayStyle.None;

                // Ẩn chuột và khóa nó vào giữa màn hình khi thả phím
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private void UpdateNavigationArrow()
    {
        if (m_DirectionArrow == null || m_DistanceLabel == null || m_QuestCompleted) return;

        if (m_CurrentQuestStep < TotalSteps)
        {
            Transform targetTransform = GetCurrentTargetTransform();

            if (targetTransform == null) return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            Vector3 targetPos = targetTransform.position;
            Vector3 playerPos = player.transform.position;
            Vector3 dirToTarget = targetPos - playerPos;
            dirToTarget.y = 0; // 2D flat for accurate compass

            float dist = dirToTarget.magnitude;
            m_DistanceLabel.text = $"{Mathf.RoundToInt(dist)}m";

            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 camForward = cam.transform.forward;
                camForward.y = 0;
                if (camForward.sqrMagnitude > 0.001f && dirToTarget.sqrMagnitude > 0.001f)
                {
                    float angle = Vector3.SignedAngle(camForward, dirToTarget, Vector3.up);
                    m_DirectionArrow.transform.rotation = Quaternion.Euler(0, 0, angle);
                }
            }
        }
        else
        {
            m_DistanceLabel.text = "";
            m_DirectionArrow.text = "★";
            m_DirectionArrow.transform.rotation = Quaternion.identity;
        }
    }
    
        private Transform GetCurrentTargetTransform()
        {
            if (m_RegisteredTargets.ContainsKey(m_CurrentQuestStep))
            {
                return m_RegisteredTargets[m_CurrentQuestStep];
            }

            if (m_CurrentQuestStep < allVillagers.Count && allVillagers[m_CurrentQuestStep] != null)
            {
                return allVillagers[m_CurrentQuestStep].transform;
            }

            return null;
        }

        private bool IsDialogueStep(int step)
        {
            foreach (var villager in allVillagers)
            {
                if (villager == null || villager.QuestSteps == null) continue;
                foreach (int villagerStep in villager.QuestSteps)
                {
                    if (villagerStep == step) return true;
                }
            }
            return false;
        }

        private void UpdateCurrentStepLabel()
        {
            if (m_QuestStepLabel == null) return;

            if (m_CurrentQuestStep < m_StepDescriptions.Count)
            {
                string baseText = $"➜ {m_StepDescriptions[m_CurrentQuestStep]}";
                string hintText = string.Empty;

                var player = GameObject.FindGameObjectWithTag("Player");
                var targetTransform = GetCurrentTargetTransform();
                if (player != null && targetTransform != null)
                {
                    float distance = Vector3.Distance(player.transform.position, targetTransform.position);
                    if (distance <= interactionHintDistance)
                    {
                        hintText = IsDialogueStep(m_CurrentQuestStep)
                            ? "\nNhấn E để nói chuyện"
                            : "\nNhấn F để làm nhiệm vụ";
                    }
                }

                m_QuestStepLabel.text = baseText + hintText;
                m_QuestStepLabel.style.color = new Color(0.5f, 0.85f, 1f);
            }
            else
            {
                m_QuestStepLabel.text = "★ Chuẩn bị biến hình!";
                m_QuestStepLabel.style.color = new Color(0.95f, 0.75f, 0.2f);
            }
        }
    
        #endregion

        #region Public Methods

        public void RegisterQuestTarget(int step, Transform targetTransform, string targetName)
        {
            if (!m_RegisteredTargets.ContainsKey(step)) m_RegisteredTargets.Add(step, targetTransform);
            else m_RegisteredTargets[step] = targetTransform;

            // Đảm bảo list description đủ lớn
            while(m_StepDescriptions.Count <= step) m_StepDescriptions.Add("");
            
            // Ghi đè label hành động cho phù hợp
            m_StepDescriptions[step] = $"{targetName}";
            
            // Rebuild lại danh sách rút gọn và vẽ lại UI
            RefreshQuestUI();
        }

        public void UpdateTotalSteps(int requiredTotalSteps)
        {
            while (m_StepDescriptions.Count < requiredTotalSteps) m_StepDescriptions.Add("Nhiệm vụ mới...");
        }

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
        public void OnVillagerDialogueCompleted(string villagerName, int completedStep)
        {
            if (m_QuestCompleted) return;

            // Cho phép NPC hoàn thành nhiều bước mà không bị chặn lại bởi tên
            string stepKey = $"{villagerName}_{completedStep}";

            if (m_CompletedVillagers.Add(stepKey))
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
                        string nextStep = m_CurrentQuestStep < m_StepDescriptions.Count
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

        #region Save / Load

        public void SaveQuestProgress()
        {
            PlayerPrefs.SetInt("SavedQuestStep", m_CurrentQuestStep);
            PlayerPrefs.SetString("SavedCompletedVillagers", string.Join(",", m_CompletedVillagers));
            PlayerPrefs.SetInt("SavedQuestCompleted", m_QuestCompleted ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[QuestManager] Saved quest progress. Step: {m_CurrentQuestStep}");
        }

        public void LoadQuestProgress()
        {
            if (!PlayerPrefs.HasKey("SavedQuestStep")) return;

            m_CurrentQuestStep = PlayerPrefs.GetInt("SavedQuestStep", 0);
            
            m_CompletedVillagers.Clear();
            string completedStr = PlayerPrefs.GetString("SavedCompletedVillagers", "");
            if (!string.IsNullOrEmpty(completedStr))
            {
                var arr = completedStr.Split(',');
                foreach(var s in arr)
                {
                    m_CompletedVillagers.Add(s);
                }
            }
            
            m_QuestCompleted = PlayerPrefs.GetInt("SavedQuestCompleted", 0) == 1;

            if (m_QuestTrackerRoot != null)
            {
                UpdateQuestTrackerUI();
            }

            Debug.Log($"[QuestManager] Loaded quest progress. Step: {m_CurrentQuestStep}");
        }

        public void FastForwardQuestsToCurrentStep(GameObject player)
        {
            var villagers = FindObjectsOfType<VillagerNPC>(true);
            var questItems = FindObjectsOfType<QuestItemInteract>(true);

            for (int s = 0; s < m_CurrentQuestStep; s++)
            {
                // Gọi Event của QuestItemInteract ở bước 's'
                foreach (var item in questItems)
                {
                    if (item != null && item.QuestStep == s)
                    {
                        item.FastForward(player);
                    }
                }

                // Gọi Event của VillagerNPC ở bước 's'
                foreach (var v in villagers)
                {
                    if (v != null)
                    {
                        v.FastForward(s, player);
                    }
                }
            }
            Debug.Log($"[QuestManager] Fast-forwarded all quest events up to step {m_CurrentQuestStep}");
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
                RefreshQuestUI();
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

                RebuildUIDisplayList();
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

            // Title row to hold both Title and Pointer
            var titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.alignItems = Align.Center;
            titleRow.style.justifyContent = Justify.SpaceBetween;

            // Title
            m_QuestTitleLabel = new Label("Sứ Mệnh Thánh Gióng");
            m_QuestTitleLabel.style.fontSize = 16;
            m_QuestTitleLabel.style.color = new Color(0.95f, 0.75f, 0.2f);
            m_QuestTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            m_QuestTitleLabel.style.marginBottom = 5;

            // Pointer container (la bàn chỉ hướng)
            var pointerContainer = new VisualElement();
            pointerContainer.style.flexDirection = FlexDirection.Row;
            pointerContainer.style.alignItems = Align.Center;

            m_DistanceLabel = new Label("");
            m_DistanceLabel.style.fontSize = 14;
            m_DistanceLabel.style.color = new Color(0.6f, 1f, 0.6f); // Xanh nhạt
            m_DistanceLabel.style.marginRight = 8;
            m_DistanceLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            m_DirectionArrow = new Label("⬆");
            m_DirectionArrow.style.fontSize = 24;
            m_DirectionArrow.style.color = new Color(0.95f, 0.75f, 0.2f); // Vàng
            m_DirectionArrow.style.unityFontStyleAndWeight = FontStyle.Bold;
            // Thêm bóng cho rõ nét
            m_DirectionArrow.style.textShadow = new StyleTextShadow(new TextShadow { 
                offset = new Vector2(1, 1), 
                color = Color.black, 
                blurRadius = 1 
            });
            // Xoay quanh tâm
            m_DirectionArrow.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(50));

            pointerContainer.Add(m_DistanceLabel);
            pointerContainer.Add(m_DirectionArrow);

            titleRow.Add(m_QuestTitleLabel);
            titleRow.Add(pointerContainer);

            m_QuestTrackerContainer.Add(titleRow);

            // Current step instruction (luôn hiện)
            m_QuestStepLabel = new Label("");
            m_QuestStepLabel.style.fontSize = 13;
            m_QuestStepLabel.style.color = new Color(0.5f, 0.85f, 1f);
            m_QuestStepLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            m_QuestStepLabel.style.marginBottom = 8;
            m_QuestStepLabel.style.whiteSpace = WhiteSpace.Normal;
            m_QuestTrackerContainer.Add(m_QuestStepLabel);

            // TẠO CONTAINER CHO LIST NHIỆM VỤ (CHỈ HIỆN KHI BẤM TAB)
            m_FullDetailsContainer = new VisualElement();
            m_FullDetailsContainer.style.display = DisplayStyle.None; // Ẩn lúc đầu

            // Divider
            var divider = new VisualElement();
            divider.style.height = 1;
            divider.style.backgroundColor = new Color(0.95f, 0.75f, 0.2f, 0.3f);
            divider.style.marginBottom = 8;
            m_FullDetailsContainer.Add(divider);

            // Progress
            m_QuestProgressLabel = new Label($"Tiến trình: 0/{TotalSteps}");
            m_QuestProgressLabel.style.fontSize = 12;
            m_QuestProgressLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            m_QuestProgressLabel.style.marginBottom = 8;
            m_FullDetailsContainer.Add(m_QuestProgressLabel);

            // Rebuild map groups
            RebuildUIDisplayList();

            // Step list
            m_StepListContainer = new ScrollView();
            m_StepListContainer.style.maxHeight = 350; // Cho phép cuộn nếu quá nhiều nhiệm vụ
            // Cắt nội dung tràn ra ngoài
            m_StepListContainer.style.overflow = Overflow.Hidden;

            for (int i = 0; i < m_UIDisplayNames.Count; i++)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 4;

                var icon = new Label(i == 0 ? "➜" : "○");
                icon.style.fontSize = 12;
                icon.style.color = i == 0 ? new Color(0.5f, 0.85f, 1f) : new Color(0.4f, 0.4f, 0.4f);
                icon.style.marginRight = 8;
                icon.style.width = 16;

                var label = new Label(m_UIDisplayNames[i]);
                label.style.fontSize = 13;
                label.style.color = i == 0 ? Color.white : new Color(0.5f, 0.5f, 0.5f);

                row.Add(icon);
                row.Add(label);
                m_StepListContainer.Add(row);

                m_StepRows.Add(row);
                m_StepIcons.Add(icon);
                m_StepLabels.Add(label);
            }

            m_FullDetailsContainer.Add(m_StepListContainer);
            m_QuestTrackerContainer.Add(m_FullDetailsContainer);
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

            // Update current step label (with contextual E/F hint)
            UpdateCurrentStepLabel();

            // Update step icons and colors (m_StepIcons size == m_UIDisplayNames.Count)
            for (int i = 0; i < m_StepIcons.Count; i++)
            {
                if (i >= m_UIRowToStepsMap.Count) continue;
                
                var steps = m_UIRowToStepsMap[i];
                int firstStep = steps[0];
                int lastStep = steps[steps.Count - 1];
                int totalInGroup = steps.Count;

                if (m_CurrentQuestStep > lastStep)
                {
                    // Completed
                    m_StepIcons[i].text = "✓";
                    m_StepIcons[i].style.color = new Color(0.3f, 0.85f, 0.55f);
                    m_StepLabels[i].style.color = new Color(0.3f, 0.85f, 0.55f);
                    
                    if (totalInGroup > 1) m_StepLabels[i].text = $"{m_UIDisplayNames[i]} ({totalInGroup}/{totalInGroup})";
                    else m_StepLabels[i].text = m_UIDisplayNames[i];
                }
                else if (m_CurrentQuestStep >= firstStep && m_CurrentQuestStep <= lastStep)
                {
                    // Current active group
                    m_StepIcons[i].text = "➜";
                    m_StepIcons[i].style.color = new Color(0.5f, 0.85f, 1f);
                    m_StepLabels[i].style.color = Color.white;
                    
                    if (totalInGroup > 1)
                    {
                        int completedInGroup = m_CurrentQuestStep - firstStep;
                        m_StepLabels[i].text = $"{m_UIDisplayNames[i]} ({completedInGroup}/{totalInGroup})";
                    }
                    else m_StepLabels[i].text = m_UIDisplayNames[i];
                }
                else
                {
                    // Locked
                    m_StepIcons[i].text = "○";
                    m_StepIcons[i].style.color = new Color(0.4f, 0.4f, 0.4f);
                    m_StepLabels[i].style.color = new Color(0.5f, 0.5f, 0.5f);
                    
                    if (totalInGroup > 1) m_StepLabels[i].text = $"{m_UIDisplayNames[i]} (0/{totalInGroup})";
                    else m_StepLabels[i].text = m_UIDisplayNames[i];
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
