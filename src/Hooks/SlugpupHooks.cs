using System;
using MoreSlugcats;
using UnityEngine;

namespace FunMod.Hooks
{
    internal static class SlugpupHooks
    {
        private const float SLUP_SPAWN_CHANCE = 0.001f; // chance a slup spawns when exiting a shortcut
        private const float SLUP_EXPLODE_CHANCE = 0.1f; // chance to explode before multiplied by food like/dislike

        public static void Apply()
        {
            // Slugpup shenanigans :monksilly:
            On.Player.ObjectEaten += Player_ObjectEaten;
            On.Player.SpitOutOfShortCut += Player_SpitOutOfShortCut;
        }

        private static void Player_ObjectEaten(On.Player.orig_ObjectEaten orig, Player self, IPlayerEdible edible)
        {
            orig(self, edible);

            // Get how much slup likes or dislikes the food
            if (ModManager.MSC)
            {
                var ai = self.AI;
                var foodType = ai.GetFoodType(edible as PhysicalObject);
                float foodWeight = (foodType != SlugNPCAI.Food.NotCounted && foodType.Index > -1) ? Mathf.Abs(ai.foodPreference[foodType.Index]) : 0f;

                // Decide if the slugpup explodes >:3
                if (Random.value < foodWeight * SLUP_EXPLODE_CHANCE)
                {
                    // Adapted from Player.Die for slugpups in inv campaign (basically spawns a singularity bomb that instantly explodes)
                    var bomb = new AbstractPhysicalObject(
                        self.abstractCreature.Room.world,
                        MoreSlugcatsEnums.AbstractObjectType.SingularityBomb,
                        null,
                        self.room.GetWorldCoordinate(self.mainBodyChunk.pos),
                        self.abstractCreature.Room.world.game.GetNewID());
                    self.abstractCreature.Room.AddEntity(bomb);
                    bomb.RealizeInRoom();
                    (bomb.realizedObject as SingularityBomb).Explode();
                }
            }
        }

        private static void Player_SpitOutOfShortCut(On.Player.orig_SpitOutOfShortCut orig, Player self, RWCustom.IntVector2 pos, Room newRoom, bool spitOutAllSticks)
        {
            orig(self, pos, newRoom, spitOutAllSticks);
            if (ModManager.MSC && Random.value < SLUP_SPAWN_CHANCE)
            {
                // Create slugpup and spit out of shortcut with player
                var slup = new AbstractCreature(newRoom.world, StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC), null, self.abstractCreature.pos, newRoom.game.GetNewID());
                var state = (slup.state as PlayerNPCState);
                state.foodInStomach = 1;
                newRoom.abstractRoom.AddEntity(slup);
                slup.RealizeInRoom();

                // Make the slugpup like the player (so it will follow)
                var rel = state.socialMemory.GetOrInitiateRelationship(self.abstractCreature.ID);
                rel.InfluenceLike(1f);
                rel.InfluenceTempLike(1f);
                var slupAI = (slup.abstractAI as SlugNPCAbstractAI);
                slupAI.isTamed = true;
                slupAI.RealAI.friendTracker.friend = self;
                slupAI.RealAI.friendTracker.friendRel = rel;

                // Create shockwave
                newRoom.AddObject(new ShockWave(new Vector2(self.mainBodyChunk.pos.x, self.mainBodyChunk.pos.y), 300f, 0.2f, 15, false));
            }
        }
    }
}
