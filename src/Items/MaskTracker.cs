using MajorasTerraria.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace MajorasTerraria.Items {
	internal class MaskTracker : GlobalItem {
		public override bool AppliesToEntity(Item entity, bool lateInstantiation) {
			return lateInstantiation && (CoreMod.MaskItems?[entity.type] ?? false);
		}

		public override void OnSpawn(Item item, IEntitySource source) {
			if (source is EntitySource_Loot loot && loot.Entity is NPC npc) {
				int npcType = npc.type;

				if (npcType == NPCID.EaterofWorldsBody || npcType == NPCID.EaterofWorldsTail)
					npcType = NPCID.EaterofWorldsHead;
				else if (npcType == NPCID.TheDestroyerBody || npcType == NPCID.TheDestroyerTail)
					npcType = NPCID.TheDestroyer;
				else if (npcType == NPCID.Retinazer)
					npcType = NPCID.Spazmatism;

				var key = new DayTracking.NPCType(npcType);

				if (CoreMod.MaskNPCs[npcType] && !DayTracking.masksObtained.Contains(key)) {
					DayTracking.masksObtained.Add(key);

					if (npc.type == NPCID.Retinazer)
						DayTracking.masksObtained.Add(new(NPCID.Retinazer));

					if (npc.type == NPCID.Spazmatism || npc.type == NPCID.Retinazer) {
						Main.NewText(Lang.GetNPCNameValue(NPCID.Spazmatism) + " is now permanently defeated!", Color.Red);
						Main.NewText(Lang.GetNPCNameValue(NPCID.Retinazer) + " is now permanently defeated!", Color.Red);
					} else
						Main.NewText(Lang.GetNPCNameValue(npcType) + " is now permanently defeated!", Color.Red);
				}
			}
		}
	}
}
