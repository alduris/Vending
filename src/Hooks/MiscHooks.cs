using System;
using System.Collections.Generic;
using MoreSlugcats;
using UnityEngine;

namespace FunMod.Hooks
{
    internal static class MiscHooks
    {
        public static void Apply()
        {
            // Explode on death
            On.Creature.Die += Creature_Die;

            // Moon's neurons kill you
            On.PhysicalObject.Collide += NeuronFlyKill;

            // Everything has the same relationships
            On.CreatureTemplate.ctor_Type_CreatureTemplate_List1_List1_Relationship += HungryWorld_DefaultRelationship;
            On.StaticWorld.EstablishRelationship += HungryWorld_EstablishRelationship;
        }

        private static void Creature_Die(On.Creature.orig_Die orig, Creature self)
        {
            bool wasAlive = self.State.alive;

            orig(self);

            if (wasAlive && self is not Fly) // batflies are exempt so you can actually eat them
            {
                Room room = self.room;
                Color color = self.ShortCutColor();
                Vector2 pos;
                for (int i = 0; i < self.bodyChunks.Length; i++)
                {
                    pos = self.bodyChunks[i].pos;
                    float strength = self.bodyChunks[i].mass;

                    room.AddObject(new Explosion(room, self, pos, 7, strength * 350f, strength * 8f, strength * 2.5f, strength * 400f, 0.25f, self, 0.7f, strength * 225f, 1f));
                    room.AddObject(new Explosion.ExplosionLight(pos, strength * 350f, 1f, 7, color));
                    room.AddObject(new Explosion.ExplosionLight(pos, strength * 300f, 1f, 3, Color.white));
                    room.AddObject(new ExplosionSpikes(room, pos, (int)Mathf.Sqrt(strength * 400f), 30f, 9f, strength * 10f, strength * 250f, color));
                    room.AddObject(new ShockWave(pos, strength * 475f, 0.045f, 5, false));
                }

                pos = self.mainBodyChunk.pos;
                room.ScreenMovement(pos, default, Math.Min(40f, self.TotalMass));
                room.PlaySound(SoundID.Bomb_Explode, pos);
                room.InGameNoise(new Noise.InGameNoise(pos, 9000f, self, 1f));
            }
        }

        private static void NeuronFlyKill(On.PhysicalObject.orig_Collide orig, PhysicalObject self, PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            orig(self, otherObject, myChunk, otherChunk);
            if (self is SLOracleSwarmer && otherObject is Player && (otherObject as Player).State.alive && !(otherObject as Player).isNPC)
            {
                (otherObject as Player).Die();
            }
        }

        private static void HungryWorld_DefaultRelationship(On.CreatureTemplate.orig_ctor_Type_CreatureTemplate_List1_List1_Relationship orig, CreatureTemplate self, CreatureTemplate.Type type, CreatureTemplate ancestor, List<TileTypeResistance> tileResistances, List<TileConnectionResistance> connectionResistances, CreatureTemplate.Relationship defaultRelationship)
        {
            orig(self, type, ancestor, tileResistances, connectionResistances, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 1f));
        }

        private static void HungryWorld_EstablishRelationship(On.StaticWorld.orig_EstablishRelationship orig, CreatureTemplate.Type a, CreatureTemplate.Type b, CreatureTemplate.Relationship relationship)
        {
            orig(a, b, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 1f));
        }
    }
}
