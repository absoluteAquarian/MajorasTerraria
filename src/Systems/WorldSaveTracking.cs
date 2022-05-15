using Terraria;
using Terraria.IO;
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
			tag["lastKnownTime"] = Main.time;
			tag["lastKnownDaytime"] = Main.dayTime;
		}

		public override void LoadWorldData(TagCompound tag) {
			MostRecentLoadWasValid = true;

			resetPending = tag.GetBool("resetPending");
			lastKnownTime = tag.GetDouble("lastKnownTime");
			lastKnownDaytime = tag.GetBool("lastKnownDaytime");

			//Prevent cheesing by leaving the world before the day ends or unloading mods
			if (resetPending || Main.time != lastKnownTime || Main.dayTime != lastKnownDaytime) {
				Mod.Logger.Warn("Time cheesing detected.  Forcing a world reset.");
				MostRecentLoadWasValid = false;
				resetPending = true;
			}
			
			Main.time = lastKnownTime;
			Main.dayTime = lastKnownDaytime;
		}

#if !TML_2022_04
		public override bool CanWorldBePlayed(PlayerFileData playerData, WorldFileData worldFileData)
			=> CoreMod.CheckPlayerFileData(playerData) && CoreMod.CheckWorldFileData(worldFileData);

		public override string WorldCanBePlayedRejectionMessage(PlayerFileData playerData, WorldFileData worldData) {
			if (!CoreMod.CheckPlayerFileData(playerData))
				return "Only players created while Terrible Fate was enabled can be used with the mod.";
			else if (!CoreMod.CheckWorldFileData(worldData))
				return "Only worlds created while Terrible Fate was enabled can be used with the mod.";
			return "";
		}
#endif
	}
}
