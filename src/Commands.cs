using System.Collections.Generic;
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
                List<string> rooms = [];
                foreach (var room in game.world.abstractRooms)
                {
                    if (room.shelter || room.gate || room.offScreenDen) continue;
                    Random.InitState(room.name.GetHashCode());
                    if (Random.value < VendingHooks.VENDING_CHANCE) rooms.Add(room.name);
                }
                rooms.Sort();
                foreach (var room in rooms)
                {
                    GameConsole.WriteLine(room);
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
