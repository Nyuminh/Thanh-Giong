using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Script gắn vào các vật phẩm có thể nhặt được trong chuỗi nhiệm vụ (vd: Con Diều).
    /// </summary>
    public class QuestItemInteract : MonoBehaviour
    {
        [Header("Item Info")]
        [SerializeField] private string itemName = "Con Diều";
        
        [Header("Quest Step")]
        [Tooltip("Bước đánh dấu lúc cần đi nhặt vật phẩm này")]
        [SerializeField] private int questStep;

        [Header("Interaction Settings")]
        [SerializeField] private float interactionRadius = 4f;
        [Tooltip("Hiệu ứng/UI đánh dấu vật phẩm (tùy chọn)")]
        [SerializeField] private GameObject questMarker;

        [Header("Events (Rất quan trọng)")]
        [Tooltip("Gọi khi nhặt vật phẩm. Kéo Mô hình diều trên cây vào -> Tắt")]
        public UnityEvent OnPickedUp;

        [Header("Player Equipment (Dành cho Player sinh ra lúc Play)")]
        [Tooltip("Gõ chính xác TÊN của cục diều đang bị ĐÓNG (ẩn) trên tay/lưng prefab của Gióng. Script sẽ tự tìm và BẬT nó lên!")]
        [SerializeField] private string playerEquipObjectName = "";

        private bool m_HasInteracted;
        private bool m_PlayerInRange;
        private Transform m_PlayerTransform;

        private void Start()
        {
            if (questMarker != null) questMarker.SetActive(false);
            
            // Đăng ký vị trí của Item này với Manager để La bàn chĩa về đây
            if (VillageQuestManager.Instance != null)
            {
                VillageQuestManager.Instance.RegisterQuestTarget(questStep, this.transform, itemName);
                VillageQuestManager.Instance.UpdateTotalSteps(questStep + 1);
            }
        }

        private void Update()
        {
            if (m_HasInteracted) return;

            if (VillageQuestManager.Instance == null) return;
            int currentStep = VillageQuestManager.Instance.GetCurrentQuestStep();

            if (currentStep == questStep)
            {
                if (questMarker != null && !questMarker.activeSelf)
                    questMarker.SetActive(true);
            }
            else
            {
                if (questMarker != null && questMarker.activeSelf)
                    questMarker.SetActive(false);
                    
                if (m_PlayerInRange)
                {
                    m_PlayerInRange = false;
                    HideInteractPrompt();
                }
                return;
            }

            if (m_PlayerTransform == null)
            {
                FindPlayer();
                if (m_PlayerTransform == null) return;
            }

            float distance = Vector3.Distance(transform.position, m_PlayerTransform.position);
            bool wasInRange = m_PlayerInRange;
            m_PlayerInRange = distance <= interactionRadius;

            if (m_PlayerInRange && !wasInRange)
            {
                ShowInteractPrompt();
            }
            else if (!m_PlayerInRange && wasInRange)
            {
                HideInteractPrompt();
            }

            if (m_PlayerInRange)
            {
                var keyboard = Keyboard.current;
                // Bấm phím F để nhặt
                if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
                {
                    PickUp();
                }
            }
        }

        private void PickUp()
        {
            m_HasInteracted = true;
            m_PlayerInRange = false;

            HideInteractPrompt();
            if (questMarker != null) questMarker.SetActive(false);

            Debug.Log($"[QuestItem] Đã nhặt: {itemName}");

            // Kích hoạt các sự kiện Unity (Bật/Tắt model)
            OnPickedUp?.Invoke();

            // Nếu điền tên Object trên người Gióng, tự động tìm và BẬT nó
            if (!string.IsNullOrEmpty(playerEquipObjectName) && m_PlayerTransform != null)
            {
                Transform[] allChildren = m_PlayerTransform.GetComponentsInChildren<Transform>(true);
                foreach (var child in allChildren)
                {
                    if (child.name == playerEquipObjectName)
                    {
                        child.gameObject.SetActive(true);
                        Debug.Log($"[QuestItem] Đã BẬT {playerEquipObjectName} trên người Gióng.");
                        break;
                    }
                }
            }

            // Thông báo hoàn thành bước cho Manager
            VillageQuestManager.Instance.OnVillagerDialogueCompleted(itemName, questStep);

            // Xóa script này hoặc Object này sau khi nhặt
            // Tạm thời mình chỉ tắt Script, bạn có thể tự Disable Gameobject trong Unity Event
            this.enabled = false; 
        }

        private void FindPlayer()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                m_PlayerTransform = playerObj.transform;
            }
        }

        private void ShowInteractPrompt()
        {
            var dialogueUI = Object.FindObjectOfType<DialogueUI>();
            if (dialogueUI != null)
            {
                dialogueUI.ShowInteractPrompt($"Nhấn F để nhặt {itemName}");
            }
        }

        private void HideInteractPrompt()
        {
            var dialogueUI = Object.FindObjectOfType<DialogueUI>();
            if (dialogueUI != null)
            {
                dialogueUI.HideInteractPrompt();
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = m_HasInteracted ? Color.gray : Color.blue;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
