using System.Collections.Generic;
using RWCustom;
using UnityEngine;

namespace FunMod.Hooks
{
    internal static class VultureGrubHooks
    {
        private const float GRUB_FUN_CHANCE = 0.6f;

        public static void Apply()
        {
            On.VultureGrub.RayTraceSky += VultureGrub_RayTraceSky;
            On.VultureGrub.AttemptCallVulture += VultureGrub_AttemptCallVulture;
        }

        private static bool VultureGrub_RayTraceSky(On.VultureGrub.orig_RayTraceSky orig, VultureGrub self, Vector2 testDir)
        {
            // Just the original code without the attraction test
            if (self.room.abstractRoom.skyExits < 1)
            {
                return false;
            }
            Vector2 corner = Custom.RectCollision(self.bodyChunks[1].pos, self.bodyChunks[1].pos + testDir * 100000f, self.room.RoomRect).GetCorner(FloatRect.CornerLabel.D);
            if (SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(self.room, self.bodyChunks[1].pos, corner) != null)
            {
                return false;
            }
            if (corner.y >= self.room.PixelHeight - 5f)
            {
                self.skyPosition = new IntVector2?(self.room.GetTilePosition(corner));
                return true;
            }
            return false;
        }

        private static void VultureGrub_AttemptCallVulture(On.VultureGrub.orig_AttemptCallVulture orig, VultureGrub self)
        {
            // See if we're not going to have fun today
            if (Random.value > GRUB_FUN_CHANCE) return;

            // Check if there's a sky entrance
            int closestDist = int.MaxValue;
            int closestSky = -1;
            for (int k = 0; k < self.room.borderExits.Length; k++)
            {
                if (!(self.room.borderExits[k].type == AbstractRoomNode.Type.SkyExit))
                {
                    continue;
                }
                for (int l = 0; l < self.room.borderExits[k].borderTiles.Length; l++)
                {
                    if (Custom.ManhattanDistance(self.room.borderExits[k].borderTiles[l], self.skyPosition.Value) < closestDist)
                    {
                        closestDist = Custom.ManhattanDistance(self.room.borderExits[k].borderTiles[l], self.skyPosition.Value);
                        closestSky = k + self.room.exitAndDenIndex.Length;
                    }
                }
            }
            if (closestSky < 0) return;

            // Check if we can spawn a creature
            var list = new List<IntVector2>();
            SharedPhysics.RayTracedTilesArray(self.bodyChunks[1].pos, self.room.MiddleOfTile(self.skyPosition.Value), list);
            IntVector2? spawnPos = null;
            for (int m = 0; m < list.Count; m++)
            {
                if (self.room.aimap.TileAccessibleToCreature(list[m], StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Vulture)))
                {
                    spawnPos = list[m];
                    break;
                }
            }
            if (spawnPos.HasValue)
            {
                // Spawn the creature(s)
                var amount = Mathf.FloorToInt(Mathf.Pow(Random.value, 1.75f) * 6 + 1);
                for (int i = 0; i < amount; i++)
                {
                    var randCrit = new CreatureTemplate.Type(CreatureTemplate.Type.values.entries[CreatureTemplate.Type.values.entries.Count]);
                    var spawned = new AbstractCreature(self.room.world, StaticWorld.GetCreatureTemplate(randCrit), null, new WorldCoordinate(self.room.abstractRoom.index, spawnPos.Value.x, spawnPos.Value.y, closestSky), self.room.game.GetNewID());
                    self.room.abstractRoom.AddEntity(spawned);
                    spawned.RealizeInRoom();
                }

                // Other stuff
                self.callingMode = 1;
                if (self.graphicsModule != null)
                {
                    (self.graphicsModule as VultureGrubGraphics).blinking = 220;
                }
                self.vultureCalled = true;
            }

        }
    }
}
