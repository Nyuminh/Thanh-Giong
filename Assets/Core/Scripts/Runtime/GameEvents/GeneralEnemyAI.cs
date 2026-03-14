using Blocks.Gameplay.Core;
using UnityEngine;
using UnityEngine.AI;

public class GeneralEnemyAI : MonoBehaviour
{
    private Transform player;
    public float alertRange = 10f;
    public float attackRange = 2f;
    public GameObject finishZone;
    [Header("Combat Settings")]
    public float damageAmount = 15f;
    public float attackRate = 1.5f;
    private float nextAttackTime = 0f;
    [Header("Music Settings")]
    public AudioSource combatMusic;
    private NavMeshAgent agent;
    private Animator anim;
    private GeneralHitReceiver hitReceiver; // Thêm biến để tham chiếu trạng thái sống/chết
    public AudioSource enemyAudioSource; // Kéo AudioSource của tướng địch vào đây
    public AudioClip attackSound;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        hitReceiver = GetComponent<GeneralHitReceiver>(); // Lấy component nhận sát thương
    }

    void Update()
    {
        // BƯỚC QUAN TRỌNG: Nếu sư tử đã chết, dừng mọi logic và thoát hàm
        if (hitReceiver != null && hitReceiver.isDead)
        {

            if (agent.enabled) agent.isStopped = true; // Dừng di chuyển hẳn
            if (combatMusic != null)
            {
                combatMusic.Stop();
                // Hoặc dùng combatMusic.Pause(); nếu bạn muốn nhạc dừng tạm thời
            }                // Kích hoạt vùng hoàn thành
            if (finishZone != null)
            {
                finishZone.SetActive(true);
                Debug.Log("Tướng địch đã chết! Cổng về đích đã xuất hiện.");
            }
            return;
        }

        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            AttackState();
        }
        else if (distanceToPlayer <= alertRange)
        {
            FollowState();
        }
        else
        {
            IdleState();
        }
    }

    void AttackState()
    {
        // Kiểm tra lại lần nữa để chắc chắn không vồ khi đang diễn anim chết
        if (hitReceiver != null && hitReceiver.isDead) return;

        agent.isStopped = true;
        anim.SetBool("isWalking", false);
        anim.SetBool("isAttacking", true);
        LookAtPlayer();

        if (Time.time >= nextAttackTime)
        {
            DealDamage();
            nextAttackTime = Time.time + attackRate;
        }
    }

    void DealDamage()
    {
        var hittable = player.GetComponent<IHittable>();
        if (hittable != null)
        {
            Debug.Log("<color=red>general đã đánh trúng Player!</color>");
            if (enemyAudioSource != null && attackSound != null)
            {
                enemyAudioSource.PlayOneShot(attackSound);
            }
            HitInfo info = new HitInfo
            {
                amount = damageAmount,
                hitPoint = player.position,
                hitNormal = Vector3.up,
                attackerId = 999,
                impactForce = transform.forward * 5f
            };
            hittable.OnHit(info);
        }
    }

    void FollowState()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
        anim.SetBool("isWalking", true);
        anim.SetBool("isAttacking", false);
    }

    void IdleState()
    {
        agent.isStopped = true;
        anim.SetBool("isWalking", false);
        anim.SetBool("isAttacking", false);
    }

    void LookAtPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, alertRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}