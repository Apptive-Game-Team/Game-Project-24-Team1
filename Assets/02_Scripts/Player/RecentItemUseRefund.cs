using System.Collections.Generic;
using MushOut.UI;
using UnityEngine;

namespace MushOut.Player
{
    public static class RecentItemUseRefund
    {
        private const float DefaultRefundWindowSeconds = 3f;

        private enum ResourceType
        {
            SleepFungus,
            AggroFungus,
            BombFungus,
            MemorySpore
        }

        private struct UseRecord
        {
            public float Time;
            public ResourceType Type;
            public AbilityController AbilityController;
            public MemorySporeUI MemorySporeUI;
            public int Amount;
        }

        private static readonly List<UseRecord> Records = new List<UseRecord>();

        public static void RecordSleepFungusUse(AbilityController controller, int amount = 1)
        {
            RecordAbilityUse(ResourceType.SleepFungus, controller, amount);
        }

        public static void RecordAggroFungusUse(AbilityController controller, int amount = 1)
        {
            RecordAbilityUse(ResourceType.AggroFungus, controller, amount);
        }

        public static void RecordBombFungusUse(AbilityController controller, int amount = 1)
        {
            RecordAbilityUse(ResourceType.BombFungus, controller, amount);
        }

        public static void RecordMemorySporeUse(MemorySporeUI ui, int amount = 1)
        {
            if (ui == null || amount <= 0) return;

            Records.Add(new UseRecord
            {
                Time = Time.unscaledTime,
                Type = ResourceType.MemorySpore,
                MemorySporeUI = ui,
                Amount = amount
            });

            PruneExpired(Time.unscaledTime, DefaultRefundWindowSeconds);
        }

        public static void RefundRecentUses(float windowSeconds = DefaultRefundWindowSeconds)
        {
            float now = Time.unscaledTime;

            for (int i = Records.Count - 1; i >= 0; i--)
            {
                UseRecord record = Records[i];
                if (now - record.Time > windowSeconds)
                {
                    Records.RemoveAt(i);
                    continue;
                }

                Refund(record);
                Records.RemoveAt(i);
            }
        }

        private static void RecordAbilityUse(ResourceType type, AbilityController controller, int amount)
        {
            if (controller == null || amount <= 0) return;

            Records.Add(new UseRecord
            {
                Time = Time.unscaledTime,
                Type = type,
                AbilityController = controller,
                Amount = amount
            });

            PruneExpired(Time.unscaledTime, DefaultRefundWindowSeconds);
        }

        private static void Refund(UseRecord record)
        {
            for (int i = 0; i < record.Amount; i++)
            {
                switch (record.Type)
                {
                    case ResourceType.SleepFungus:
                        if (record.AbilityController != null) record.AbilityController.AddSleepFungus();
                        break;
                    case ResourceType.AggroFungus:
                        if (record.AbilityController != null) record.AbilityController.AddAggroFungus();
                        break;
                    case ResourceType.BombFungus:
                        if (record.AbilityController != null) record.AbilityController.AddBombFungus();
                        break;
                    case ResourceType.MemorySpore:
                        if (record.MemorySporeUI != null) record.MemorySporeUI.AddMemorySpores(1);
                        break;
                }
            }
        }

        private static void PruneExpired(float now, float windowSeconds)
        {
            for (int i = Records.Count - 1; i >= 0; i--)
            {
                if (now - Records[i].Time > windowSeconds)
                {
                    Records.RemoveAt(i);
                }
            }
        }
    }
}
