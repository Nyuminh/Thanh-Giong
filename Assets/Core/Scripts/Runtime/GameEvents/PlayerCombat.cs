using UnityEngine;
using UnityEngine.InputSystem;
using Blocks.Gameplay.Core;

public class PlayerCombat : MonoBehaviour
{
    public float attackRange = 2.5f;
    public float damageAmount = 20f;
    public LayerMask enemyLayer;
    public AudioSource attackAudioSource;
    public AudioClip attackSound;
    [Header("Combo Settings")]
    public int comboCount = 0;
    public float lastClickTime = 0f;
    public float comboDelay = 1f; // Thời gian tối đa giữa các cú nhấn để tính combo

    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // Kiểm tra Reset combo nếu để quá lâu không đánh
        if (Time.time - lastClickTime > comboDelay)
        {
            comboCount = 0;
            if (anim != null) anim.SetInteger("ComboCount", 0);
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Attack();
        }
    }

    void Attack()
    {
        lastClickTime = Time.time;
        comboCount++;
        Debug.Log($"Combo {comboCount}");
        // Reset về đòn 1 nếu đã đánh hết 3 đòn
        if (comboCount > 3) comboCount = 1;

        // Gửi dữ liệu sang Animator
        if (anim != null)
        {
            anim.SetInteger("ComboCount", comboCount);
            anim.SetTrigger("IsAttack");
        }

        // Tăng sát thương cho đòn thứ 3 (đòn kết liễu)
        float currentDamage = (comboCount == 3) ? damageAmount * 2f : damageAmount;

        // Quét gây sát thương
        Vector3 scanPosition = transform.position + transform.forward + Vector3.up;
        Collider[] hitEnemies = Physics.OverlapSphere(scanPosition, attackRange, enemyLayer);

        
        foreach (Collider enemy in hitEnemies)
        {
            var networkObj = enemy.GetComponent<Unity.Netcode.NetworkObject>();
            // Chỉ đánh nếu sư tử đã spawned và chưa chết
            if (networkObj != null && networkObj.IsSpawned)
            {
                var hittable = enemy.GetComponent<IHittable>();
                var lion = enemy.GetComponent<GeneralHitReceiver>();

                if (hittable != null && (lion == null || !lion.isDead))
                {
                    HitInfo info = new HitInfo { amount = currentDamage, attackerId = 0 };
                    hittable.OnHit(info);
                    if (attackAudioSource != null && attackSound != null)
                    {
                        attackAudioSource.PlayOneShot(attackSound);
                    }
                    Debug.Log($"Combo {comboCount} trúng đích! Sát thương: {currentDamage}");
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward + Vector3.up, attackRange);
    }
}