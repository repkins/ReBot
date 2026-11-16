using CommandSystem;
using System;
using System.Linq;

namespace ReBot.Navigation.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    internal class Nav : ParentCommand
    {
        public override string Command { get; } = "nav";

        public override string[] Aliases { get; } = new string[] { };

        public override string Description { get; } = "Manipulates navigation mesh build system.";

        public override void LoadGeneratedCommands()
        {
            this.RegisterCommand(new NavVisualizeCommand());
            this.RegisterCommand(new NavReinitCommand());
            //this.RegisterCommand(new NavCell());
        }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = $"Please specify a valid subcommand. ({string.Join("/", Commands.Keys.ToArray())})";
            return false;
        }
    }
}
