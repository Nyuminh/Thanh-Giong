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

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
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
            // --- Main Container (full screen overlay) ---
            m_DialogueContainer = new VisualElement();
            m_DialogueContainer.name = "dialogue-container";
            m_DialogueContainer.style.position = Position.Absolute;
            m_DialogueContainer.style.left = 0;
            m_DialogueContainer.style.right = 0;
            m_DialogueContainer.style.top = 0;
            m_DialogueContainer.style.bottom = 0;
            m_DialogueContainer.style.justifyContent = Justify.FlexEnd;
            m_DialogueContainer.style.alignItems = Align.Center;
            m_DialogueContainer.pickingMode = PickingMode.Ignore;

            // --- Dialogue Box ---
            m_DialogueBox = new VisualElement();
            m_DialogueBox.name = "dialogue-box";
            m_DialogueBox.style.width = Length.Percent(75);
            m_DialogueBox.style.minHeight = 160;
            m_DialogueBox.style.maxWidth = 900;
            m_DialogueBox.style.marginBottom = 40;
            m_DialogueBox.style.paddingTop = 20;
            m_DialogueBox.style.paddingBottom = 20;
            m_DialogueBox.style.paddingLeft = 25;
            m_DialogueBox.style.paddingRight = 25;
            m_DialogueBox.style.backgroundColor = new Color(0.05f, 0.05f, 0.12f, backgroundOpacity);
            m_DialogueBox.style.borderTopLeftRadius = 12;
            m_DialogueBox.style.borderTopRightRadius = 12;
            m_DialogueBox.style.borderBottomLeftRadius = 12;
            m_DialogueBox.style.borderBottomRightRadius = 12;
            m_DialogueBox.style.borderTopWidth = 2;
            m_DialogueBox.style.borderBottomWidth = 2;
            m_DialogueBox.style.borderLeftWidth = 2;
            m_DialogueBox.style.borderRightWidth = 2;
            m_DialogueBox.style.borderTopColor = new Color(0.85f, 0.65f, 0.2f, 0.7f);
            m_DialogueBox.style.borderBottomColor = new Color(0.85f, 0.65f, 0.2f, 0.7f);
            m_DialogueBox.style.borderLeftColor = new Color(0.85f, 0.65f, 0.2f, 0.7f);
            m_DialogueBox.style.borderRightColor = new Color(0.85f, 0.65f, 0.2f, 0.7f);
            m_DialogueBox.style.flexDirection = FlexDirection.Row;
            m_DialogueBox.style.alignItems = Align.FlexStart;

            // --- Portrait Section ---
            m_PortraitContainer = new VisualElement();
            m_PortraitContainer.name = "portrait-container";
            m_PortraitContainer.style.width = 80;
            m_PortraitContainer.style.height = 80;
            m_PortraitContainer.style.minWidth = 80;
            m_PortraitContainer.style.marginRight = 18;
            m_PortraitContainer.style.marginTop = 5;
            m_PortraitContainer.style.borderTopLeftRadius = 40;
            m_PortraitContainer.style.borderTopRightRadius = 40;
            m_PortraitContainer.style.borderBottomLeftRadius = 40;
            m_PortraitContainer.style.borderBottomRightRadius = 40;
            m_PortraitContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.25f, 0.8f);
            m_PortraitContainer.style.borderTopWidth = 2;
            m_PortraitContainer.style.borderBottomWidth = 2;
            m_PortraitContainer.style.borderLeftWidth = 2;
            m_PortraitContainer.style.borderRightWidth = 2;
            m_PortraitContainer.style.borderTopColor = new Color(0.85f, 0.65f, 0.2f, 0.5f);
            m_PortraitContainer.style.borderBottomColor = new Color(0.85f, 0.65f, 0.2f, 0.5f);
            m_PortraitContainer.style.borderLeftColor = new Color(0.85f, 0.65f, 0.2f, 0.5f);
            m_PortraitContainer.style.borderRightColor = new Color(0.85f, 0.65f, 0.2f, 0.5f);
            m_PortraitContainer.style.justifyContent = Justify.Center;
            m_PortraitContainer.style.alignItems = Align.Center;

            // Speaker icon (placeholder - a simple icon character)
            m_SpeakerIcon = new VisualElement();
            m_SpeakerIcon.name = "speaker-icon";
            m_SpeakerIcon.style.width = 50;
            m_SpeakerIcon.style.height = 50;
            m_PortraitContainer.Add(m_SpeakerIcon);

            m_DialogueBox.Add(m_PortraitContainer);

            // --- Text Content Section ---
            var textContainer = new VisualElement();
            textContainer.name = "text-container";
            textContainer.style.flexGrow = 1;
            textContainer.style.flexShrink = 1;

            // Speaker Name
            m_SpeakerNameLabel = new Label("Speaker");
            m_SpeakerNameLabel.name = "speaker-name";
            m_SpeakerNameLabel.style.fontSize = 20;
            m_SpeakerNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            m_SpeakerNameLabel.style.color = npcNameColor;
            m_SpeakerNameLabel.style.marginBottom = 8;
            m_SpeakerNameLabel.style.letterSpacing = 1;
            textContainer.Add(m_SpeakerNameLabel);

            // Divider line
            var divider = new VisualElement();
            divider.name = "name-divider";
            divider.style.height = 1;
            divider.style.backgroundColor = new Color(0.85f, 0.65f, 0.2f, 0.4f);
            divider.style.marginBottom = 10;
            textContainer.Add(divider);

            // Dialogue Text
            m_DialogueTextLabel = new Label("...");
            m_DialogueTextLabel.name = "dialogue-text";
            m_DialogueTextLabel.style.fontSize = 16;
            m_DialogueTextLabel.style.color = dialogueTextColor;
            m_DialogueTextLabel.style.whiteSpace = WhiteSpace.Normal;
            m_DialogueTextLabel.style.flexWrap = Wrap.Wrap;
            m_DialogueTextLabel.style.flexGrow = 1;
            textContainer.Add(m_DialogueTextLabel);

            // Continue Prompt
            m_ContinuePromptLabel = new Label("▼ Nhấn E để tiếp tục...");
            m_ContinuePromptLabel.name = "continue-prompt";
            m_ContinuePromptLabel.style.fontSize = 12;
            m_ContinuePromptLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            m_ContinuePromptLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            m_ContinuePromptLabel.style.marginTop = 12;
            m_ContinuePromptLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            m_ContinuePromptLabel.style.display = DisplayStyle.None;
            textContainer.Add(m_ContinuePromptLabel);

            m_DialogueBox.Add(textContainer);
            m_DialogueContainer.Add(m_DialogueBox);

            // --- Quest Notification (top center) ---
            m_QuestNotification = new VisualElement();
            m_QuestNotification.name = "quest-notification";
            m_QuestNotification.style.position = Position.Absolute;
            m_QuestNotification.style.top = 80;
            m_QuestNotification.style.left = Length.Percent(50);
            m_QuestNotification.style.translate = new Translate(Length.Percent(-50), 0);
            m_QuestNotification.style.paddingTop = 12;
            m_QuestNotification.style.paddingBottom = 12;
            m_QuestNotification.style.paddingLeft = 30;
            m_QuestNotification.style.paddingRight = 30;
            m_QuestNotification.style.backgroundColor = new Color(0.1f, 0.1f, 0.2f, 0.9f);
            m_QuestNotification.style.borderTopLeftRadius = 8;
            m_QuestNotification.style.borderTopRightRadius = 8;
            m_QuestNotification.style.borderBottomLeftRadius = 8;
            m_QuestNotification.style.borderBottomRightRadius = 8;
            m_QuestNotification.style.borderTopWidth = 1;
            m_QuestNotification.style.borderBottomWidth = 1;
            m_QuestNotification.style.borderLeftWidth = 1;
            m_QuestNotification.style.borderRightWidth = 1;
            m_QuestNotification.style.borderTopColor = new Color(0.3f, 0.85f, 0.55f, 0.6f);
            m_QuestNotification.style.borderBottomColor = new Color(0.3f, 0.85f, 0.55f, 0.6f);
            m_QuestNotification.style.borderLeftColor = new Color(0.3f, 0.85f, 0.55f, 0.6f);
            m_QuestNotification.style.borderRightColor = new Color(0.3f, 0.85f, 0.55f, 0.6f);
            m_QuestNotification.style.display = DisplayStyle.None;

            m_QuestNotificationLabel = new Label("Nhiệm vụ đã cập nhật!");
            m_QuestNotificationLabel.name = "quest-notification-label";
            m_QuestNotificationLabel.style.fontSize = 16;
            m_QuestNotificationLabel.style.color = new Color(0.3f, 0.85f, 0.55f);
            m_QuestNotificationLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            m_QuestNotificationLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

            m_QuestNotification.Add(m_QuestNotificationLabel);
            m_DialogueContainer.Add(m_QuestNotification);

            // --- Interact Prompt (bottom center, "Press E") ---
            m_InteractPrompt = new VisualElement();
            m_InteractPrompt.name = "interact-prompt";
            m_InteractPrompt.style.position = Position.Absolute;
            m_InteractPrompt.style.bottom = 120;
            m_InteractPrompt.style.left = Length.Percent(50);
            m_InteractPrompt.style.translate = new Translate(Length.Percent(-50), 0);
            m_InteractPrompt.style.paddingTop = 10;
            m_InteractPrompt.style.paddingBottom = 10;
            m_InteractPrompt.style.paddingLeft = 25;
            m_InteractPrompt.style.paddingRight = 25;
            m_InteractPrompt.style.backgroundColor = new Color(0.05f, 0.05f, 0.15f, 0.85f);
            m_InteractPrompt.style.borderTopLeftRadius = 8;
            m_InteractPrompt.style.borderTopRightRadius = 8;
            m_InteractPrompt.style.borderBottomLeftRadius = 8;
            m_InteractPrompt.style.borderBottomRightRadius = 8;
            m_InteractPrompt.style.borderTopWidth = 2;
            m_InteractPrompt.style.borderBottomWidth = 2;
            m_InteractPrompt.style.borderLeftWidth = 2;
            m_InteractPrompt.style.borderRightWidth = 2;
            m_InteractPrompt.style.borderTopColor = new Color(0.95f, 0.75f, 0.2f, 0.6f);
            m_InteractPrompt.style.borderBottomColor = new Color(0.95f, 0.75f, 0.2f, 0.6f);
            m_InteractPrompt.style.borderLeftColor = new Color(0.95f, 0.75f, 0.2f, 0.6f);
            m_InteractPrompt.style.borderRightColor = new Color(0.95f, 0.75f, 0.2f, 0.6f);
            m_InteractPrompt.style.display = DisplayStyle.None;

            m_InteractPromptLabel = new Label("[E] Nói chuyện");
            m_InteractPromptLabel.name = "interact-prompt-label";
            m_InteractPromptLabel.style.fontSize = 18;
            m_InteractPromptLabel.style.color = new Color(0.95f, 0.85f, 0.4f);
            m_InteractPromptLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            m_InteractPromptLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

            m_InteractPrompt.Add(m_InteractPromptLabel);
            m_DialogueContainer.Add(m_InteractPrompt);

            // Add to root
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
        }

        private void HandleDialogueLineShown(string speakerName, string text, bool isPlayerLine)
        {
            m_SpeakerNameLabel.text = speakerName;
            m_SpeakerNameLabel.style.color = isPlayerLine ? playerNameColor : npcNameColor;
            m_DialogueTextLabel.text = text;

            // Change portrait background based on speaker
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
