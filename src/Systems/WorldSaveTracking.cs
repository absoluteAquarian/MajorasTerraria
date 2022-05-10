using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MajorasTerraria.Systems {
	internal class WorldSaveTracking : ModSystem {
		public static bool resetPending;
		public static double lastKnownTime;
		public static bool lastKnownDaytime;

		public static bool MostRecentLoadWasValid { get; private set; }

		public override void SaveWorldData(TagCompound tag) {
			tag["terribleFate"] = true;
			tag["resetPending"] = resetPending;
			tag["lastKnownTime"] = lastKnownTime;
			tag["lastKnownDaytime"] = lastKnownDaytime;
		}

		public override void LoadWorldData(TagCompound tag) {
			MostRecentLoadWasValid = true;

			resetPending = tag.GetBool("resetPending");
			lastKnownTime = tag.GetDouble("lastKnownTime");
			lastKnownDaytime = tag.GetBool("lastKnownDaytime");

			//Prevent cheesing by leaving the world before the day ends or unloading mods
			if (Main.time != lastKnownTime || Main.dayTime != lastKnownDaytime) {
				Mod.Logger.Warn("Time cheesing detected.  Forcing a world reset.");
				MostRecentLoadWasValid = false;
				resetPending = true;
			}
			
			Main.time = lastKnownTime;
			Main.dayTime = lastKnownDaytime;
		}
	}
}
