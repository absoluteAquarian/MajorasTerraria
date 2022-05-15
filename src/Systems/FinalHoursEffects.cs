using MajorasTerraria.API;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Capture;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MajorasTerraria.Systems {
	internal class FinalHoursEffects : ModSystem {
		private static bool requestMoonCrashCutscene;
		internal static bool moonCrashCutscenePlaying;

		public static float tremorStrength, tremorModification;
		internal static int tremorTimer, tremorWait, tremorSign;

		public static Color? watchTextOverrideColor;
		private static bool flashText;
		private static int flashTimer;

		public override void SaveWorldData(TagCompound tag) {
			tag[nameof(moonCrashCutscenePlaying)] = moonCrashCutscenePlaying;
			tag[nameof(tremorStrength)] = tremorStrength;
			tag[nameof(tremorModification)] = tremorModification;
			tag[nameof(tremorTimer)] = tremorTimer;
			tag[nameof(tremorWait)] = tremorWait;
			tag[nameof(tremorSign)] = tremorSign;
			tag[nameof(flashText)] = flashText;
			tag[nameof(flashTimer)] = flashTimer;
		}

		public override void LoadWorldData(TagCompound tag) {
			moonCrashCutscenePlaying = tag.GetBool(nameof(moonCrashCutscenePlaying));
			tremorStrength = tag.GetFloat(nameof(tremorStrength));
			tremorModification = tag.GetFloat(nameof(tremorModification));
			tremorTimer = tag.GetInt(nameof(tremorTimer));
			tremorWait = tag.GetInt(nameof(tremorWait));
			tremorSign = tag.GetInt(nameof(tremorSign));
			flashText = tag.GetBool(nameof(flashText));
			flashTimer = tag.GetInt(nameof(flashTimer));
		}

		public override void PreUpdatePlayers() {
			//This hook is the first one to be called
			if (requestMoonCrashCutscene && !moonCrashCutscenePlaying) {
				requestMoonCrashCutscene = false;
				moonCrashCutscenePlaying = true;
				InitializeCutscene();
			}

			if (tremorStrength > 0 && tremorModification != 0) {
				tremorStrength += tremorModification;

				if (tremorStrength < 0)
					tremorStrength = 0;
			}

			if (tremorStrength == 0)
				tremorTimer = 0;
			else
				tremorTimer++;

			if (tremorWait > 0)
				tremorWait--;

			//Final day?
			if (DayTracking.currentDay == 1 && !Main.dayTime) {
				double time = Main.nightLength - Main.time;

				if (time < Utility.ToTicks(hours: 2) * Main.dayRate) {
					//2 minutes of IRL time left.  Warn the player!
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
				const double frequency = 1 / 1.5;
				double sin = Math.Sin(flashTimer / 60d * 2d * Math.PI * frequency);
				double abs = Math.Abs(sin);

				watchTextOverrideColor = Color.Lerp(new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor), Color.Red, (float)abs);

				flashTimer++;
			}
		}

		public static void RequestFinalCutscene() => requestMoonCrashCutscene = true;

		private void InitializeCutscene() {
			// TODO: add an IL edit for Main.DrawInterface for drawing the final hours overlays
		}

		public override void ModifyScreenPosition() {
			if (tremorStrength > 0) {
				if (tremorSign == 0)
					tremorSign = 1;
				
				Main.screenPosition.Y += tremorStrength * tremorSign;

				if (tremorTimer % 3 == 0)
					tremorSign = -tremorSign;
			}
		}

		public override void PostUpdateTime() {
			if (!(DayTracking.currentDay == 1 && !moonCrashCutscenePlaying)) {
				tremorWait = (int)(3600 * Main.rand.NextFloat(0.85f, 1.15f));
				return;
			}

			//Play the bell sounds at increasing intervals
			LegacySoundStyle sound = SoundLoader.GetLegacySoundSlot(Mod, "Assets/Sounds/Custom/ClockTowerChime")
				.WithPitchVariance(0.02f)
				.WithVolume(Math.Max(0.1f, Main.musicVolume * 0.78f));

			Utility.GetCurrentTerrariaTime(out _, out int minutes, out double seconds);
				
			if (Main.dayTime) {
				//Daytime; play every hour

				if (minutes == 0 && seconds < Main.dayRate)
					SoundEngine.PlaySound(sound);
			} else {
				if (Main.time < Utility.ToTicks(hours: 2)) {
					//First 2 hours of the night; play every 30 minutes
					if (minutes % 30 == 0 && seconds < Main.dayRate)
						SoundEngine.PlaySound(sound);
				} else if (Main.time >= Utility.ToTicks(hours: 2) && Main.time < Utility.ToTicks(hours: 4)) {
					//Next 2 hours of the night; play every 15 minutes
					if (minutes % 15 == 0 && seconds < Main.dayRate)
						SoundEngine.PlaySound(sound);
				} else if (seconds % 30 < Main.dayRate && Main.time >= Utility.ToTicks(hours: 4) && Main.time < Utility.ToTicks(hours: 5, minutes: 30)) {
					//Next 1.5 hours of the night; play every 7.5 minutes
					if (minutes % 7 == 0 && seconds < Main.dayRate)
						SoundEngine.PlaySound(sound);
				} else if (Main.time >= Utility.ToTicks(hours: 5, minutes: 30) && Main.time < Utility.ToTicks(hours: 7)) {
					//Next 1.5 hours of the night; play every 5 minutes
					if (minutes % 5 == 0 && seconds < Main.dayRate)
						SoundEngine.PlaySound(sound);
				} else if (Main.time >= Utility.ToTicks(hours: 7)) {
					//Final 2 hours of the night; play every 2 minutes
					if (minutes % 2 == 0 && seconds < Main.dayRate)
						SoundEngine.PlaySound(sound);
				}
			}

			//Randomly start an earthquake tremor
			if (tremorWait <= 0) {
				double total = Utility.CurrentTotalTime();
				double factor = total / Utility.FullDay;

				int time = (int)(Math.Max(8 * 60, 3600 * factor) * Main.rand.NextFloat(0.85f, 1.15f));

				factor *= factor * factor;

				tremorStrength = MathHelper.Lerp(0f, 5f, (float)factor) * Main.rand.NextFloat(0.7f, 1.4f);
				tremorModification = -0.65f / 60f * Main.rand.NextFloat(0.5f, 1.2f);

				SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(Mod, "Assets/Sounds/Custom/Earthquake").WithVolume(0.95f));

				tremorWait = time;
			}
		}
	}

	public class FinalHoursMusic : ModSceneEffect {
		public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/FinalHours");

		public override SceneEffectPriority Priority => SceneEffectPriority.Environment;

		public override float GetWeight(Player player)
			=> 1f;

		public override bool IsSceneEffectActive(Player player) {
			Utility.GetCurrentTime(out int hours, out int minutes, out _);
			
			return DayTracking.currentDay == 1 && !Main.dayTime && hours >= 0 && hours + minutes / 60f < 4.5f;
		}
	}

	public class TerribleFateMusic : ModSceneEffect {
		//Complete and utter silence
		public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/TerribleFate");

		public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

		public override float GetWeight(Player player)
			=> 1f;

		public override bool IsSceneEffectActive(Player player)
			=> InterfaceSystem.dayTransferUIActive;
	}
}
