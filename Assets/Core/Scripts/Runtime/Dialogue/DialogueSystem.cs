using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Central dialogue system that manages dialogue playback, UI display, and completion tracking.
    /// Singleton pattern - attach to a persistent GameObject in the scene.
    /// </summary>
    public class DialogueSystem : MonoBehaviour
    {
        #region Singleton

        public static DialogueSystem Instance { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// Fired when a dialogue starts. Passes the NPC name.
        /// </summary>
        public event Action<string> OnDialogueStarted;

        /// <summary>
        /// Fired when a new dialogue line is displayed.
        /// Parameters: speakerName, text, isPlayerLine
        /// </summary>
        public event Action<string, string, bool> OnDialogueLineShown;

        /// <summary>
        /// Fired when a dialogue ends. Passes the NPC name.
        /// </summary>
        public event Action<string> OnDialogueEnded;

        /// <summary>
        /// Fired when player input is needed to advance dialogue.
        /// </summary>
        public event Action OnWaitingForInput;

        #endregion

        #region Fields & Properties

        [Header("Settings")]
        [Tooltip("Time in seconds for the typewriter effect per character.")]
        [SerializeField] private float typewriterSpeed = 0.03f;

        [Tooltip("If true, enables typewriter text effect.")]
        [SerializeField] private bool useTypewriterEffect = true;

        /// <summary>
        /// Returns true if a dialogue is currently playing.
        /// </summary>
        public bool IsDialoguePlaying { get; private set; }

        /// <summary>
        /// Returns the name of the current NPC being talked to.
        /// </summary>
        public string CurrentNPCName { get; private set; }

        // Internal state
        private DialogueData m_CurrentDialogue;
        private int m_CurrentLineIndex;
        private Coroutine m_DialogueCoroutine;
        private Coroutine m_TypewriterCoroutine;
        private bool m_IsTyping;
        private bool m_SkipRequested;
        private string m_FullCurrentText;

        // Track which dialogues have been completed
        private readonly HashSet<string> m_CompletedDialogues = new HashSet<string>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[DialogueSystem] Duplicate instance detected. Destroying self.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            if (!IsDialoguePlaying) return;

            // Check for input using New Input System
            bool inputPressed = false;

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (keyboard != null)
            {
                inputPressed = keyboard.eKey.wasPressedThisFrame
                            || keyboard.spaceKey.wasPressedThisFrame
                            || keyboard.enterKey.wasPressedThisFrame;
            }

            if (!inputPressed && mouse != null)
            {
                inputPressed = mouse.leftButton.wasPressedThisFrame;
            }

            if (inputPressed)
            {
                if (m_IsTyping)
                {
                    // Skip typewriter - show full text immediately
                    m_SkipRequested = true;
                }
                else
                {
                    // Advance to next line
                    AdvanceDialogue();
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts playing a dialogue sequence.
        /// </summary>
        /// <param name="dialogueData">The dialogue data to play.</param>
        /// <param name="onComplete">Optional callback when dialogue finishes.</param>
        public void StartDialogue(DialogueData dialogueData, Action onComplete = null)
        {
            if (dialogueData == null)
            {
                Debug.LogError("[DialogueSystem] Cannot start dialogue: dialogueData is null.");
                return;
            }

            if (IsDialoguePlaying)
            {
                Debug.LogWarning("[DialogueSystem] Cannot start new dialogue while another is playing.");
                return;
            }

            // Check if dialogue was already played (and is play-once)
            if (dialogueData.playOnce && m_CompletedDialogues.Contains(dialogueData.npcName))
            {
                Debug.Log($"[DialogueSystem] Dialogue for '{dialogueData.npcName}' already completed.");
                onComplete?.Invoke();
                return;
            }

            m_CurrentDialogue = dialogueData;
            m_CurrentLineIndex = 0;
            IsDialoguePlaying = true;
            CurrentNPCName = dialogueData.npcName;

            // Lock cursor for dialogue
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            OnDialogueStarted?.Invoke(dialogueData.npcName);

            // Show first line
            ShowCurrentLine();

            // Store completion callback
            m_DialogueCoroutine = StartCoroutine(WaitForDialogueEnd(onComplete));
        }

        /// <summary>
        /// Checks if a specific NPC's dialogue has been completed.
        /// </summary>
        /// <param name="npcName">The NPC name to check.</param>
        /// <returns>True if the dialogue has been completed.</returns>
        public bool IsDialogueCompleted(string npcName)
        {
            return m_CompletedDialogues.Contains(npcName);
        }

        /// <summary>
        /// Gets the number of completed dialogues.
        /// </summary>
        public int CompletedDialogueCount => m_CompletedDialogues.Count;

        /// <summary>
        /// Force-ends the current dialogue.
        /// </summary>
        public void ForceEndDialogue()
        {
            if (!IsDialoguePlaying) return;
            EndDialogue();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Shows the current dialogue line with optional typewriter effect.
        /// </summary>
        private void ShowCurrentLine()
        {
            if (m_CurrentDialogue == null || m_CurrentLineIndex >= m_CurrentDialogue.lines.Count)
            {
                EndDialogue();
                return;
            }

            var line = m_CurrentDialogue.lines[m_CurrentLineIndex];
            m_FullCurrentText = line.text;

            if (useTypewriterEffect)
            {
                if (m_TypewriterCoroutine != null)
                {
                    StopCoroutine(m_TypewriterCoroutine);
                }
                m_TypewriterCoroutine = StartCoroutine(TypewriterEffect(line));
            }
            else
            {
                OnDialogueLineShown?.Invoke(line.speakerName, line.text, line.isPlayerLine);
                m_IsTyping = false;
                OnWaitingForInput?.Invoke();
            }
        }

        /// <summary>
        /// Typewriter coroutine that reveals text character by character.
        /// </summary>
        private IEnumerator TypewriterEffect(DialogueLine line)
        {
            m_IsTyping = true;
            m_SkipRequested = false;
            string displayText = "";

            for (int i = 0; i < line.text.Length; i++)
            {
                if (m_SkipRequested)
                {
                    // Show full text immediately
                    OnDialogueLineShown?.Invoke(line.speakerName, line.text, line.isPlayerLine);
                    break;
                }

                displayText += line.text[i];
                OnDialogueLineShown?.Invoke(line.speakerName, displayText, line.isPlayerLine);
                yield return new WaitForSeconds(typewriterSpeed);
            }

            // Ensure full text is shown
            if (!m_SkipRequested)
            {
                OnDialogueLineShown?.Invoke(line.speakerName, line.text, line.isPlayerLine);
            }

            m_IsTyping = false;
            m_SkipRequested = false;
            OnWaitingForInput?.Invoke();
        }

        /// <summary>
        /// Advances to the next dialogue line.
        /// </summary>
        private void AdvanceDialogue()
        {
            m_CurrentLineIndex++;

            if (m_CurrentLineIndex >= m_CurrentDialogue.lines.Count)
            {
                EndDialogue();
            }
            else
            {
                ShowCurrentLine();
            }
        }

        /// <summary>
        /// Ends the current dialogue session.
        /// </summary>
        private void EndDialogue()
        {
            if (m_TypewriterCoroutine != null)
            {
                StopCoroutine(m_TypewriterCoroutine);
                m_TypewriterCoroutine = null;
            }

            string npcName = m_CurrentDialogue?.npcName;

            // Mark as completed
            if (npcName != null)
            {
                m_CompletedDialogues.Add(npcName);
            }

            m_CurrentDialogue = null;
            m_CurrentLineIndex = 0;
            IsDialoguePlaying = false;
            CurrentNPCName = null;
            m_IsTyping = false;

            // Re-lock cursor for gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            OnDialogueEnded?.Invoke(npcName);
        }

        /// <summary>
        /// Waits for the dialogue to end and then invokes the completion callback.
        /// </summary>
        private IEnumerator WaitForDialogueEnd(Action onComplete)
        {
            while (IsDialoguePlaying)
            {
                yield return null;
            }

            onComplete?.Invoke();
            m_DialogueCoroutine = null;
        }

        #endregion
    }
}
