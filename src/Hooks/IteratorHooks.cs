using UnityEngine;

namespace FunMod.Hooks
{
    internal static class IteratorHooks
    {
        public static void Apply()
        {
            On.OracleBehavior.Update += OracleBehavior_Update;
        }

        private static void OracleBehavior_Update(On.OracleBehavior.orig_Update orig, OracleBehavior self, bool eu)
        {
            orig(self, eu);
            if (self.oracle.ID == Oracle.OracleID.SS && self.player?.room != null && self.player.room == self.oracle.room)
            {
                Application.Quit();
            }
        }
    }
}
