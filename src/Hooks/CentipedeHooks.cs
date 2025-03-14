using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace FunMod.Hooks
{
    internal static class CentipedeHooks
    {
        private const float CENTIPEDE_EXTEND_CHANCE = 0.99f;

        public static void Apply()
        {
            IL.Centipede.ctor += Centipede_ctor;
        }

        private static void Centipede_ctor(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext(MoveType.AfterLabel, x => x.MatchNewarr<BodyChunk>());

            // Emit our delegate that generates some random amount to add to the pre-set size
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((Centipede self) => {
                if (self.Small) return 0; // leave infant centipedes as small for convenience

                int i = 0;
                Random.State state = Random.state;
                Random.InitState(self.abstractCreature.ID.RandomSeed);
                while (Random.value < CENTIPEDE_EXTEND_CHANCE * self.size) // unlike old code, make it depend on size
                {
                    i++;
                }
                Random.state = state;
                return i;
            });
            c.Emit(OpCodes.Add);
        }
    }
}
