using CommandSystem;
using LabApi.Features.Wrappers;
using RemoteAdmin;
using System;

namespace ReBot.Navigation.Commands
{
    [CommandHandler(typeof(Nav))]
    internal class NavVisualizeCommand : ICommand
    {
        public string Command { get; } = "visualize";

        public string[] Aliases { get; } = [];

        public string Description { get; } = "Toggles visalization of baked navigation mesh.";

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

            if (NavigationVisualizer.Player == null)
            {
                NavigationVisualizer.Enable(Player.Get(playerCommandSender));
            }
            else
            {
                NavigationVisualizer.Disable();
            }

            response = $"Nav visualization is now for {NavigationVisualizer.Player}.";
            return true;
        }
    }
}
