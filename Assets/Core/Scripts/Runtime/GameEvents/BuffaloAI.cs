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
    
    [Header("Vật Lộn Bắt Trâu (Khó hơn)")]
    [Tooltip("Số lần phải bấm F mới thu phục được (VD: 5 lần là 20% máu/lần)")]
    public int totalCatchesRequired = 5;
    [Tooltip("Khoảng cách trâu bị văng ra sau khi giằng co thành công 1 nhịp")]
    public float pushbackDistance = 8f;
    private int catchesDone = 0;

    /// <summary>Remaining HP as 0..1 ratio (1 = full, 0 = caught).</summary>
    public float HealthRatio =>
        1f - (float)catchesDone / Mathf.Max(1, totalCatchesRequired);

    /// <summary>True after the buffalo has been fully tamed.</summary>
    public bool IsCaught => isCaught;
    
    [Header("Audio Settings")]
    [Tooltip("Tiếng Trâu kêu la giằng co phát ra mỗi khi người chơi bấm F tóm được")]
    public AudioClip catchSound;

    [Header("Animation Settings")]
    [Tooltip("Tên của biễn (Trigger) trong Animator cho cảnh Trâu giãy/chạy hoảng hốt lúc bị bấm F")]
    public string hitAnimTrigger = "Hit";

    [Tooltip("Kéo thả NPC (ví dụ Cậu Bé) vào đây. Khi thu phục xong Trâu sẽ chạy theo NPC này. Nếu để trống sẽ chạy theo Gióng.")]
    public Transform targetToFollow;

    private NavMeshAgent agent;
    private Animator anim;
    private float timer;

    private Transform player;
    private bool isCaught = false;
    private bool playerInRangeToCatch = false;
    private bool isBeingPushed = false; // Trạng thái đang bị văng ra xa

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

        // Bỏ qua mọi xử lý AI khi Trâu đang bị văng vật lộn ra xa
        if (isBeingPushed) return;

        if (isCaught)
        {
            // Trạng thái đã bị thuần phục: Đi theo mục tiêu (NPC hoặc Gióng)
            FollowTarget();
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
                FleeFromPlayerSmart(); // Gọi thuật toán trốn tường thông minh
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
        catchesDone++;
        HideInteractPrompt();

        // Phát tiếng la lối/kêu của trâu ngay khi người chơi đụng tay
        if (catchSound != null)
        {
            AudioSource.PlayClipAtPoint(catchSound, transform.position);
        }

        // Gọi hiệu ứng Animation giãy giụa (kích hoạt Trigger)
        if (anim != null && !string.IsNullOrEmpty(hitAnimTrigger))
        {
            anim.SetTrigger(hitAnimTrigger);
        }

        if (catchesDone >= totalCatchesRequired)
        {
            // NHÁT CHÓT: HOÀN TOÀN THU PHỤC
            isCaught = true;
            Debug.Log($"[BuffaloAI] Đã thu phục được {buffaloName}");

            // Gửi tín hiệu hoàn thành bước Quest
            if (catchQuestStep >= 0 && VillageQuestManager.Instance != null)
            {
                VillageQuestManager.Instance.OnVillagerDialogueCompleted(buffaloName, catchQuestStep);
            }
        }
        else
        {
            // VẬT LỘN: TRÂU VẪN CÒN SỨC, GIẰNG RA XA
            Debug.Log($"[BuffaloAI] Đã tóm {catchesDone}/{totalCatchesRequired}. Trâu vùng vẫy văng ra xa!");
            
            // Tính hướng đẩy văng (Hướng từ Gióng đâm thẳng qua Trâu)
            Vector3 pushDirection = (transform.position - player.position).normalized;
            Vector3 pushTargetPos = transform.position + pushDirection * pushbackDistance;

            NavMeshHit hit;
            // Tìm điểm rớt trên Navmesh để Trâu không lọt vách núi
            if (NavMesh.SamplePosition(pushTargetPos, out hit, 5f, NavMesh.AllAreas))
            {
                // Ép Trâu bay tức thì tới vị trí lùi lại bằng hiệu ứng trượt
                StartCoroutine(PushbackRoutine(transform.position, hit.position));
            }
        }
    }

    // Biến cảnh văng Trâu thành "Lướt" mềm mại
    private System.Collections.IEnumerator PushbackRoutine(Vector3 start, Vector3 end)
    {
        isBeingPushed = true;
        agent.enabled = false; // Tắt AI để can thiệp vật lý tay

        float elapsed = 0f;
        float duration = 0.25f; // Thời gian bật ngửa siêu âm (0.25 giây)

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // Làm mượt đường trượt bằng hàm Lerp
            transform.position = Vector3.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        transform.position = end;
        agent.enabled = true; // Quăng ngược lại cho Navmesh kiểm soát
        isBeingPushed = false;
    }

    private void FleeFromPlayerSmart()
    {
        // Kiểm tra tránh vướng tường thông minh
        agent.speed = fleeSpeed;
        agent.isStopped = false;

        Vector3 bestPos = transform.position;
        float maxDist = -1f;

        // Quét 8 hướng xung quanh như Rada tìm lối thoát
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Vector3 checkPos = transform.position + dir * fleeSpeed;

            NavMeshHit hit;
            // Nếu hẻm này chui Navmesh được
            if (NavMesh.SamplePosition(checkPos, out hit, 2f, NavMesh.AllAreas))
            {
                // Đo xa cách với Gióng
                float d = Vector3.Distance(hit.position, player.position);
                if (d > maxDist)
                {
                    maxDist = d;      // Chọn hẻm này làm hẻm tốt nhất vì cách Gióng xa nhất!
                    bestPos = hit.position;
                }
            }
        }

        // Bỏ chạy vào hẻm mới bói được
        agent.SetDestination(bestPos);
    }

    private void FollowTarget()
    {
        // Ưu tiên target NPC trước, nếu không truyền gì thì mới chọn Player
        Transform target = (targetToFollow != null) ? targetToFollow : player;
        if (target == null) return;

        agent.speed = followSpeed;
        float distance = Vector3.Distance(transform.position, target.position);
        
        // Mục tiêu đi xa mới chạy bước theo
        if (distance > followDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(target.position);
        }
        else
        {
            // Đủ khoảng cách ngoan ngoãn thì đứng lại
            agent.isStopped = true;
            
            // Xoay đầu về hướng người chủ
            Vector3 dir = (target.position - transform.position).normalized;
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
            if (totalCatchesRequired > 1)
            {
                int healthPercent = 100 - (catchesDone * 100 / totalCatchesRequired);
                dialogueUI.ShowInteractPrompt($"Nhấn F vật lộn trâu (Máu: {healthPercent}%)");
            }
            else
            {
                dialogueUI.ShowInteractPrompt($"Nhấn F để dắt {buffaloName}");
            }
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
