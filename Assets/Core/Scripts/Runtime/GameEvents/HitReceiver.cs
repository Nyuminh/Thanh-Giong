using UnityEngine;
using UnityEngine.AI;
using Blocks.Gameplay.Core;

public class GeneralHitReceiver : HitProcessor
{
    public float health = 500f; // Boss máu trâu hơn sư tử
    public bool isDead = false;
    private Animator anim;
    private NavMeshAgent agent;

    void Awake()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }

    // Ghi đè lại phương thức xử lý hit từ HitProcessor
    protected override void HandleHit(HitInfo info)
    {
        if (isDead) return;

        health -= info.amount;
        Debug.Log($"Tướng giặc bị chém! Máu còn: {health}");

        if (health <= 0)
        {
            Die();
        }
        else
        {
            if (anim != null) anim.SetTrigger("IsAttacked");
        }
    }

    void Die()
    {
        if (isDead) return; // Tránh chạy lệnh Die nhiều lần

        isDead = true;

        if (anim != null)
        {
            // Chuyển sang trigger hoặc bool tùy theo Animator của tướng giặc
            anim.SetBool("Die", true);
        }

        if (agent != null) agent.isStopped = true;

        // Vô hiệu hóa Collider để không bị chém trúng thêm lần nào nữa
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Debug.Log("<color=red>Tướng giặc đã gục ngã!</color>");
    }
}