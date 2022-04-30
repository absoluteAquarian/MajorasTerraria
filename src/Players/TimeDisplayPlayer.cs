using Terraria.ModLoader;

namespace MajorasTerraria.Players {
	internal class TimeDisplayPlayer : ModPlayer {
		public override void PostUpdateEquips() {
			Player.accWatch = 3;  //Gold/Platinum watch
		}
	}
}
