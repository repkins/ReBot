using CommandSystem;
using LabApi.Features.Wrappers;
using RemoteAdmin;
using System;
using UnityEngine.AI;

namespace ReBot.Navigation.Commands
{
    [CommandHandler(typeof(Nav))]
    internal class NavTraceCommand : ICommand
    {
        public string Command { get; } = "trace";

        public string[] Aliases { get; } = [];

        public string Description { get; } = "Visualizes corners path.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission(PlayerPermissions.FacilityManagement, out response))
            {
                return false;
            }

            if (sender is not PlayerCommandSender playerCommandSender)
            {
                response = "You must be in-game to use this command!";
                return false;
            }

            var player = Player.Get(playerCommandSender)!;

            if (NavigationVisualizer.CachedPosition.HasValue)
            {
                var sourcePosition = NavigationVisualizer.CachedPosition.Value;

                NavigationVisualizer.CachedPosition = null;

                if (!NavMesh.CalculatePath(sourcePosition, player.Position, NavMesh.AllAreas, NavigationVisualizer.TracePath))
                {
                    response = $"Could not find path. Source position cleared.";
                    return false;
                }

                response = $"Path found. Source position cleared.";
                return true;
            }

            NavigationVisualizer.CachedPosition = player.Position;

            response = $"Source position set.";
            return true;
        }
    }
}
