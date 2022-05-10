using MajorasTerraria.API;
using MajorasTerraria.Config;
using MajorasTerraria.UI.Elements;
using Microsoft.Xna.Framework;
using SixLabors.ImageSharp.ColorSpaces;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader.Default;
using Terraria.UI;

namespace MajorasTerraria.UI {
	internal class DayTransferState : UIState {
		public struct CoinSlotInBank {
			public readonly int slot;
			public readonly int type;
			public readonly int stack;

			public CoinSlotInBank(int slot, int type, int stack) {
				this.slot = slot;
				this.type = type;
				this.stack = stack;
			}
		}
		
		public List<MTUIItemSlot> inventorySource = new();
		public List<MTUIItemSlot> itemsToTransfer = new();
		public List<MTUIItemSlot> toolsToTransfer = new();
		
		public List<CoinSlotInBank> coinsToTransfer = new();

		private UIList inventory;
		
		public override void OnInitialize() {
			UIText header = new("You've met with a terrible fate, haven't you?", large: true) {
				HAlign = 0.5f
			};
			header.Top.Set(100, 0f);
			Append(header);
			
			UIText info = new("Select the items you want to transfer.", textScale: 0.9f, large: true) {
				HAlign = 0.5f
			};
			info.Top.Set(header.Top.Pixels + DawnOfTheDayState.LargeUITextSize + 20, 0f);
			Append(info);

			inventory = new();
			inventory.Left.Set(50, 0f);
			inventory.Top.Set(info.Top.Pixels + DawnOfTheDayState.LargeUITextSize * 0.9f + 20, 0f);
			inventory.Width.Set(-100, 1f);
			inventory.Height.Set(500, 0f);
			Append(inventory);
			
			UIScrollbar inventoryScrollbar = new();
			inventoryScrollbar.Left.Set(0, 0.92f);
			inventoryScrollbar.Top.Set(0, 0.025f);
			inventoryScrollbar.Height.Set(0, 0.95f);
			inventory.SetScrollbar(inventoryScrollbar);
		}

		private static void RemoveAll(List<MTUIItemSlot> slots) {
			if (slots is null)
				return;

			foreach (MTUIItemSlot slot in slots)
				slot.Remove();

			slots.Clear();
		}

		public void SetupSlotsFromLocalPlayer() {
			Player player = Main.LocalPlayer;

			RemoveAll(inventorySource);
			RemoveAll(toolsToTransfer);
			coinsToTransfer.Clear();

			int slotOffset = TextureAssets.InventoryBack9.Value.Width + 10;

			//Relative positions
			float left = 0, top = 0;

			//Filter out any tools in the main inventory
			List<int> usedSlots = new();
			for (int i = 0; i < 50; i++) {
				Item item = player.inventory[i];

				if (item.pick > 0 || item.axe > 0 || item.hammer > 0) {
					MTUIItemSlot slot = new(scale: 0.9f) {
						//Player can only view the items
						IgnoreClicks = true
					};
					slot.SetItem(item);

					toolsToTransfer.Add(slot);

					Append(slot);

					player.inventory[i] = new Item();

					usedSlots.Add(i);
				}
			}

			//Main inventory
			SetupItemSlots(new Vector2(left, top), player.inventory, 0, slotColumns: 10, slotRows: 5, out _);

			//Restore the items in case the player exits the game
			for (int i = 0; i < usedSlots.Count; i++)
				player.inventory[usedSlots[i]] = toolsToTransfer[i].StoredItem.Clone();

			//Coins
			SetupItemSlots(new Vector2(left + slotOffset * 10 + 50, top), player.inventory, 50, 1, 4, out _);

			//Ammo
			SetupItemSlots(new Vector2(left + slotOffset * 11 + 50, top), player.inventory, 54, 1, 4, out top);

			//Mouse item
			SetupItemSlots(new Vector2(left + slotOffset + 10.5f + 50, top + 20), player.inventory, 58, 1, 1, out top);

			//Trash item
			Item[] oneSlotInventory = new Item[1] { player.trashItem };
			SetupItemSlots(new Vector2(left + slotOffset + 10.5f + 50, top), oneSlotInventory, 0, 1, 1, out top);

			//Equipment
			SetupItemSlots(new Vector2(left, top + 10), player.armor, 0, 10, 1, out top);

			//Modded equipment (max of 10 slots per row)
			Item[] moddedAccessories = ReflectionHelper<ModAccessorySlotPlayer>.InvokeGetterFunction("exAccessorySlot", player.GetModPlayer<ModAccessorySlotPlayer>()) as Item[];
			Item[] moddedEquipment = moddedAccessories.Where((i, s) => s % 2 == 0).ToArray();
			SetupItemSlots(new Vector2(left, top), moddedEquipment, 0, 10, int.MaxValue, out top);

			//Vanity
			SetupItemSlots(new Vector2(left, top + 30), player.armor, 10, 10, 1, out top);

			//Modded vanity (max of 10 slots per row)
			Item[] moddedVanity = moddedAccessories.Where((i, s) => s % 2 == 1).ToArray();
			SetupItemSlots(new Vector2(left, top), moddedVanity, 0, 10, int.MaxValue, out top);

			//Dyes
			SetupItemSlots(new Vector2(left, top + 30), player.dye, 0, 10, 1, out top);

			//Modded dyes (max of 10 slots per row)
			Item[] moddedDyes = ReflectionHelper<ModAccessorySlotPlayer>.InvokeGetterFunction("exDyesAccessory", player.GetModPlayer<ModAccessorySlotPlayer>()) as Item[];
			SetupItemSlots(new Vector2(left, top), moddedDyes, 0, 10, int.MaxValue, out top);

			//Misc equipment
			SetupItemSlots(new Vector2(left, top + 30), player.miscEquips, 0, 5, 1, out top);

			//Misc dyes
			SetupItemSlots(new Vector2(left, top + 10), player.miscDyes, 0, 5, 1, out top);

			//Coins in the piggy bank
			for (int i = 0; i < player.bank.item.Length; i++) {
				Item item = player.bank.item[i];
				if (item.type == ItemID.CopperCoin || item.type == ItemID.SilverCoin || item.type == ItemID.GoldCoin || item.type == ItemID.PlatinumCoin)
					coinsToTransfer.Add(new(i, item.type, item.stack));
			}

			foreach (MTUIItemSlot slot in inventorySource)
				inventory.Append(slot);

			SetupTransferSlots();

			float toolLeft = inventory.Left.Pixels, toolTop = itemsToTransfer[0].Top.Pixels + itemsToTransfer[0].Height.Pixels + 40;

			for (int i = 0; i < toolsToTransfer.Count; i++) {
				toolsToTransfer[i].Left.Set(toolLeft + slotOffset * i, 0f);
				toolsToTransfer[i].Top.Set(toolTop, 0f);
			}
		}

		private void SetupItemSlots(Vector2 initialPosition, Item[] inventory, int startInventorySlot, int slotColumns, int slotRows, out float top) {
			int i = startInventorySlot;

			int slotOffset = TextureAssets.InventoryBack9.Value.Width + 10;

			top = initialPosition.Y;
			
			for (int y = 0; y < slotRows; y++) {
				for (int x = 0; x < slotColumns; x++) {
					MTUIItemSlot slot = new() {
						Left = new(initialPosition.X + slotOffset * x, 0f),
						Top = new(initialPosition.Y + slotOffset * y, 0f)
					};

					slot.SetItem(inventory[i]);

					inventorySource.Add(slot);

					i++;

					if (i >= inventory.Length)
						break;
				}

				top += slotOffset;

				if (i >= inventory.Length)
					break;
			}
		}

		/// <remarks>Invoked by <see cref="SetupSlotsFromLocalPlayer"/></remarks>
		/// <exception cref="Exception"/>
		public void SetupTransferSlots() {
			RemoveAll(itemsToTransfer);

			int numSlots = 3;

			if (MajorasTerrariaConfig.Instance.ExpandTransferInventoryOnStoryProgressionBossDefeated) {
				if (NPC.downedBoss3)
					numSlots++;

				if (Main.hardMode)
					numSlots++;

				if (NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3)
					numSlots++;

				if (NPC.downedPlantBoss)
					numSlots++;

				if (NPC.downedGolemBoss)
					numSlots++;

				if (NPC.downedAncientCultist)
					numSlots++;
			}

			if (inventory is null)
				throw new Exception("UI elements for the player's inventory have not been initialized");

			float left = inventory.Left.Pixels + 120, top = inventory.Top.Pixels + inventory.Height.Pixels + 70;

			int slotOffset = TextureAssets.InventoryBack9.Value.Width + 10;

			for (int i = 0; i < numSlots; i++) {
				MTUIItemSlot slot = new() {
					Left = new(left + i * slotOffset, 0f),
					Top = new(top, 0f)
				};

				itemsToTransfer.Add(slot);
			}

			foreach (MTUIItemSlot slot in itemsToTransfer)
				Append(slot);
		}

		public static bool CanBeginWorldReset() {
			if (Main.netMode == NetmodeID.SinglePlayer)
				return true;
			
			// TODO: multiplayer support
			return true;
		}
	}
}
