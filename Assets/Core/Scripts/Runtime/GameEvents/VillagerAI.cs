using UnityEngine;
using UnityEngine.AI;

public class VillagerAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float wanderRadius = 10f;     // Bán kính đi dạo
    public float idleTime = 3f;          // Thời gian đứng nghỉ

    [Header("Detection Settings")]
    public float detectionRange = 5f;    // Khoảng cách nhận biết người chơi
    public Transform player;             // Tham chiếu tới người chơi

    private NavMeshAgent agent;
    private Animator anim;
    private float timer;
    private bool isPlayerNearby = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        // Thử tìm ở chính nó, nếu không có thì tìm ở các object con
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        timer = idleTime;
    }
    // Tên hàm phải viết CHÍNH XÁC từng chữ cái như trong thông báo lỗi
    public void OnFootstepWalk()
    {
        // Đây là nơi bạn phát âm thanh bước chân hoặc tạo hiệu ứng bụi
        // Debug.Log("Nhân vật đang chạy!"); 
    }
    void Update()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            return; // Dừng Update frame này để đợi frame sau có player rồi mới chạy logic tiếp
        }
        CheckPlayerDistance();

        if (isPlayerNearby)
        {
            StopMovingAndLook();
        }
        else
        {
            WanderLogic();
        }

        UpdateAnimation();
    }

    void CheckPlayerDistance()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        isPlayerNearby = (distance <= detectionRange);
    }

    void StopMovingAndLook()
    {
        // Dừng NavMeshAgent
        agent.isStopped = true;

        // Xoay mặt về phía người chơi một cách mượt mà
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Giữ thăng bằng không cho AI ngửa đầu lên trời

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    void WanderLogic()
    {
        agent.isStopped = false;
        timer += Time.deltaTime;

        // Nếu đã nghỉ đủ thời gian, tìm điểm đi mới
        if (timer >= idleTime)
        {
            Vector3 newPos = RandomNavMeshLocation(wanderRadius);
            agent.SetDestination(newPos);
            timer = 0;
        }
    }

    void UpdateAnimation()
    {
        if (anim == null) return;

        // Dùng vận tốc thực tế của Agent để điều khiển animation chạy/đi bộ
        float speed = agent.velocity.magnitude;
        anim.SetFloat("Speed", speed);

        // Nếu dừng lại do người chơi gần, ép về trạng thái Idle
        anim.SetBool("IsTalking", isPlayerNearby);
    }

    public Vector3 RandomNavMeshLocation(float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += transform.position;
        NavMeshHit hit;
        Vector3 finalPosition = Vector3.zero;

        // Tìm điểm gần nhất trên NavMesh để không đi xuyên tường
        if (NavMesh.SamplePosition(randomDirection, out hit, radius, 1))
        {
            finalPosition = hit.position;
        }
        return finalPosition;
    }
}