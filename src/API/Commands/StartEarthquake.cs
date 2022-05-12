using MajorasTerraria.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MajorasTerraria.API.Commands {
	internal class StartEarthquake : ModCommand {
		public override CommandType Type => CommandType.Chat;

		public override string Usage => "[c/ff6a00:Usage: /starteq]";

		public override string Command => "starteq";

		public override string Description => "Immediately starts an earthquake if the current day is in the Final Day.";

		public override void Action(CommandCaller caller, string input, string[] args) {
			if (Main.netMode != NetmodeID.SinglePlayer) {
				caller.Reply("This command can only be used in singleplayer.", Color.Red);
				return;
			}

			if (args.Length != 0) {
				caller.Reply("Expected no arguments", Color.Red);
				return;
			}
			
			FinalHoursEffects.tremorWait = 0;

			caller.Reply("Earthquake started.", Color.Green);
		}
	}
}
