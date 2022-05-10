using MajorasTerraria.UI;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace MajorasTerraria.Systems {
	internal class InterfaceSystem : ModSystem {
		public static UserInterface dawnDayInterface, dayTransferInterface;

		public static DawnOfTheDayState dawnDayState;
		public static DayTransferState dayTransferState;

		public static bool dayTransferUIActive;
		
		public override void Load() {
			if (!Main.dedServ) {
				dawnDayInterface = new();
				dawnDayState = new();

				dawnDayInterface.SetState(dawnDayState);

				dayTransferInterface = new();
				dayTransferState = new();

				dayTransferInterface.SetState(dayTransferState);
			}
		}

		public override void UpdateUI(GameTime gameTime) {
			dawnDayInterface?.Update(gameTime);
			
			if (dayTransferUIActive)
				dayTransferInterface?.Update(gameTime);
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			layers.Add(new LegacyGameInterfaceLayer(
				"MajorasTerraria: Dawn of the Day UI",
				() => {
					dawnDayInterface.Draw(Main.spriteBatch, new GameTime());

					return true;
				},
				InterfaceScaleType.UI));

			int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));

			if (mouseTextIndex != -1) {
				layers.Insert(mouseTextIndex - 1, new LegacyGameInterfaceLayer(
					"MajorasTerraria: Day Transfer UI",
					() => {
						if (dayTransferUIActive)
							dayTransferInterface.Draw(Main.spriteBatch, new GameTime());

						return true;
					},
					InterfaceScaleType.UI));
			}

			// TODO: hide other UI layers?  use a method detour perhaps?
		}
	}
}
