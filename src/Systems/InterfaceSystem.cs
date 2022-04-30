using MajorasTerraria.UI;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace MajorasTerraria.Systems {
	internal class InterfaceSystem : ModSystem {
		public static UserInterface dawnDayInterface;

		public static DawnOfTheDayState dawnDayState;

		public override void Load() {
			if (!Main.dedServ) {
				dawnDayInterface = new();
				dawnDayState = new();

				dawnDayInterface.SetState(dawnDayState);
			}
		}

		public override void UpdateUI(GameTime gameTime) {
			dawnDayInterface?.Update(gameTime);
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			layers.Add(new LegacyGameInterfaceLayer(
				"MajorasTerraria: Dawn of the Day UI",
				() => {
					dawnDayInterface.Draw(Main.spriteBatch, new GameTime());

					return true;
				},
				InterfaceScaleType.UI));
		}
	}
}
