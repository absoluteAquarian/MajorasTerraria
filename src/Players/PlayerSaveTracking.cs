using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MajorasTerraria.Players {
	internal class PlayerSaveTracking : ModPlayer {
		public override void SaveData(TagCompound tag) {
			tag["terribleFate"] = true;
		}

		public override void LoadData(TagCompound tag) {
			
		}
	}
}
