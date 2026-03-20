using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Villager NPC - player must press E to talk.
    /// NPC walks around normally via VillagerAI.
    /// Only shows E prompt when player is near AND it's the right quest step.
    /// </summary>
    public class VillagerNPC : MonoBehaviour
    {
        #region Fields

        [Header("NPC Info")]
        [SerializeField] private string villagerName = "Dân làng";
        [SerializeField] private DialogueData dialogueData;

        [Header("Quest Step")]
        [Tooltip("Which quest step this NPC belongs to (0 = first, 1 = second, etc.)")]
        [SerializeField] private int questStep = 0;

        [Header("Interaction Settings")]
        [SerializeField] private float interactionRadius = 5f;

        [Header("Visual Indicators")]
        [SerializeField] private GameObject questMarker;
        [SerializeField] private GameObject completedMarker;

        // Internal state
        private bool m_HasInteracted;
        private bool m_IsInDialogue;
        private bool m_PlayerInRange;
        private Transform m_PlayerTransform;
        private GameObject m_PlayerGameObject;

        #endregion

        #region Properties

        public string VillagerName => villagerName;
        public bool HasInteracted => m_HasInteracted;
        public int QuestStep => questStep;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (questMarker != null) questMarker.SetActive(true);
            if (completedMarker != null) completedMarker.SetActive(false);
        }

        private void Update()
        {
            if (m_HasInteracted || m_IsInDialogue) return;

            // Check if this NPC's quest step is the current step
            if (VillageQuestManager.Instance == null) return;
            int currentStep = VillageQuestManager.Instance.GetCurrentQuestStep();

            if (currentStep != questStep)
            {
                // Not our turn
                if (m_PlayerInRange)
                {
                    m_PlayerInRange = false;
                    HideInteractPrompt();
                }
                return;
            }

            // Find player if needed
            if (m_PlayerTransform == null)
            {
                FindPlayer();
                if (m_PlayerTransform == null) return;
            }

            // Check distance
            float distance = Vector3.Distance(transform.position, m_PlayerTransform.position);
            bool wasInRange = m_PlayerInRange;
            m_PlayerInRange = distance <= interactionRadius;

            if (m_PlayerInRange && !wasInRange)
            {
                // Player just entered range - show E prompt
                ShowInteractPrompt();
            }
            else if (!m_PlayerInRange && wasInRange)
            {
                // Player just left range - hide E prompt
                HideInteractPrompt();
            }

            // Check for E key press while in range
            if (m_PlayerInRange)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                {
                    // Start dialogue!
                    StartCoroutine(DialogueSequence(m_PlayerGameObject));
                }
            }
        }

        #endregion

        #region Player Detection

        private void FindPlayer()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                m_PlayerTransform = playerObj.transform;
                m_PlayerGameObject = playerObj;
                return;
            }

            var playerManager = Object.FindObjectOfType<CorePlayerManager>();
            if (playerManager != null)
            {
                m_PlayerTransform = playerManager.transform;
                m_PlayerGameObject = playerManager.gameObject;
            }
        }

        #endregion

        #region Interact Prompt

        private void ShowInteractPrompt()
        {
            // Show "Press E" via DialogueUI
            var dialogueUI = Object.FindObjectOfType<DialogueUI>();
            if (dialogueUI != null)
            {
                dialogueUI.ShowInteractPrompt($"Nhấn E để nói chuyện với {villagerName}");
            }
            Debug.Log($"[VillagerNPC] Player near {villagerName} - press E to talk");
        }

        private void HideInteractPrompt()
        {
            var dialogueUI = Object.FindObjectOfType<DialogueUI>();
            if (dialogueUI != null)
            {
                dialogueUI.HideInteractPrompt();
            }
        }

        #endregion

        #region Dialogue

        private IEnumerator DialogueSequence(GameObject player)
        {
            if (DialogueSystem.Instance == null || dialogueData == null) yield break;
            if (DialogueSystem.Instance.IsDialoguePlaying) yield break;
            AudioSource audio = GetComponent<AudioSource>();
            if (BGMManager.Instance != null) BGMManager.Instance.LowerBGM();
            if (audio != null && audio.isPlaying)
            {
                audio.Stop();
                Debug.Log($"[VillagerNPC] Stopped background audio for {villagerName}");
            }
            m_IsInDialogue = true;

            // Hide E prompt
            HideInteractPrompt();

            Debug.Log($"[VillagerNPC] Starting dialogue step {questStep}: {villagerName}");

            // Start dialogue - DON'T disable player movement
            bool dialogueComplete = false;
            DialogueSystem.Instance.StartDialogue(dialogueData, () =>
            {
                dialogueComplete = true;
            });

            // Wait for dialogue to finish
            while (!dialogueComplete)
            {
                yield return null;
            }

            // Mark completed
            m_HasInteracted = true;
            m_IsInDialogue = false;
            m_PlayerInRange = false;

            // Update visuals
            if (questMarker != null) questMarker.SetActive(false);
            if (completedMarker != null) completedMarker.SetActive(true);

            Debug.Log($"[VillagerNPC] Completed step {questStep}: {villagerName}");

            // Notify quest manager
            if (VillageQuestManager.Instance != null)
            {
                VillageQuestManager.Instance.OnVillagerDialogueCompleted(villagerName);
            }
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = m_HasInteracted ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }

        #endregion
    }
}
