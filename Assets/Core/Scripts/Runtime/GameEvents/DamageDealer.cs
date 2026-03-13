using UnityEngine;
using Blocks.Gameplay.Core;

public class GeneralDamageDealer : MonoBehaviour
{
  
    public float damageAmount = 10f;
    public float pushForceMagnitude = 5f;

    public void DealDamage(GameObject target)
    {

        var hittable = target.GetComponent<IHittable>();

        if (hittable != null)
        {
            Debug.Log("general đang tancong!");
           
            HitInfo info = new HitInfo
            {
                amount = damageAmount, 
                hitPoint = target.transform.position,
                hitNormal = (target.transform.position - transform.position).normalized,
                attackerId = 0, 
                impactForce = (target.transform.position - transform.position).normalized * pushForceMagnitude // Dùng Vector3
            };

            hittable.OnHit(info);
        }
    }
}