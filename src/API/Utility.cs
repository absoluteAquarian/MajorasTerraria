using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Exceptions;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace MajorasTerraria.API {
	internal static class Utility {
		public static TagCompound LoadWorldData<T>(string path, bool isCloudSave) where T : ModSystem {
			//A compressed version of WorldIO.Load
			path = Path.ChangeExtension(path, ".twld");

			if (!FileUtilities.Exists(path, isCloudSave))
				return null;

			byte[] buf = FileUtilities.ReadAllBytes(path, isCloudSave);

			if (buf[0] != 0x1F || buf[1] != 0x8B) {
				//LoadLegacy(buf);
				return null;
			}

			var tag = TagIO.FromStream(new MemoryStream(buf));

			foreach (var data in tag.GetList<TagCompound>("modData")) {
				if (ModContent.TryFind(data.GetString("mod"), data.GetString("name"), out ModSystem system) && system is T typedSystem) {
					try {
						return data.GetCompound("data");
					} catch (Exception e) {
						throw new CustomModDataException(system.Mod,
							"Error in reading custom world data for " + system.Mod.Name, e);
					}
				}
			}

			return null;
		}

		public static TagCompound LoadPlayerData<T>(string path, bool isCloudSave) where T : ModPlayer {
			//A compressed version of PlayerIO.Load
			path = Path.ChangeExtension(path, ".tplr");

			if (!FileUtilities.Exists(path, isCloudSave))
				return null;

			byte[] buf = FileUtilities.ReadAllBytes(path, isCloudSave);

			if (buf[0] != 0x1F || buf[1] != 0x8B) {
				//LoadLegacy(player, buf);
				return null;
			}

			var tag = TagIO.FromStream(new MemoryStream(buf));

			foreach (var data in tag.GetList<TagCompound>("modData")) {
				string modName = data.GetString("mod");
				string modPlayerName = data.GetString("name");

				if (ModContent.TryFind<ModPlayer>(modName, modPlayerName, out var modPlayerBase) && modPlayerBase is T) {
					try {
						return data.GetCompound("data");
					}
					catch (Exception e) {
						var mod = modPlayerBase.Mod;

						throw new CustomModDataException(mod,
							"Error in reading custom player data for " + mod.Name, e);
					}
				}
			}

			return null;
		}

		public static HSVA ToHSVA(this Color rgba) {
			double r = rgba.R / 255d;
			double g = rgba.G / 255d;
			double b = rgba.B / 255d;

			double ColorMax = Math.Max(Math.Max(r, g), b);
			double ColorMin = Math.Min(Math.Min(r, g), b);

			double delta = ColorMax - ColorMin;

			HSVA hsv = new();
			//Calculate H value
			if (delta == 0f)
				hsv.H = 0;
			else if (ColorMax == r)
				hsv.H = 60 * ((g - b) / delta % 6);
			else if (ColorMax == g)
				hsv.H = 60 * (((b - r) / delta) + 2);
			else if (ColorMax == b)
				hsv.H = 60 * (((r - g) / delta) + 4);

			//Possible if R > B > G
			if (hsv.H < 0)
				hsv.H += 360f;

			//In case we looped around, we need to truncate H within [0, 360)
			hsv.H %= 360f;

			//Calculate S value
			if (ColorMax == 0)
				hsv.S = 0;
			else
				hsv.S = delta / ColorMax;

			//Calculate V value
			hsv.V = ColorMax;

			hsv.A = rgba.A;

			return hsv;
		}

		public const int _4_30 = 4 * 3600 + 30 * 60;
		public const int _7_30 = 7 * 3600 + 30 * 60;
		public const int _12_00 = 12 * 3600;
		public const int _7_30PM_day = (int)Main.dayLength;
		public const int _4_30AM_night = (int)Main.nightLength;
		public const int _12AM = _4_30AM_night - _4_30;  //16,200
		public const int _12PM = _7_30PM_day - _7_30;    //27,000
		public const int FullDay = _4_30AM_night + _7_30PM_day;

		public static double CurrentTotalTime()
			=> Main.dayTime ? Main.time : Main.time + _7_30PM_day;

		public static void GetCurrentTime(out int hours, out int minutes, out double seconds) {
			double total = CurrentTotalTime();

			//Move time so that 12 AM is at tick time 0
			total += _4_30;
			
			if (total >= FullDay)
				total -= FullDay;

			hours = (int)(total / 3600);
			minutes = (int)(total / 60) % 60;
			seconds = total % 60;
		}

		public static void GetCurrentTerrariaTime(out int hours, out int minutes, out double seconds) {
			if (Main.dayTime) {
				hours = 4;
				minutes = 30;
			} else {
				hours = 19;
				minutes = 30;
			}

			hours += (int)(Main.time / 3600);
			hours %= 24;
			minutes += (int)(Main.time / 60) % 60;
			minutes %= 60;
			seconds = Main.time % 60;
		}

		public static double ToTicks(int hours = 0, int minutes = 0, double seconds = 0)
			=> hours * 3600 + minutes * 60 + seconds;
	}
}
