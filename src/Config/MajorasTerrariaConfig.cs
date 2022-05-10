using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace MajorasTerraria.Config {
	internal class MajorasTerrariaConfig : ModConfig {
		public static MajorasTerrariaConfig Instance => ModContent.GetInstance<MajorasTerrariaConfig>();

		public override ConfigScope Mode => ConfigScope.ServerSide;

		[Label("Allow Existing Saves")]
		[Tooltip("Whether or not existing saves can be used with the mod.")]
		[DefaultValue(false)]
		public bool AllowExistingSaves;

		[Label("Gain Slots on Boss Defeat")]
		[Tooltip("Whether or not the player is allowed to transfer more items to a world reset after defeating key bosses in progression.\n" +
			"Bosses considered key bosses: Skeletron, Wall of Flesh, the Mechanical Trio, Plantera, Golem, Lunatic Cultist")]
		[DefaultValue(true)]
		public bool ExpandTransferInventoryOnStoryProgressionBossDefeated;
	}
}
