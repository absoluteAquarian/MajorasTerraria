using MajorasTerraria.API;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace MajorasTerraria.Systems {
	internal class FinalHoursEffects : ModSystem {
		internal static bool moonCrashCutscenePlaying;

		public static float tremorStrength;
		public static bool decreaseTremor;

		public static Color? watchTextOverrideColor;
		private static bool flashText;
		private static int flashTimer;

		public override void PreUpdatePlayers() {
			//This hook is the first one to be called
			if (WorldSaveTracking.resetPending && !moonCrashCutscenePlaying) {
				moonCrashCutscenePlaying = true;
				InitializeCutscene();
			}

			if (tremorStrength > 0 && decreaseTremor) {
				tremorStrength -= 2.75f / 60f;

				if (tremorStrength < 0)
					tremorStrength = 0;
			}

			//Final day?
			if (DayTracking.currentDay == 1) {
				Utility.GetCurrentTime(out int hours, out int minutes, out double seconds);

				double time = hours + minutes / 60d + seconds / 3600d;

				if (time >= 4.5f - 2 * Main.dayRate && time <= 4.5f) {
					//2 minutes left.  Warn the player!
					flashText = true;
				} else
					flashText = false;
			} else
				flashText = false;

			if (!flashText) {
				flashTimer = 0;
				watchTextOverrideColor = null;
			}

			if (flashText) {
				/*   Intended value function:
				 *       ___       ___
				 *     .'   '.   .'   '.
				 *    /       \ /       \
				 *   |         |         |
				 *   
				 *   Function used:  abs(sin(x))
				 *   Cycle frequency:  1 half-cycle per 0.75 seconds = 1.5 seconds per cycle = sin(timer / 60 * 2pi / 1.5)
				 */
				double frequency = 1 / 1.5;
				double sin = Math.Sin(flashTimer / 60d * 2d * Math.PI * frequency);
				double abs = Math.Abs(sin);

				watchTextOverrideColor = Color.Lerp(new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor), Color.Red, (float)abs);

				flashTimer++;
			}
		}

		private void InitializeCutscene() {
			
		}

		public override void ModifyScreenPosition() {
			if (tremorStrength > 0)
				Main.screenPosition += Main.rand.NextVector2Square(-tremorStrength, tremorStrength);
		}
	}
}
