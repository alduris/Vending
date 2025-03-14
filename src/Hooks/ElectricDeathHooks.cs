using System;
using MonoMod.RuntimeDetour;

namespace FunMod.Hooks
{
    internal static class ElectricDeathHooks
    {
        private const float MINOR_ELEC_DEATH_AMOUNT = 0.02f; // 50% is where it becomes lethal; don't set it to that

        public static void Apply()
        {
            On.Room.Loaded += Room_Loaded;
            new Hook(typeof(ElectricDeath).GetProperty(nameof(ElectricDeath.Intensity))!.GetGetMethod(), ElectricDeath_Intensity_get);
        }

        private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
        {
            orig(self);

            // Check if exists
            bool hasED = false;
            foreach (UpdatableAndDeletable obj in self.updateList)
            {
                if (obj is ElectricDeath)
                {
                    hasED = true;
                    (obj as ElectricDeath).effect.amount = 1f;
                    break;
                }
            }

            if (!hasED && !self.abstractRoom.shelter)
            {
                RoomSettings.RoomEffect effect = new(RoomSettings.RoomEffect.Type.ElectricDeath, 1f, false);
                self.AddObject(new ElectricDeath(effect, self));
            }
        }

        private static float ElectricDeath_Intensity_get(Func<ElectricDeath, float> orig, ElectricDeath self)
        {
            return Math.Max(MINOR_ELEC_DEATH_AMOUNT, orig(self));
        }
    }
}
