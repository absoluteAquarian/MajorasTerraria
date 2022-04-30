using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MajorasTerraria.Systems {
	internal class WorldSaveTracking : ModSystem {
		public override void SaveWorldData(TagCompound tag) {
			tag["terribleFate"] = true;
		}

		public override void LoadWorldData(TagCompound tag) {
			
		}
	}
}
