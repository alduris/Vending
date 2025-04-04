using System;
using System.Runtime.CompilerServices;
using RWCustom;
using UnityEngine;

namespace VendingMod
{
    internal class VendingControls : UpdatableAndDeletable
    {
        private static readonly ConditionalWeakTable<Player, VendingControls> controlCWT = new();
        public static bool GetControls(Player player, out VendingControls controls)
        {
            controls = null;
            if (player?.room != null && !player.isNPC && !player.dead)
            {
                controls = controlCWT.GetValue(player, (_) => new VendingControls(player, player.room));
                return true;
            }
            return false;
        }

        private readonly WeakReference<Player> playerRef;
        private int buttonTrack = 0;

        public bool DoTrade => buttonTrack == 40;

        public VendingControls(Player player, Room room)
        {
            this.playerRef = new(player);
            this.room = room;
            room.AddObject(this);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (!slatedForDeletetion && playerRef.TryGetTarget(out var player) && player.room == room)
            {
                var inp = player.input[0];
                bool good = !player.dead && !player.Stunned && inp.x == 0 && !inp.pckp && !inp.thrw && !inp.mp && !inp.jmp
                    && (player.bodyMode == Player.BodyModeIndex.Stand || player.bodyMode == Player.BodyModeIndex.ZeroG);
                if (good && inp.y == 1)
                {
                    buttonTrack++;
                }
                else
                {
                    buttonTrack = 0;
                }
            }
            else if (!slatedForDeletetion)
            {
                Destroy();
            }
        }

        public override void Destroy()
        {
            buttonTrack = 0;
            if (playerRef.TryGetTarget(out var player)) controlCWT.Remove(player);
            playerRef.SetTarget(null);
            base.Destroy();
        }
    }
}
