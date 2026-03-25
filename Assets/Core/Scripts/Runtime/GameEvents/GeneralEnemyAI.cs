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

    [Header("Weapon Trail")]
    [Tooltip("Optional: child with WeaponTrailEffect on weapon blade. Auto-found if unset.")]
    [SerializeField] private WeaponTrailEffect weaponTrail;

    [Header("Attack Timing")]
    [Tooltip("Thời gian chờ từ lúc bắt đầu đánh tới lúc gây sát thương (khớp animation).")]
    [SerializeField] private float enemyHitDelay = 0.12f;

    private bool m_HitScheduled;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        hitReceiver = GetComponent<GeneralHitReceiver>(); // Lấy component nhận sát thương
        if (weaponTrail == null)
            weaponTrail = GetComponentInChildren<WeaponTrailEffect>(true);
        if (weaponTrail == null)
        {
            var go = new GameObject("WeaponTrail");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 1f, 0.55f);
            weaponTrail = go.AddComponent<WeaponTrailEffect>();
        }
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
            nextAttackTime = Time.time + attackRate;

            // Tránh tạo nhiều coroutine gây sát thương chồng lên nhau.
            if (!m_HitScheduled)
            {
                m_HitScheduled = true;
                if (weaponTrail != null)
                    weaponTrail.Play();
                StartCoroutine(EnemyHitRoutine());
            }
        }
    }

    private System.Collections.IEnumerator EnemyHitRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, enemyHitDelay));
        DealDamage();
        m_HitScheduled = false;
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

            Vector3 bloodPos = player.position + Vector3.up;
            Vector3 bloodDir = (player.position - transform.position).normalized;
            HitBloodVFX.Spawn(bloodPos, bloodDir);
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