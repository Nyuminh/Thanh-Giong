using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Events;

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

        [Header("Quest Steps (Mỗi bước = 1 lượt nói chuyện)")]
        [Tooltip("Cài đặt mảng các bước nhiệm vụ mà NPC này sẽ xuất hiện. VD: Mẹ đứng lấy diều = Bước 4 và Bước 6")]
        [SerializeField] private int[] questSteps = new int[] { 0 };
        
        [Tooltip("Mỗi bước tương ứng với 1 đoạn hội thoại (theo thứ tự mảng questSteps)")]
        [SerializeField] private DialogueData[] dialogueDatas;

        [Header("Events (Rất quan trọng)")]
        [Tooltip("Những sự kiện sẽ CHẠY LÊN khi Gióng nói chuyện XONG với NPC (mỗi phần tử tương ứng với questSteps ở trên). VD: Lúc Gióng trả diều, bạn kéo model diều trên tay Gióng vào đây và SetActive(false).")]
        public UnityEvent[] onStepCompleted;

        [Header("Player Equipment (Dành cho Player sinh ra lúc Play)")]
        [Tooltip("Nếu Gióng được sinh ra lúc Play, gõ tên Con Diều đang ĐƯỢC BẬT trên người vào đây (mỗi phần tử tương ứng với questSteps). Script sẽ TỰ TÌM bằng tên và TẮT nó đi.")]
        public string[] hidePlayerEquipNames;

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
        public int[] QuestSteps => questSteps;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (questMarker != null) questMarker.SetActive(false);
            if (completedMarker != null) completedMarker.SetActive(false);
        }

        private void Update()
        {
            if (m_IsInDialogue) return;

            // Check if this NPC's quest step is the current step
            if (VillageQuestManager.Instance == null) return;
            int currentStep = VillageQuestManager.Instance.GetCurrentQuestStep();

            bool isMyTurn = false;
            foreach (int step in questSteps)
            {
                if (step == currentStep)
                {
                    isMyTurn = true;
                    break;
                }
            }

            if (isMyTurn)
            {
                if (questMarker != null && !questMarker.activeSelf)
                    questMarker.SetActive(true);
            }
            else
            {
                if (questMarker != null && questMarker.activeSelf)
                    questMarker.SetActive(false);
            }
            if (!isMyTurn)
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
            if (DialogueSystem.Instance == null) yield break;
            if (DialogueSystem.Instance.IsDialoguePlaying) yield break;

            // Tìm Data hội thoại phù hợp với step hiện tại
            int currentStep = VillageQuestManager.Instance.GetCurrentQuestStep();
            int phaseIndex = -1;
            DialogueData currentDialogue = null;
            
            for (int i = 0; i < questSteps.Length; i++)
            {
                if (questSteps[i] == currentStep)
                {
                    phaseIndex = i;
                    if (dialogueDatas != null && i < dialogueDatas.Length)
                        currentDialogue = dialogueDatas[i];
                    break;
                }
            }

            if (currentDialogue == null)
            {
                Debug.LogWarning($"[VillagerNPC] Không có DialogueData cho step {currentStep}");
                yield break;
            }

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

            Debug.Log($"[VillagerNPC] Starting dialogue step {currentStep}: {villagerName}");

            // Start dialogue - DON'T disable player movement
            bool dialogueComplete = false;
            DialogueSystem.Instance.StartDialogue(currentDialogue, () =>
            {
                dialogueComplete = true;
            });

            // Wait for dialogue to finish
            while (!dialogueComplete)
            {
                yield return null;
            }

            m_IsInDialogue = false;
            m_PlayerInRange = false;

            // Kích hoạt sự kiện Unity khi NPC nói xong
            if (onStepCompleted != null && phaseIndex >= 0 && phaseIndex < onStepCompleted.Length)
            {
                onStepCompleted[phaseIndex]?.Invoke();
            }

            // Tính năng TẮT model trên người Gióng dựa theo tên cho Player sinh ra lúc Play
            if (hidePlayerEquipNames != null && phaseIndex >= 0 && phaseIndex < hidePlayerEquipNames.Length)
            {
                string objName = hidePlayerEquipNames[phaseIndex];
                if (!string.IsNullOrEmpty(objName) && player != null)
                {
                    Transform[] allChildren = player.GetComponentsInChildren<Transform>(true);
                    foreach (var child in allChildren)
                    {
                        if (child.name == objName)
                        {
                            child.gameObject.SetActive(false);
                            Debug.Log($"[VillagerNPC] Đã TẮT {objName} trên người Gióng bằng tên.");
                            break;
                        }
                    }
                }
            }

            // Update visuals
            if (questMarker != null) questMarker.SetActive(false);
            if (completedMarker != null) completedMarker.SetActive(true);

            // Notify quest manager
            if (VillageQuestManager.Instance != null)
            {
                VillageQuestManager.Instance.OnVillagerDialogueCompleted(villagerName, currentStep);
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
