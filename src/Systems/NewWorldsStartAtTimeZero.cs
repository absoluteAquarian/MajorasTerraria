using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace MajorasTerraria.Systems {
	internal class NewWorldsStartAtTimeZero : ModSystem {
		public override void ModifyWorldGenTasks(List<GenPass> tasks, ref float totalWeight) {
			int index = tasks.FindIndex(genPass => genPass.Name == "Reset");

			if (index >= 0) {
				tasks.Insert(index + 1, new PassLegacy("MajorasTerraria:ZeroTime", (progress, configuration) => {
					progress.Message = "Modifying Time";
					Main.time = 0;
					Main.dayTime = true;
				}));
			}
		}
	}
}
