using System.Collections;
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

    [Header("Weapon Trail")]
    [Tooltip("Optional: child with WeaponTrailEffect (weapon tip). Auto-found if unset.")]
    [SerializeField] private WeaponTrailEffect weaponTrail;

    [Header("Combo Settings")]
    public int comboCount = 0;
    public float lastClickTime = 0f;
    public float comboDelay = 1f; // Thời gian tối đa giữa các cú nhấn để tính combo

    [Header("Attack Timing")]
    [Tooltip("Thời gian chờ từ lúc bấm chuột tới lúc quét trúng mục tiêu (để khớp animation vung).")]
    [SerializeField] private float hitDelay = 0.18f;

    [Tooltip("Khóa việc bấm chuột trong thời gian này để tránh 'click nhanh hơn vung'.")]
    [SerializeField] private float attackLockDuration = 0.55f;

    private Animator anim;
    private bool m_AttackLocked;
    private Coroutine m_AttackRoutine;

    void Start()
    {
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (weaponTrail == null)
            weaponTrail = GetComponentInChildren<WeaponTrailEffect>(true);
        if (weaponTrail == null)
        {
            var go = new GameObject("WeaponTrail");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 1f, 0.65f);
            weaponTrail = go.AddComponent<WeaponTrailEffect>();
        }
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
        if (m_AttackLocked) return;

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

        if (weaponTrail != null)
            weaponTrail.Play();

        // Tăng sát thương cho đòn thứ 3 (đòn kết liễu)
        float currentDamage = (comboCount == 3) ? damageAmount * 2f : damageAmount;

        // Tách timing sát thương để khớp với animation vung.
        if (m_AttackRoutine != null)
            StopCoroutine(m_AttackRoutine);

        m_AttackLocked = true;
        m_AttackRoutine = StartCoroutine(AttackRoutine(currentDamage, comboCount));
    }

    private IEnumerator AttackRoutine(float currentDamage, int comboSnapshot)
    {
        // Chờ tới lúc vũ khí "ra tới" mới quét trúng.
        yield return new WaitForSeconds(Mathf.Max(0f, hitDelay));

        Vector3 scanPosition = transform.position + transform.forward + Vector3.up;
        Collider[] hitEnemies = Physics.OverlapSphere(scanPosition, attackRange, enemyLayer);

        foreach (Collider enemy in hitEnemies)
        {
            var networkObj = enemy.GetComponent<Unity.Netcode.NetworkObject>();
            if (networkObj != null && networkObj.IsSpawned)
            {
                var hittable = enemy.GetComponent<IHittable>();
                var lion = enemy.GetComponent<GeneralHitReceiver>();

                if (hittable != null && (lion == null || !lion.isDead))
                {
                    HitInfo info = new HitInfo { amount = currentDamage, attackerId = 0 };
                    hittable.OnHit(info);

                    Vector3 hitPoint = enemy.ClosestPoint(scanPosition);
                    Vector3 hitNormal = (hitPoint - transform.position).normalized;
                    HitBloodVFX.Spawn(hitPoint, hitNormal);
                }
            }
        }

        if (hitEnemies != null && hitEnemies.Length > 0 && attackAudioSource != null && attackSound != null)
        {
            attackAudioSource.PlayOneShot(attackSound);
        }

        Debug.Log($"Combo {comboSnapshot} hit (after delay). Damage: {currentDamage}");

        // Khóa đủ lâu để người chơi không click nhanh hơn nhịp vung.
        yield return new WaitForSeconds(Mathf.Max(0f, attackLockDuration - hitDelay));

        m_AttackLocked = false;
        m_AttackRoutine = null;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward + Vector3.up, attackRange);
    }
}