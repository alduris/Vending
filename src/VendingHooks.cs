using System.Runtime.CompilerServices;
using RWCustom;
using UnityEngine;

namespace VendingMod
{
    internal static class VendingHooks
    {
        private const float VENDING_CHANCE = 0.8f;

        private static readonly ConditionalWeakTable<AbstractRoom, StrongBox<IntVector2>> vendPos = new();
        public static void Apply()
        {
            On.Room.Loaded += SpawnVending;
        }

        private static void SpawnVending(On.Room.orig_Loaded orig, Room self)
        {
            bool firstTime = self.abstractRoom.firstTimeRealized;
            orig(self);

            if (firstTime && !vendPos.TryGetValue(self.abstractRoom, out _) && !self.abstractRoom.shelter && !self.abstractRoom.gate && self.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.ZeroG) < 0.8f && Random.value < VENDING_CHANCE)
            {
                bool placed = false;
                for (int attempts = 0; attempts < 20; attempts++)
                {
                    var tile = self.RandomTile();
                    if (self.GetTile(tile).Solid) continue;

                    bool valid = false;
                    while (tile.y > 0)
                    {
                        if (!self.GetTile(tile.x, tile.y).Solid && !self.GetTile(tile.x + 1, tile.y).Solid && self.GetTile(tile.x, tile.y - 1).Solid && self.GetTile(tile.x + 1, tile.y - 1).Solid)
                        {
                            valid = true;
                            break;
                        }
                        tile = new IntVector2(tile.x, tile.y - 1);
                    }

                    if (valid)
                    {
                        // Check tiles above
                        for (int i = 0; i < 4; i++)
                        {
                            if (self.GetTile(tile.x, tile.y + i).Solid || self.GetTile(tile.x + 1, tile.y + i).Solid)
                            {
                                valid = false;
                                break;
                            }
                        }
                        if (valid)
                        {
                            // Ok good place to put the vending machine
                            vendPos.Add(self.abstractRoom, new(tile));
                            placed = true;
                            break;
                        }
                    }
                }

                if (!placed)
                {
                    vendPos.Add(self.abstractRoom, null);
                }
            }

            if (vendPos.TryGetValue(self.abstractRoom, out var posBox) && posBox is not null)
            {
                var pos = posBox.Value;
                self.AddObject(new Vending(self, self.MiddleOfTile(pos) + new Vector2(10f, 30f)));
            }
        }
    }
}
