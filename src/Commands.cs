using DevConsole;
using DevConsole.Commands;
using UnityEngine;

namespace VendingMod
{
    internal static class Commands
    {
        public static void Register()
        {
            new CommandBuilder("list_machines")
                .Help("list_machines")
                .RunGame(RunListMachines)
                .Register();
        }

        private static void RunListMachines(RainWorldGame game, string[] args)
        {
            if (game.world?.name != null)
            {
                GameConsole.WriteLine("Probable vending machine rooms in region:");
                Random.State old = Random.state;
                foreach (var room in game.world.abstractRooms)
                {
                    if (room.shelter || room.gate) continue;
                    Random.InitState(room.name.GetHashCode());
                    if (Random.value < VendingHooks.VENDING_CHANCE) GameConsole.WriteLine(room.name);
                }
                Random.state = old;
            }
            else
            {
                GameConsole.WriteLine("Must be in a region!", Color.red);
            }
        }
    }
}
