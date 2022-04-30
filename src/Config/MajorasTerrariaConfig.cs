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
		[ReloadRequired]
		public bool AllowExistingSaves;
	}
}
