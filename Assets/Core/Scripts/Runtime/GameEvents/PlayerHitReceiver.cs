using UnityEngine;
using Unity.Netcode;
using Blocks.Gameplay.Core;

namespace Blocks.Gameplay.Core
{
    public class PlayerHitReceiver : HitProcessor
    {
        private CoreStatsHandler statsHandler;
        private int healthStatHash;
        private Animator anim; // 1. Thêm biến Animator

        void Awake()
        {
            statsHandler = GetComponent<CoreStatsHandler>();
            // Tìm Animator ở chính nó hoặc con của nó
            anim = GetComponent<Animator>();
            if (anim == null) anim = GetComponentInChildren<Animator>();

            healthStatHash = Animator.StringToHash("Health");
        }

        protected override void HandleHit(HitInfo info)
        {
            if (statsHandler != null)
            {
                statsHandler.ModifyStat(
                    healthStatHash,
                    -info.amount,
                    info.attackerId,
                    ModificationSource.Direct
                );

                // 2. Kích hoạt animation trúng đòn tại đây
                if (anim != null)
                {
                    anim.SetTrigger("GetHit");
                }

                Debug.Log($"[Hit] Player bị trúng đòn, trừ {info.amount} máu và chạy anim GetHit.");
            }
        }
    }
}