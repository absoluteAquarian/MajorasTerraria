using MajorasTerraria.Systems;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace MajorasTerraria.API.Commands {
	internal class ShowDay : ModCommand {
		public override CommandType Type => CommandType.Chat;

		public override string Usage => "[c/ff6a00:Usage: /showday <int>]";

		public override string Command => "showday";

		public override string Description => "Plays the \"Dawn of the Day\" animation for a given day";

		public override void Action(CommandCaller caller, string input, string[] args) {
			if (args.Length != 1) {
				caller.Reply("Expected only one integer argument", Color.Red);
				return;
			}

			if (!int.TryParse(args[0], out int day)) {
				caller.Reply("Expected an integer argument", Color.Red);
				return;
			}

			InterfaceSystem.dawnDayState.SetDay(day);

			caller.Reply($"Playing animation for day {day}", Color.Green);
		}
	}
}
