<<<<<<< HEAD
using UnityEngine;
=======
﻿using UnityEngine;
>>>>>>> b2ebe84852fd14c307f08712b560c049bdd58ce9
using UnityEngine.AI;

public class VillagerAI : MonoBehaviour
{
    [Header("Movement Settings")]
<<<<<<< HEAD
    public float wanderRadius = 10f;     // B�n k�nh �i d?o
    public float idleTime = 3f;          // Th?i gian �?ng ngh?

    [Header("Detection Settings")]
    public float detectionRange = 5f;    // Kho?ng c�ch nh?n bi?t ng�?i ch�i
    public Transform player;             // Tham chi?u t?i ng�?i ch�i
=======
    public float wanderRadius = 10f;     // Bán kính đi dạo
    public float idleTime = 3f;          // Thời gian đứng nghỉ

    [Header("Detection Settings")]
    public float detectionRange = 5f;    // Khoảng cách nhận biết người chơi
    public Transform player;             // Tham chiếu tới người chơi
>>>>>>> b2ebe84852fd14c307f08712b560c049bdd58ce9

    private NavMeshAgent agent;
    private Animator anim;
    private float timer;
    private bool isPlayerNearby = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
<<<<<<< HEAD
        // Th? t?m ? ch�nh n�, n?u kh�ng c� th? t?m ? c�c object con
=======
        // Thử tìm ở chính nó, nếu không có thì tìm ở các object con
>>>>>>> b2ebe84852fd14c307f08712b560c049bdd58ce9
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        timer = idleTime;
    }
<<<<<<< HEAD
    // T�n h�m ph?i vi?t CH�NH X�C t?ng ch? c�i nh� trong th�ng b�o l?i
    public void OnFootstepWalk()
    {
        // ��y l� n�i b?n ph�t �m thanh b�?c ch�n ho?c t?o hi?u ?ng b?i
        // Debug.Log("Nh�n v?t �ang ch?y!"); 
=======
    // Tên hàm phải viết CHÍNH XÁC từng chữ cái như trong thông báo lỗi
    public void OnFootstepWalk()
    {
        // Đây là nơi bạn phát âm thanh bước chân hoặc tạo hiệu ứng bụi
        // Debug.Log("Nhân vật đang chạy!"); 
>>>>>>> b2ebe84852fd14c307f08712b560c049bdd58ce9
    }
    void Update()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
<<<<<<< HEAD
            return; // D?ng Update frame n�y �? �?i frame sau c� player r?i m?i ch?y logic ti?p
=======
            return; // Dừng Update frame này để đợi frame sau có player rồi mới chạy logic tiếp
>>>>>>> b2ebe84852fd14c307f08712b560c049bdd58ce9
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
<<<<<<< HEAD
        // D?ng NavMeshAgent
        agent.isStopped = true;

        // Xoay m?t v? ph�a ng�?i ch�i m?t c�ch m�?t m�
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Gi? th�ng b?ng kh�ng cho AI ng?a �?u l�n tr?i
=======
        // Dừng NavMeshAgent
        agent.isStopped = true;

        // Xoay mặt về phía người chơi một cách mượt mà
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Giữ thăng bằng không cho AI ngửa đầu lên trời
>>>>>>> b2ebe84852fd14c307f08712b560c049bdd58ce9

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

<<<<<<< HEAD
        // N?u �? ngh? �? th?i gian, t?m �i?m �i m?i
=======
        // Nếu đã nghỉ đủ thời gian, tìm điểm đi mới
>>>>>>> b2ebe84852fd14c307f08712b560c049bdd58ce9
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

<<<<<<< HEAD
        // D�ng v?n t?c th?c t? c?a Agent �? �i?u khi?n animation ch?y/�i b?
        float speed = agent.velocity.magnitude;
        anim.SetFloat("Speed", speed);

        // N?u d?ng l?i do ng�?i ch�i g?n, �p v? tr?ng th�i Idle
=======
        // Dùng vận tốc thực tế của Agent để điều khiển animation chạy/đi bộ
        float speed = agent.velocity.magnitude;
        anim.SetFloat("Speed", speed);

        // Nếu dừng lại do người chơi gần, ép về trạng thái Idle
>>>>>>> b2ebe84852fd14c307f08712b560c049bdd58ce9
        anim.SetBool("IsTalking", isPlayerNearby);
    }

    public Vector3 RandomNavMeshLocation(float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += transform.position;
        NavMeshHit hit;
        Vector3 finalPosition = Vector3.zero;

<<<<<<< HEAD
        // T?m �i?m g?n nh?t tr�n NavMesh �? kh�ng �i xuy�n t�?ng
=======
        // Tìm điểm gần nhất trên NavMesh để không đi xuyên tường
>>>>>>> b2ebe84852fd14c307f08712b560c049bdd58ce9
        if (NavMesh.SamplePosition(randomDirection, out hit, radius, 1))
        {
            finalPosition = hit.position;
        }
        return finalPosition;
    }
}