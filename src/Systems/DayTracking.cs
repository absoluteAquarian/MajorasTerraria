using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MajorasTerraria.Systems {
	internal class DayTracking : ModSystem {
		public static int currentDay = 3;

		public static List<NPCType> masksObtained;

		public static bool MoonLordDefeated;

		public override void OnWorldLoad() {
			masksObtained = new();
		}

		public override void LoadWorldData(TagCompound tag) {
			currentDay = tag.GetInt("day");

			if (currentDay < 1 || currentDay > 3)
				throw new IOException("Invalid day: " + currentDay);

			if (tag.GetList<TagCompound>("masks") is var list)
				masksObtained = list.Select(NPCType.Load).ToList();

			MoonLordDefeated = tag.GetBool("moonlord");
		}

		public override void SaveWorldData(TagCompound tag) {
			tag["day"] = currentDay;
			tag["masks"] = masksObtained?.Select(n => n.Save()).ToList();
			tag["moonlord"] = MoonLordDefeated;
		}

		public override void PreUpdateEntities() {
			if (MoonLordDefeated)
				currentDay = 3;
		}

		public struct NPCType {
			public readonly string mod;
			public readonly string id;

			public NPCType(string mod, string id) {
				this.mod = mod;
				this.id = id;
			}

			public NPCType(int id) {
				if (id < NPCID.Count) {
					mod = "Terraria";
					this.id = id.ToString();
				} else {
					ModNPC npc = ModContent.GetModNPC(id);
					mod = npc.Mod.Name;
					this.id = npc.Name;
				}
			}

			public bool TryGetID(out int id) {
				if (mod == "Terraria") {
					id = (int)uint.Parse(this.id);
					return true;
				} else if (ModLoader.TryGetMod(mod, out Mod instance) && instance.TryFind(this.id, out ModNPC npcInstance)) {
					id = npcInstance.Type;
					return true;
				}

				id = -1;
				return false;
			}

			public TagCompound Save() {
				return new() {
					["mod"] = mod,
					["id"] = id
				};
			}

			public static NPCType Load(TagCompound tag) {
				return new(tag.GetString("mod"), tag.GetString("id"));
			}

			public override int GetHashCode()
				=> TryGetID(out int id) ? id : -1;
		}
	}
}
