using MajorasTerraria.API;
using MajorasTerraria.API.Edits;
using MajorasTerraria.Config;
using MajorasTerraria.IO;
using MajorasTerraria.Players;
using MajorasTerraria.Systems;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Reflection;
using System.Threading;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace MajorasTerraria {
	public class CoreMod : Mod {
		internal static event Action UnloadReflection;

		public static CoreMod Instance => ModContent.GetInstance<CoreMod>();

		public static bool[] MaskNPCs;
		public static bool[] MaskItems;

		public override void Load() {
			ILHelper.LogILEdits = true;

			On.Terraria.GameContent.UI.Elements.UICharacterListItem.ctor += Hook_UICharacterListItem_ctor;
			On.Terraria.GameContent.UI.Elements.UICharacterListItem.PlayGame += Hook_UICharacterListItem_PlayGame;
			On.Terraria.GameContent.UI.Elements.UICharacterListItem.PlayMouseOver += Hook_UICharacterListItem_PlayMouseOver;
			On.Terraria.GameContent.UI.Elements.UICharacterListItem.MouseOver += Hook_UICharacterListItem_MouseOver;
			On.Terraria.GameContent.UI.Elements.UICharacterListItem.MouseOut += Hook_UICharacterListItem_MouseOut;

			//GamerMod is dumb and stupid and completely removes the check for invalid gamemodes
			if (ModLoader.HasMod("GamerMod")) {
				Logger.Debug("GamerMod was loaded.  Applying alternative IL edit instead of method detour.");
				IL.Terraria.GameContent.UI.Elements.UIWorldListItem.PlayGame += Patch_UIWorldListItem_PlayGame;
			} else
				On.Terraria.GameContent.UI.Elements.UIWorldListItem.TryMovingToRejectionMenuIfNeeded += Hook_UIWorldListItem_TryMovingToRejectionMenuIfNeeded;

			On.Terraria.GameContent.UI.Elements.UIWorldListItem.ctor += Hook_UIWorldListItem_ctor;

			On.Terraria.Main.UpdateTime_StartDay += Hook_Main_UpdateTime_StartDay;

			On.Terraria.IO.WorldFile.ResetTempsToDayTime += Hook_WorldFile_ResetTempsToDayTime;

			IL.Terraria.WorldGen.do_worldGenCallBack += Patch_WorldGen_do_worldGenCallBack;

			On.Terraria.Main.CanPauseGame += Hook_Main_CanPauseGame;
			On.Terraria.Main.ShouldUpdateEntities += Hook_Main_ShouldUpdateEntities;
			IL.Terraria.Main.DrawInfoAccs += Patch_Main_DrawInfoAccs;
		}

		private void Patch_Main_DrawInfoAccs(ILContext il) {
			FieldInfo Main_mouseTextColor = typeof(Main).GetField("mouseTextColor", BindingFlags.Static | BindingFlags.Public);

			ILHelper.EnsureAreNotNull((Main_mouseTextColor, typeof(Main).FullName + "::mouseTextColor"));

			ILCursor c = new(il);
			
			int patchNum = 1;

			ILHelper.CompleteLog(Instance, c, beforeEdit: true);

			if (!c.TryGotoNext(MoveType.After, i => i.MatchLdsfld(Main_mouseTextColor),
				i => i.MatchLdsfld(Main_mouseTextColor),
				i => i.MatchLdsfld(Main_mouseTextColor),
				i => i.MatchLdsfld(Main_mouseTextColor)))
				goto bad_il;

			//Move after the Color.ctor() call
			c.Index++;

			c.Emit(OpCodes.Ldloc, 11);
			c.Emit(OpCodes.Ldloc, 64);
			c.EmitDelegate<Func<InfoDisplay, Color, Color>>((info, black) => {
				if (info == InfoDisplay.Watches && FinalHoursEffects.watchTextOverrideColor is Color color)
					return color;

				return black;
			});
			c.Emit(OpCodes.Stloc, 64);

			ILHelper.UpdateInstructionOffsets(c);

			ILHelper.CompleteLog(Instance, c, beforeEdit: false);

			return;
			bad_il:
			throw new Exception("Unable to fully patch " + il.Method.Name + "()\n" +
				"Reason: Could not find instruction sequence for patch #" + patchNum);			
		}

		private bool Hook_Main_ShouldUpdateEntities(On.Terraria.Main.orig_ShouldUpdateEntities orig, Main self)
			=> !InterfaceSystem.dayTransferUIActive && orig(self);

		private bool Hook_Main_CanPauseGame(On.Terraria.Main.orig_CanPauseGame orig)
			=> !FinalHoursEffects.moonCrashCutscenePlaying && orig();

		private void Patch_WorldGen_do_worldGenCallBack(ILContext il) {
			MethodInfo WorldFile_SaveWorld = typeof(WorldFile).GetMethod("SaveWorld", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(bool), typeof(bool) });

			ILHelper.EnsureAreNotNull((WorldFile_SaveWorld, typeof(WorldFile).FullName + "::SaveWorld(bool, bool)"));
			
			ILCursor c = new(il);
			
			int patchNum = 1;

			ILHelper.CompleteLog(Instance, c, beforeEdit: true);

			if (!c.TryGotoNext(MoveType.After, i => i.MatchCall(WorldFile_SaveWorld)))
				goto bad_il;

			patchNum++;

			c.EmitDelegate(() => WorldData.Save(Main.ActiveWorldFileData.Path, Main.ActiveWorldFileData.IsCloudSave));
			
			ILHelper.UpdateInstructionOffsets(c);

			ILHelper.CompleteLog(Instance, c, beforeEdit: false);

			return;
			bad_il:
			throw new Exception("Unable to fully patch " + il.Method.Name + "()\n" +
				"Reason: Could not find instruction sequence for patch #" + patchNum);
		}

		private void Hook_WorldFile_ResetTempsToDayTime(On.Terraria.IO.WorldFile.orig_ResetTempsToDayTime orig) {
			orig();

			ReflectionHelper<WorldFile>.InvokeSetterStaticFunction("_tempTime", 0d);
		}

		private void Hook_Main_UpdateTime_StartDay(On.Terraria.Main.orig_UpdateTime_StartDay orig, ref bool stopEvents) {
			orig(ref stopEvents);

			DayTracking.displayedDay = false;
			DayTracking.currentDay--;

			if (DayTracking.currentDay < 1) {
				DayTracking.currentDay = 3;

				// TODO: transition toward UI state for selecting items once the 3 days have run out
			}
		}

		public override void PostSetupContent() {
			MaskNPCs = new SetFactory(NPCLoader.NPCCount).CreateBoolSet(false,
				NPCID.KingSlime,
				NPCID.EyeofCthulhu,
				NPCID.EaterofWorldsHead,
				NPCID.BrainofCthulhu,
				NPCID.QueenBee,
				NPCID.SkeletronHead,
				NPCID.Deerclops,
				NPCID.WallofFlesh,
				NPCID.QueenSlimeBoss,
				NPCID.TheDestroyer,
				NPCID.Spazmatism,
				NPCID.Retinazer,
				NPCID.SkeletronPrime,
				NPCID.Plantera,
				NPCID.Golem,
				NPCID.HallowBoss,
				NPCID.DukeFishron,
				NPCID.CultistBoss,
				NPCID.MoonLordCore);

			MaskItems = new SetFactory(ItemLoader.ItemCount).CreateBoolSet(false,
				ItemID.KingSlimeMask,
				ItemID.EyeMask,
				ItemID.EaterMask,
				ItemID.BrainMask,
				ItemID.BeeMask,
				ItemID.SkeletronMask,
				ItemID.DeerclopsMask,
				ItemID.FleshMask,
				ItemID.QueenSlimeMask,
				ItemID.DestroyerMask,
				ItemID.TwinMask,
				ItemID.SkeletronPrimeMask,
				ItemID.PlanteraMask,
				ItemID.GolemMask,
				ItemID.FairyQueenMask,
				ItemID.DukeFishronMask,
				ItemID.BossMaskCultist,
				ItemID.BossMaskMoonlord);
		}

		private void Hook_UICharacterListItem_ctor(On.Terraria.GameContent.UI.Elements.UICharacterListItem.orig_ctor orig, UICharacterListItem self, PlayerFileData data, int snapPointIndex) {
			orig(self, data, snapPointIndex);

			if (!CheckPlayerData(self)) {
				self.BorderColor = new Color(127, 127, 127) * 0.7f;
				self.BackgroundColor = Color.Lerp(new Color(63, 82, 151), new Color(80, 80, 80), 0.5f) * 0.7f;
			}
		}

		private void Hook_UIWorldListItem_ctor(On.Terraria.GameContent.UI.Elements.UIWorldListItem.orig_ctor orig, UIWorldListItem self, WorldFileData data, int orderInList, bool canBePlayed) {
			orig(self, data, orderInList, canBePlayed);

			if (CheckWorldData(self, setMenuMode: false)) {
				ReflectionHelper<UIWorldListItem>.InvokeSetterFunction("_canBePlayed", self, false);

				self.BorderColor = new Color(127, 127, 127) * 0.7f;
				self.BackgroundColor = Color.Lerp(new Color(63, 82, 151), new Color(80, 80, 80), 0.5f) * 0.7f;
			}
		}

		private void Hook_UICharacterListItem_MouseOut(On.Terraria.GameContent.UI.Elements.UICharacterListItem.orig_MouseOut orig, UICharacterListItem self, UIMouseEvent evt) {
			orig(self, evt);

			if (!CheckPlayerData(self)) {
				self.BorderColor = new Color(127, 127, 127) * 0.7f;
				self.BackgroundColor = Color.Lerp(new Color(63, 82, 151), new Color(80, 80, 80), 0.5f) * 0.7f;
			}
		}

		private void Hook_UICharacterListItem_MouseOver(On.Terraria.GameContent.UI.Elements.UICharacterListItem.orig_MouseOver orig, UICharacterListItem self, UIMouseEvent evt) {
			orig(self, evt);
			
			if (!CheckPlayerData(self)) {
				self.BackgroundColor = new Color(150, 150, 150);
				self.BorderColor = Color.Lerp(self.BackgroundColor, new Color(120, 120, 120), 0.5f);

				UICharacter _playerPanel = ReflectionHelper<UICharacterListItem>.InvokeGetterFunction("_playerPanel", self) as UICharacter;
				_playerPanel.SetAnimated(animated: false);
			}
		}

		private void Hook_UICharacterListItem_PlayMouseOver(On.Terraria.GameContent.UI.Elements.UICharacterListItem.orig_PlayMouseOver orig, UICharacterListItem self, UIMouseEvent evt, UIElement listeningElement) {
			if (CheckPlayerData(self))
				orig(self, evt, listeningElement);
			else {
				UIText _buttonLabel = ReflectionHelper<UICharacterListItem>.InvokeGetterFunction("_buttonLabel", self) as UIText;
				_buttonLabel.SetText("Cannot play.  Character was not created by Terrible Fate.");
			}
		}

		private static void Patch_UIWorldListItem_PlayGame(ILContext il) {
			ILCursor c = new(il);
			
			int patchNum = 1;

			ILHelper.CompleteLog(Instance, c, beforeEdit: true);

			ILLabel branch = null;
			if (!c.TryGotoNext(MoveType.Before, i => i.MatchBneUn(out branch),
				i => i.MatchLdarg(0)))
				goto bad_il;

			patchNum++;

			//Move to before "ldarg.0"
			c.Index++;

			//Insert an additonal check in the first if statement:
			//  if (listeningElement == evt.Target && _data.IsValid && !TryMovingToRejectionMenuIfNeeded(_data.GameMode)) {
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldc_I4_1);
			c.EmitDelegate(CheckWorldData);
			c.Emit(OpCodes.Brtrue, branch);

			ILHelper.UpdateInstructionOffsets(c);

			ILHelper.CompleteLog(Instance, c, beforeEdit: false);

			return;
			bad_il:
			throw new Exception("Unable to fully patch " + il.Method.Name + "()\n" +
				"Reason: Could not find instruction sequence for patch #" + patchNum);
		}

		private static void Hook_UICharacterListItem_PlayGame(On.Terraria.GameContent.UI.Elements.UICharacterListItem.orig_PlayGame orig, UICharacterListItem self, UIMouseEvent evt, UIElement listeningElement) {
			if (CheckPlayerData(self))
				orig(self, evt, listeningElement);
		}

		private static bool CheckPlayerData(UICharacterListItem self) {
			if (MajorasTerrariaConfig.Instance.AllowExistingSaves)
				return true;

			PlayerFileData _data = ReflectionHelper<UICharacterListItem>.InvokeGetterFunction("_data", self) as PlayerFileData;

			return Utility.LoadPlayerData<PlayerSaveTracking>(_data.Path, _data.IsCloudSave) is TagCompound tag && tag.GetBool("terribleFate");
		}

		private static bool Hook_UIWorldListItem_TryMovingToRejectionMenuIfNeeded(On.Terraria.GameContent.UI.Elements.UIWorldListItem.orig_TryMovingToRejectionMenuIfNeeded orig, UIWorldListItem self, int worldGameMode) {
			if (orig(self, worldGameMode)) {
				//Existing checks failed.  Don't try to do MajorasTerraria checks since that would be unnecessary
				return true;
			}
			
			return CheckWorldData(self);
		}

		public static bool CheckWorldData(UIWorldListItem self, bool setMenuMode = true) {
			if (MajorasTerrariaConfig.Instance.AllowExistingSaves)
				return false;

			WorldFileData _data = ReflectionHelper<UIWorldListItem>.InvokeGetterFunction("_data", self) as WorldFileData;
			
			if (_data.IsValid && Utility.LoadWorldData<WorldSaveTracking>(_data.Path, _data.IsCloudSave) is TagCompound tag && tag.GetBool("terribleFate"))
				return false;

			if (setMenuMode) {
				Main.statusText = "Only worlds created while Terrible Fate was enabled can be used with the mod.";
				Main.menuMode = MenuID.RejectedWorld;
			}
			return true;
		}

		public override void Unload() {
			Interlocked.Exchange(ref UnloadReflection, null)?.Invoke();
		}
	}
}