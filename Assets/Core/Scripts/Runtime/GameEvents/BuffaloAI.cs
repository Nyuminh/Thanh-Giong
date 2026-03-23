using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Blocks.Gameplay.Core;

public class BuffaloAI : MonoBehaviour
{
    [Header("Quest Settings")]
    [Tooltip("Tên cho Trâu (VD: Trâu Lạc)")]
    public string buffaloName = "Con Trâu";
    
    [Tooltip("Thuộc bước Quest số mấy? (Để báo về UI nhiệm vụ La bàn)")]
    public int catchQuestStep = -1; 

    [Header("Movement Settings")]
    public float wanderRadius = 15f;
    public float idleTime = 3f;
    public float walkSpeed = 1.5f;     // Tốc độ lững thững đi dạo
    
    [Header("Flee Settings")]
    public float fleeDetectionRange = 7f; // Cách M mét là bắt đầu chạy trốn
    public float fleeSpeed = 5.5f;     // Tốc độ phóng đi (Player phải chạy nhanh mới kịp)

    [Header("Catch & Follow Settings")]
    public float catchRadius = 2.5f;   // Lại gần cỡ 2.5m mới hiện nút F
    public float followDistance = 3.5f;  // Khoảng cách dắt trâu theo sau
    public float followSpeed = 4f;       // Tốc độ trâu lững thững đi theo

    private NavMeshAgent agent;
    private Animator anim;
    private float timer;

    private Transform player;
    private bool isCaught = false;
    private bool playerInRangeToCatch = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        agent.speed = walkSpeed;

        // Đăng ký vị trí Trâu với VillageQuestManager để hiện la bàn chĩa về nó
        if (catchQuestStep >= 0 && VillageQuestManager.Instance != null)
        {
            VillageQuestManager.Instance.RegisterQuestTarget(catchQuestStep, this.transform, buffaloName);
            VillageQuestManager.Instance.UpdateTotalSteps(catchQuestStep + 1);
        }
    }

    void Update()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            return;
        }

        if (isCaught)
        {
            // Trạng thái đã bị thuần phục: Đi theo Gióng
            FollowPlayer();
        }
        else
        {
            // Trạng thái hoang dã: Kiểm tra khoảng cách
            float dist = Vector3.Distance(transform.position, player.position);

            // Kiểm tra tiến trình Quest để xem có được phép bắt chưa
            bool canCatch = true;
            if (catchQuestStep >= 0 && VillageQuestManager.Instance != null)
            {
                canCatch = (VillageQuestManager.Instance.GetCurrentQuestStep() == catchQuestStep);
            }

            // Logic hiện nút F nếu vào vùng Catch
            bool wasInRange = playerInRangeToCatch;
            playerInRangeToCatch = canCatch && (dist <= catchRadius);

            if (playerInRangeToCatch && !wasInRange)
            {
                ShowInteractPrompt();
            }
            else if (!playerInRangeToCatch && wasInRange)
            {
                HideInteractPrompt();
            }

            // Xử lý nút bấm
            if (playerInRangeToCatch)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
                {
                    CatchBuffalo(); // Gọi hàm Bắt Trâu
                    return;
                }
            }

            // AI Di chuyển (Trốn hoặc Đi dạo)
            if (dist <= fleeDetectionRange && canCatch)
            {
                FleeFromPlayer(); // Thấy tới gần là chạy tốc biến!
            }
            else
            {
                WanderLogic(); // Ở xa thì lững thững đi bộ gặm cỏ
            }
        }

        UpdateAnimation();
    }

    private void CatchBuffalo()
    {
        isCaught = true;
        HideInteractPrompt();
        
        Debug.Log($"[BuffaloAI] Đã tóm được {buffaloName}");

        // Cập nhật quest trên quản lý nhiệm vụ (Qua bước tiếp theo)
        if (catchQuestStep >= 0 && VillageQuestManager.Instance != null)
        {
            VillageQuestManager.Instance.OnVillagerDialogueCompleted(buffaloName, catchQuestStep);
        }
    }

    private void FleeFromPlayer()
    {
        agent.speed = fleeSpeed;
        agent.isStopped = false;

        // Sợ Gióng, lấy hướng cắm đầu chạy Ngược Lại với Gióng
        Vector3 fleeDirection = (transform.position - player.position).normalized;
        Vector3 newPos = transform.position + fleeDirection * fleeSpeed; // Chạy ra xa thêm

        NavMeshHit hit;
        // Quét tìm khu vực an toàn không xuyên tường
        if (NavMesh.SamplePosition(newPos, out hit, 4f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private void FollowPlayer()
    {
        agent.speed = followSpeed;
        float distance = Vector3.Distance(transform.position, player.position);
        
        // Gióng đi xa mới chạy bước theo
        if (distance > followDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            // Đủ khoảng cách ngoan ngoãn thì đứng lại
            agent.isStopped = true;
            
            // Xoay đầu về hướng người chủ
            Vector3 dir = (player.position - transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
            }
        }
    }

    private void WanderLogic()
    {
        agent.speed = walkSpeed;
        agent.isStopped = false;
        timer += Time.deltaTime;

        if (timer >= idleTime)
        {
            Vector3 newPos = RandomNavMeshLocation(wanderRadius);
            agent.SetDestination(newPos);
            timer = 0;
        }
    }

    private Vector3 RandomNavMeshLocation(float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += transform.position;
        NavMeshHit hit;
        Vector3 finalPosition = transform.position;

        if (NavMesh.SamplePosition(randomDirection, out hit, radius, 1))
        {
            finalPosition = hit.position;
        }
        return finalPosition;
    }

    private void UpdateAnimation()
    {
        if (anim == null) return;
        
        // Tốc độ thực tế đang di chuyển của trâu
        float speedStr = agent.velocity.magnitude;
        anim.SetFloat("Speed", speedStr);
    }

    private void ShowInteractPrompt()
    {
        var dialogueUI = Object.FindObjectOfType<DialogueUI>();
        if (dialogueUI != null)
        {
            dialogueUI.ShowInteractPrompt($"Nhấn F để dắt {buffaloName}");
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
}
