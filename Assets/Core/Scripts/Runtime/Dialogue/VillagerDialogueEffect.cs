using UnityEngine;
using System.Collections;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// An IInteractionEffect that triggers a dialogue when the player interacts with an NPC.
    /// Attach this alongside ModularInteractable for button-press based dialogue triggering.
    /// This is an alternative to VillagerNPC's automatic trigger-based approach.
    /// </summary>
    public class VillagerDialogueEffect : MonoBehaviour, IInteractionEffect
    {
        [Header("Dialogue Settings")]
        [Tooltip("The dialogue data to play when interaction occurs.")]
        [SerializeField] private DialogueData dialogueData;

        [Tooltip("NPC display name (used for quest tracking).")]
        [SerializeField] private string villagerName = "Dân làng";

        [Tooltip("If true, play bow animation before dialogue.")]
        [SerializeField] private bool playBowAnimation = true;

        [Tooltip("Duration of the bow animation.")]
        [SerializeField] private float bowDuration = 2f;

        [Header("Effect Priority")]
        [Tooltip("Priority of this effect in the interaction chain.")]
        [SerializeField] private int priority = 10;

        private bool m_HasBeenUsed;

        /// <summary>
        /// Gets the priority of this effect.
        /// </summary>
        public int Priority => priority;

        /// <summary>
        /// Applies the dialogue effect: bow animation → dialogue → quest update.
        /// </summary>
        public IEnumerator ApplyEffect(GameObject interactor, GameObject interactable)
        {
            if (dialogueData == null)
            {
                Debug.LogError($"[VillagerDialogueEffect] No DialogueData assigned on {gameObject.name}!");
                yield break;
            }

            if (DialogueSystem.Instance == null)
            {
                Debug.LogError("[VillagerDialogueEffect] DialogueSystem.Instance is null!");
                yield break;
            }

            // Skip if already used (for one-time interactions)
            if (m_HasBeenUsed && dialogueData.playOnce)
            {
                yield break;
            }

            // --- Bow Animation ---
            if (playBowAnimation)
            {
                var playerAnimator = interactor.GetComponentInChildren<Animator>();
                if (playerAnimator != null)
                {
                    playerAnimator.SetTrigger("Bow");
                }

                var npcAnimator = interactable.GetComponentInChildren<Animator>();
                if (npcAnimator != null)
                {
                    npcAnimator.SetTrigger("Greet");
                }

                yield return new WaitForSeconds(bowDuration);
            }

            // --- Disable player movement ---
            var movement = interactor.GetComponent<CoreMovement>();
            if (movement != null)
            {
                movement.enabled = false;
            }

            // --- Start Dialogue ---
            bool dialogueComplete = false;
            DialogueSystem.Instance.StartDialogue(dialogueData, () =>
            {
                dialogueComplete = true;
            });

            // Wait for dialogue completion
            while (!dialogueComplete)
            {
                yield return null;
            }

            // --- Re-enable movement ---
            if (movement != null)
            {
                movement.enabled = true;
            }

            m_HasBeenUsed = true;

            // Notify quest manager
            if (VillageQuestManager.Instance != null)
            {
                VillageQuestManager.Instance.OnVillagerDialogueCompleted(villagerName);
            }
        }

        /// <summary>
        /// Called when the effect is cancelled before completion.
        /// </summary>
        public void CancelEffect(GameObject interactor)
        {
            // Re-enable movement if it was disabled
            if (interactor != null)
            {
                var movement = interactor.GetComponent<CoreMovement>();
                if (movement != null)
                {
                    movement.enabled = true;
                }
            }

            // Force-end any active dialogue
            if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialoguePlaying)
            {
                DialogueSystem.Instance.ForceEndDialogue();
            }
        }
    }
}
