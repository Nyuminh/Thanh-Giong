using UnityEngine;
using Unity.Netcode;
using Blocks.Gameplay.Core;

namespace Blocks.Gameplay.Core
{
    public class PlayerHitReceiver : HitProcessor
    {
        private CoreStatsHandler statsHandler;
        private int healthStatHash;

        void Awake()
        {
            statsHandler = GetComponent<CoreStatsHandler>();
            // Lấy Hash của tên "Health" để làm việc với hệ thống Stats
            healthStatHash = Animator.StringToHash("Health");
        }

        protected override void HandleHit(HitInfo info)
        {
            // Trong template này, HitProcessor đã lo việc gửi tin lên Server/Owner rồi.
            // Khi hàm này chạy, chúng ta chỉ cần gọi ModifyStat.
            if (statsHandler != null)
            {
                // Sử dụng hàm ModifyStat có sẵn trong CoreStatsHandler bạn vừa gửi
                // Tham số: (mã stat, lượng thay đổi, ID người đánh, loại thay đổi)
                statsHandler.ModifyStat(
                    healthStatHash,
                    -info.amount,
                    info.attackerId,
                    ModificationSource.Direct
                );

                Debug.Log($"[Hit] Đã trừ {info.amount} máu của Player.");
            }
        }
    }
}