using System;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Manages the dialogue UI using Unity's UI Toolkit.
    /// Displays chat box with speaker name, dialogue text, character portrait area, 
    /// and "press to continue" indicator.
    /// Attach to a GameObject with a UIDocument component.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class DialogueUI : MonoBehaviour
    {
        #region Fields

        [Header("UI Settings")]
        [Tooltip("Color for NPC name text.")]
        [SerializeField] private Color npcNameColor = new Color(0.95f, 0.75f, 0.2f); // Gold

        [Tooltip("Color for player name text.")]
        [SerializeField] private Color playerNameColor = new Color(0.3f, 0.85f, 0.55f); // Green

        [Tooltip("Color for dialogue text.")]
        [SerializeField] private Color dialogueTextColor = Color.white;

        [Tooltip("Background opacity for the dialogue box.")]
        [Range(0f, 1f)]
        [SerializeField] private float backgroundOpacity = 0.85f;

        [Header("Avatar Portraits (kéo ảnh vào đây nếu muốn thay thế)")]
        [Tooltip("Portrait Thánh Gióng. Để trống = tự load từ Resources/Portraits/Thanhgiong.")]
        [SerializeField] private Texture2D playerPortraitOverride;

        [Tooltip("Danh sách portrait NPC. Để trống = tự load từ Resources/Portraits/.")]
        [SerializeField] private List<AvatarEntry> npcPortraitOverrides = new List<AvatarEntry>();

        private UIDocument m_UIDocument;
        private VisualElement m_Root;
        private VisualElement m_DialogueContainer;
        private VisualElement m_DialogueBox;
        private VisualElement m_PortraitContainer;
        private VisualElement m_SpeakerIcon;
        private Label m_SpeakerNameLabel;
        private Label m_DialogueTextLabel;
        private Label m_ContinuePromptLabel;
        private VisualElement m_QuestNotification;
        private Label m_QuestNotificationLabel;
        private VisualElement m_InteractPrompt;
        private Label m_InteractPromptLabel;
        private Coroutine m_ContinueBlinkCoroutine;
        private Coroutine m_QuestNotificationCoroutine;
        private Coroutine m_InteractPromptBlinkCoroutine;

        // Avatar system - được kéo trực tiếp từ Inspector
        private Texture2D m_PlayerPortrait;
        private Dictionary<string, Texture2D> m_AvatarLookup = new Dictionary<string, Texture2D>();

        /// <summary>
        /// Ánh xạ tên speaker sang portrait texture. Dùng cho Inspector override.
        /// </summary>
        [Serializable]
        public class AvatarEntry
        {
            [Tooltip("Tên speaker chính xác như trong DialogueData (VD: 'Mẹ', 'Sứ Giả', 'Già Làng').")]
            public string speakerName;
            [Tooltip("Ảnh portrait.")]
            public Texture2D portrait;
        }

        #endregion

        #region Unity Lifecycle

#if UNITY_EDITOR
        /// <summary>
        /// Hàm này tự động chạy trong Editor khi cậu click vào GameObject hoặc Script được compile lại.
        /// Nó sẽ tự động "gán" các ảnh từ thư mục Art cho cậu.
        /// </summary>
        private void OnValidate()
        {
            string basePath = "Assets/Core/Art/Image/";
            
            // Tự gán ảnh cho Gióng
            if (playerPortraitOverride == null)
            {
                playerPortraitOverride = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "Thanhgiong.png");
            }

            // Tự gán ảnh cho các NPC nếu danh sách đang trống
            if (npcPortraitOverrides == null || npcPortraitOverrides.Count == 0)
            {
                npcPortraitOverrides = new List<AvatarEntry>();
                
                // Danh sách tên và file tương ứng
                var mapping = new Dictionary<string, string>
                {
                    { "Mẹ", "me.png" },
                    { "Bà Lão", "me.png" },
                    { "Sứ Giả", "Sugia.png" },
                    { "Sứ giả", "Sugia.png" },
                    { "Già Làng", "gialang.png" },
                    { "Già làng", "gialang.png" },
                    { "Thanh Niên", "Thanhnien.png" },
                    { "Bé Gái", "begai.png" },
                    { "Bé Trai", "betrai.png" }
                };

                foreach (var pair in mapping)
                {
                    var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + pair.Value);
                    if (tex != null)
                    {
                        npcPortraitOverrides.Add(new AvatarEntry { speakerName = pair.Key, portrait = tex });
                    }
                }
            }
        }
#endif

        private void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
            BuildAvatarLookup();
        }

        /// <summary>
        /// Xây dựng từ điển tra cứu từ danh sách cậu đã kéo trong Inspector.
        /// </summary>
        private void BuildAvatarLookup()
        {
            m_AvatarLookup.Clear();
            m_PlayerPortrait = playerPortraitOverride;

            if (npcPortraitOverrides != null)
            {
                foreach (var entry in npcPortraitOverrides)
                {
                    if (!string.IsNullOrEmpty(entry.speakerName) && entry.portrait != null)
                    {
                        m_AvatarLookup[entry.speakerName] = entry.portrait;
                    }
                }
            }
        }

        private void OnEnable()
        {
            StartCoroutine(InitializeUI());
        }

        private void OnDisable()
        {
            UnsubscribeEvents();

            if (m_ContinueBlinkCoroutine != null)
            {
                StopCoroutine(m_ContinueBlinkCoroutine);
                m_ContinueBlinkCoroutine = null;
            }

            if (m_QuestNotificationCoroutine != null)
            {
                StopCoroutine(m_QuestNotificationCoroutine);
                m_QuestNotificationCoroutine = null;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Waits one frame for UIDocument to initialize, then builds the UI.
        /// </summary>
        private IEnumerator InitializeUI()
        {
            yield return null;

            m_Root = m_UIDocument.rootVisualElement;
            BuildDialogueUI();
            HideDialogue();
            SubscribeEvents();
        }

        /// <summary>
        /// Builds the entire dialogue UI programmatically using UI Toolkit.
        /// </summary>
        private void BuildDialogueUI()
        {
            // --- Main Container ---
            m_DialogueContainer = new VisualElement();
            // ... (giữ nguyên phần style của m_DialogueContainer) ...
            m_DialogueContainer.style.position = Position.Absolute;
            m_DialogueContainer.style.left = 0;
            m_DialogueContainer.style.right = 0;
            m_DialogueContainer.style.top = 0;
            m_DialogueContainer.style.bottom = 0;
            m_DialogueContainer.style.justifyContent = Justify.FlexEnd;
            m_DialogueContainer.style.alignItems = Align.Center;
            m_DialogueContainer.pickingMode = PickingMode.Ignore;

            // --- Dialogue Box (Thu nhỏ lại) ---
            m_DialogueBox = new VisualElement();
            m_DialogueBox.name = "dialogue-box";
            m_DialogueBox.style.width = Length.Percent(60); // Giảm chiều rộng từ 75% xuống 60%
            m_DialogueBox.style.minHeight = 80;            // Giảm chiều cao tối thiểu từ 160 xuống 80
            m_DialogueBox.style.maxWidth = 700;            // Giới hạn chiều rộng tối đa nhỏ lại
            m_DialogueBox.style.marginBottom = 30;         // Đẩy sát xuống đáy hơn một chút
            m_DialogueBox.style.paddingTop = 10;           // Giảm padding để hộp ôm sát chữ
            m_DialogueBox.style.paddingBottom = 10;
            m_DialogueBox.style.paddingLeft = 20;
            m_DialogueBox.style.paddingRight = 20;

            // Màu nền tối hơn để chữ trắng cỡ 12 dễ đọc
            m_DialogueBox.style.backgroundColor = new Color(0f, 0f, 0f, 0.75f);
            m_DialogueBox.style.borderTopLeftRadius = 10;
            m_DialogueBox.style.borderTopRightRadius = 10;
            m_DialogueBox.style.borderBottomLeftRadius = 10;
            m_DialogueBox.style.borderBottomRightRadius = 10;
            m_DialogueBox.style.flexDirection = FlexDirection.Row;
            m_DialogueBox.style.alignItems = Align.Center; // Căn giữa avatar và text theo chiều dọc

            // --- Portrait Section (Thu nhỏ cho cân đối với hộp mới) ---
            m_PortraitContainer = new VisualElement();
            m_PortraitContainer.style.width = 50;  // Giảm từ 80 xuống 50
            m_PortraitContainer.style.height = 50;
            m_PortraitContainer.style.minWidth = 50;
            m_PortraitContainer.style.marginRight = 15;
            m_PortraitContainer.style.borderTopLeftRadius = 25;
            m_PortraitContainer.style.borderTopRightRadius = 25;
            m_PortraitContainer.style.borderBottomLeftRadius = 25;
            m_PortraitContainer.style.borderBottomRightRadius = 25;
            m_PortraitContainer.style.overflow = Overflow.Hidden;

            m_SpeakerIcon = new VisualElement();
            m_SpeakerIcon.style.width = Length.Percent(100);
            m_SpeakerIcon.style.height = Length.Percent(100);
            m_PortraitContainer.Add(m_SpeakerIcon);
            m_DialogueBox.Add(m_PortraitContainer);

            // --- Text Content Section ---
            var textContainer = new VisualElement();
            textContainer.style.flexGrow = 1;

            // Speaker Name
            m_SpeakerNameLabel = new Label("Speaker");
            m_SpeakerNameLabel.style.fontSize = 13; // Tên nhân vật lớn hơn text một chút
            m_SpeakerNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            m_SpeakerNameLabel.style.color = npcNameColor;
            m_SpeakerNameLabel.style.marginBottom = 2;
            textContainer.Add(m_SpeakerNameLabel);

            // Dialogue Text (Cỡ chữ 12 theo ý bạn)
            m_DialogueTextLabel = new Label("...");
            m_DialogueTextLabel.style.fontSize = 12; // Cỡ chữ 12
            m_DialogueTextLabel.style.color = Color.white;
            m_DialogueTextLabel.style.whiteSpace = WhiteSpace.Normal;
            m_DialogueTextLabel.style.flexWrap = Wrap.Wrap;
            textContainer.Add(m_DialogueTextLabel);

            // Continue Prompt
            m_ContinuePromptLabel = new Label("▼ E");
            m_ContinuePromptLabel.style.fontSize = 10;
            m_ContinuePromptLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            m_ContinuePromptLabel.style.marginTop = 5;
            m_ContinuePromptLabel.style.display = DisplayStyle.None;
            textContainer.Add(m_ContinuePromptLabel);

            m_DialogueBox.Add(textContainer);
            m_DialogueContainer.Add(m_DialogueBox);

            // ... (Giữ nguyên phần Quest Notification và Interact Prompt bên dưới) ...

            m_Root.Add(m_DialogueContainer);
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeEvents()
        {
            if (DialogueSystem.Instance == null)
            {
                Debug.LogWarning("[DialogueUI] DialogueSystem.Instance is null. Will retry later.");
                StartCoroutine(RetrySubscription());
                return;
            }

            DialogueSystem.Instance.OnDialogueStarted += HandleDialogueStarted;
            DialogueSystem.Instance.OnDialogueLineShown += HandleDialogueLineShown;
            DialogueSystem.Instance.OnDialogueEnded += HandleDialogueEnded;
            DialogueSystem.Instance.OnWaitingForInput += HandleWaitingForInput;
        }

        private IEnumerator RetrySubscription()
        {
            while (DialogueSystem.Instance == null)
            {
                yield return new WaitForSeconds(0.5f);
            }
            SubscribeEvents();
        }

        private void UnsubscribeEvents()
        {
            if (DialogueSystem.Instance == null) return;

            DialogueSystem.Instance.OnDialogueStarted -= HandleDialogueStarted;
            DialogueSystem.Instance.OnDialogueLineShown -= HandleDialogueLineShown;
            DialogueSystem.Instance.OnDialogueEnded -= HandleDialogueEnded;
            DialogueSystem.Instance.OnWaitingForInput -= HandleWaitingForInput;
        }

        #endregion

        #region Event Handlers

        private void HandleDialogueStarted(string npcName)
        {
            HideInteractPrompt(); // Hide E prompt when dialogue starts
            ShowDialogue();
            m_SpeakerNameLabel.text = npcName;
            m_SpeakerNameLabel.style.color = npcNameColor;
            m_DialogueTextLabel.text = "...";
            HideContinuePrompt();

            // Set initial NPC portrait
            UpdatePortrait(npcName, false);
        }

        private void HandleDialogueLineShown(string speakerName, string text, bool isPlayerLine)
        {
            m_SpeakerNameLabel.text = speakerName;
            m_SpeakerNameLabel.style.color = isPlayerLine ? playerNameColor : npcNameColor;
            m_DialogueTextLabel.text = text;

            // Update portrait avatar based on current speaker
            UpdatePortrait(speakerName, isPlayerLine);

            // Change portrait border color based on speaker
            if (isPlayerLine)
            {
                m_PortraitContainer.style.borderTopColor = new Color(0.3f, 0.85f, 0.55f, 0.5f);
                m_PortraitContainer.style.borderBottomColor = new Color(0.3f, 0.85f, 0.55f, 0.5f);
                m_PortraitContainer.style.borderLeftColor = new Color(0.3f, 0.85f, 0.55f, 0.5f);
                m_PortraitContainer.style.borderRightColor = new Color(0.3f, 0.85f, 0.55f, 0.5f);
            }
            else
            {
                m_PortraitContainer.style.borderTopColor = new Color(0.85f, 0.65f, 0.2f, 0.5f);
                m_PortraitContainer.style.borderBottomColor = new Color(0.85f, 0.65f, 0.2f, 0.5f);
                m_PortraitContainer.style.borderLeftColor = new Color(0.85f, 0.65f, 0.2f, 0.5f);
                m_PortraitContainer.style.borderRightColor = new Color(0.85f, 0.65f, 0.2f, 0.5f);
            }
        }

        /// <summary>
        /// Updates the portrait circle with the appropriate avatar texture.
        /// </summary>
        private void UpdatePortrait(string speakerName, bool isPlayerLine)
        {
            if (m_SpeakerIcon == null) return;

            Texture2D portrait = null;

            if (isPlayerLine)
            {
                portrait = m_PlayerPortrait;
            }
            else if (!string.IsNullOrEmpty(speakerName))
            {
                m_AvatarLookup.TryGetValue(speakerName, out portrait);
            }

            if (portrait != null)
            {
                m_SpeakerIcon.style.backgroundImage = new StyleBackground(portrait);
                m_SpeakerIcon.style.unityBackgroundScaleMode = ScaleMode.ScaleAndCrop;
            }
            else
            {
                m_SpeakerIcon.style.backgroundImage = StyleKeyword.None;
            }
        }

        private void HandleDialogueEnded(string npcName)
        {
            HideDialogue();
        }

        private void HandleWaitingForInput()
        {
            ShowContinuePrompt();
        }

        #endregion

        #region UI Control

        private void ShowDialogue()
        {
            if (m_DialogueContainer != null)
            {
                m_DialogueContainer.style.display = DisplayStyle.Flex;
                // Simple show - set full opacity
                m_DialogueBox.style.opacity = 1;
            }
        }

        private void HideDialogue()
        {
            if (m_DialogueContainer != null)
            {
                m_DialogueContainer.style.display = DisplayStyle.None;
            }
            HideContinuePrompt();
        }

        private void ShowContinuePrompt()
        {
            if (m_ContinuePromptLabel != null)
            {
                m_ContinuePromptLabel.style.display = DisplayStyle.Flex;

                // Start blink animation
                if (m_ContinueBlinkCoroutine != null)
                {
                    StopCoroutine(m_ContinueBlinkCoroutine);
                }
                m_ContinueBlinkCoroutine = StartCoroutine(BlinkContinuePrompt());
            }
        }

        private void HideContinuePrompt()
        {
            if (m_ContinuePromptLabel != null)
            {
                m_ContinuePromptLabel.style.display = DisplayStyle.None;
            }

            if (m_ContinueBlinkCoroutine != null)
            {
                StopCoroutine(m_ContinueBlinkCoroutine);
                m_ContinueBlinkCoroutine = null;
            }
        }

        /// <summary>
        /// Shows a quest notification at the top of the screen.
        /// </summary>
        /// <param name="message">The notification message.</param>
        /// <param name="duration">How long to show it.</param>
        public void ShowQuestNotification(string message, float duration = 3f)
        {
            if (m_QuestNotification == null || m_QuestNotificationLabel == null) return;

            m_QuestNotificationLabel.text = message;
            m_QuestNotification.style.display = DisplayStyle.Flex;

            if (m_QuestNotificationCoroutine != null)
            {
                StopCoroutine(m_QuestNotificationCoroutine);
            }
            m_QuestNotificationCoroutine = StartCoroutine(HideQuestNotificationAfterDelay(duration));
        }

        private IEnumerator HideQuestNotificationAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (m_QuestNotification != null)
            {
                m_QuestNotification.style.display = DisplayStyle.None;
            }
            m_QuestNotificationCoroutine = null;
        }

        private IEnumerator BlinkContinuePrompt()
        {
            while (true)
            {
                if (m_ContinuePromptLabel != null)
                {
                    m_ContinuePromptLabel.style.opacity = 1f;
                }
                yield return new WaitForSeconds(0.6f);
                if (m_ContinuePromptLabel != null)
                {
                    m_ContinuePromptLabel.style.opacity = 0.3f;
                }
                yield return new WaitForSeconds(0.4f);
            }
        }

        /// <summary>
        /// Shows the "Press E" interact prompt with NPC name.
        /// </summary>
        public void ShowInteractPrompt(string message)
        {
            if (m_InteractPrompt == null || m_InteractPromptLabel == null) return;

            m_InteractPromptLabel.text = message;
            m_InteractPrompt.style.display = DisplayStyle.Flex;

            // Start blink
            if (m_InteractPromptBlinkCoroutine != null)
            {
                StopCoroutine(m_InteractPromptBlinkCoroutine);
            }
            m_InteractPromptBlinkCoroutine = StartCoroutine(BlinkInteractPrompt());
        }

        /// <summary>
        /// Hides the "Press E" interact prompt.
        /// </summary>
        public void HideInteractPrompt()
        {
            if (m_InteractPrompt != null)
            {
                m_InteractPrompt.style.display = DisplayStyle.None;
            }

            if (m_InteractPromptBlinkCoroutine != null)
            {
                StopCoroutine(m_InteractPromptBlinkCoroutine);
                m_InteractPromptBlinkCoroutine = null;
            }
        }

        private IEnumerator BlinkInteractPrompt()
        {
            while (true)
            {
                if (m_InteractPrompt != null)
                {
                    m_InteractPrompt.style.opacity = 1f;
                }
                yield return new WaitForSeconds(0.8f);
                if (m_InteractPrompt != null)
                {
                    m_InteractPrompt.style.opacity = 0.5f;
                }
                yield return new WaitForSeconds(0.5f);
            }
        }

        #endregion
    }
}
