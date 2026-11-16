using CommandSystem;
using RemoteAdmin;
using System;

namespace ReBot.Navigation.Commands
{
    [CommandHandler(typeof(Nav))]
    internal class NavReinitCommand : ICommand
    {
        public string Command { get; } = "reinit";

        public string[] Aliases { get; } = [];

        public string Description { get; } = "Resets, then initializes navigation system.";

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

            NavigationSystem.Instance.ResetMeshes();
            NavigationSystem.Instance.InitializeMeshes();

            response = $"Navigation system reinitialized.";
            return true;
        }
    }
}
