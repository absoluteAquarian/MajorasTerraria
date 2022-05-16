using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MajorasTerraria.Players {
	internal class PlayerSaveTracking : ModPlayer {
		//Used to keep track of if the player needs to evaluate their inventory in the world transfer UI
		public bool needsWorldTransfer;

		public override void SaveData(TagCompound tag) {
			tag["terribleFate"] = true;
			tag["needsWorldTransfer"] = needsWorldTransfer;
		}

		public override void LoadData(TagCompound tag) {
			needsWorldTransfer = tag.GetBool("needsWorldTransfer");
		}

		public override void OnEnterWorld(Player player) {
			var playerData = Main.ActivePlayerFileData;

			CoreMod.FileEntry<PlayerFileData>.cache[new(playerData.Path, playerData.IsCloudSave)] = null;
			
			if (Main.netMode == NetmodeID.SinglePlayer) {
				var worldData = Main.ActiveWorldFileData;

				CoreMod.FileEntry<WorldFileData>.cache[new(worldData.Path, worldData.IsCloudSave)] = null;
			}
		}
	}
}
