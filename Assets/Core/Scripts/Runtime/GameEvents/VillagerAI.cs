using UnityEngine;
using UnityEngine.AI;

public class VillagerAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float wanderRadius = 10f;     // Bán kính ði d?o
    public float idleTime = 3f;          // Th?i gian ð?ng ngh?

    [Header("Detection Settings")]
    public float detectionRange = 5f;    // Kho?ng cách nh?n bi?t ngý?i chõi
    public Transform player;             // Tham chi?u t?i ngý?i chõi

    private NavMeshAgent agent;
    private Animator anim;
    private float timer;
    private bool isPlayerNearby = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        // Th? t?m ? chính nó, n?u không có th? t?m ? các object con
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        timer = idleTime;
    }
    // Tên hàm ph?i vi?t CHÍNH XÁC t?ng ch? cái nhý trong thông báo l?i
    public void OnFootstepWalk()
    {
        // Ðây là nõi b?n phát âm thanh bý?c chân ho?c t?o hi?u ?ng b?i
        // Debug.Log("Nhân v?t ðang ch?y!"); 
    }
    void Update()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            return; // D?ng Update frame này ð? ð?i frame sau có player r?i m?i ch?y logic ti?p
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
        // D?ng NavMeshAgent
        agent.isStopped = true;

        // Xoay m?t v? phía ngý?i chõi m?t cách mý?t mà
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Gi? thãng b?ng không cho AI ng?a ð?u lên tr?i

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

        // N?u ð? ngh? ð? th?i gian, t?m ði?m ði m?i
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

        // Dùng v?n t?c th?c t? c?a Agent ð? ði?u khi?n animation ch?y/ði b?
        float speed = agent.velocity.magnitude;
        anim.SetFloat("Speed", speed);

        // N?u d?ng l?i do ngý?i chõi g?n, ép v? tr?ng thái Idle
        anim.SetBool("IsTalking", isPlayerNearby);
    }

    public Vector3 RandomNavMeshLocation(float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += transform.position;
        NavMeshHit hit;
        Vector3 finalPosition = Vector3.zero;

        // T?m ði?m g?n nh?t trên NavMesh ð? không ði xuyên tý?ng
        if (NavMesh.SamplePosition(randomDirection, out hit, radius, 1))
        {
            finalPosition = hit.position;
        }
        return finalPosition;
    }
}