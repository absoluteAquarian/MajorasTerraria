using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.UI;

namespace MajorasTerraria.UI.Elements {
	internal class MTUIItemSlot : UIElement {
		private int Context { get; set; }

		public float Scale { get; private set; }

		public virtual Item StoredItem => storedItem;

		protected Item storedItem;

		private Item storedItemBeforeHandle;

		public bool ItemChanged{
			get{
				var item = storedItem;
				return item != null && storedItemBeforeHandle != null && item.IsNotSameTypePrefixAndStack(storedItemBeforeHandle);
			}
		}
		public bool ItemTypeChanged => (storedItem?.type ?? -1) != (storedItemBeforeHandle?.type ?? -2);

		public Func<Item, bool> ValidItemFunc;

		public Action<Item> OnItemChanged;

		public bool IgnoreClicks{ get; set; }

		public MTUIItemSlot(int context = ItemSlot.Context.BankItem, float scale = 1f) {
			Context = context;
			Scale = scale;

			storedItem = new Item();
			storedItem.SetDefaults();

			Width.Set(TextureAssets.InventoryBack9.Value.Width * scale, 0f);
			Height.Set(TextureAssets.InventoryBack9.Value.Height * scale, 0f);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			float oldScale = Main.inventoryScale;
			Main.inventoryScale = Scale;
			Rectangle rectangle = GetDimensions().ToRectangle();

			if ((ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface)) {
				Main.LocalPlayer.mouseInterface = true;

				if (ValidItemFunc == null || ValidItemFunc(Main.mouseItem)) {
					bool oldLeft = Main.mouseLeft;
					bool oldLeftRelease = Main.mouseLeftRelease;
					bool oldRight = Main.mouseRight;
					bool oldRightRelease = Main.mouseRightRelease;

					if (IgnoreClicks)
						Main.mouseLeft = Main.mouseLeftRelease = Main.mouseRight = Main.mouseRightRelease = false;

					// Handle handles all the click and hover actions based on the context.
					storedItemBeforeHandle = StoredItem.Clone();
					ItemSlot.Handle(ref storedItem, Context);

					if(ItemChanged || ItemTypeChanged)
						OnItemChanged?.Invoke(storedItem);

					Main.mouseLeft = oldLeft;
					Main.mouseLeftRelease = oldLeftRelease;
					Main.mouseRight = oldRight;
					Main.mouseRightRelease = oldRightRelease;
				}
			}

			// Draw draws the slot itself and Item. Depending on context, the color will change, as will drawing other things like stack counts.
			ItemSlot.Draw(spriteBatch, ref storedItem, Context, rectangle.TopLeft());

			Main.inventoryScale = oldScale;
		}

		public void SetItem(Item item){
			storedItem = item.Clone();
		}

		public void SetItem(int itemType, int stack = 1){
			storedItem.SetDefaults(itemType);
			storedItem.stack = stack;
		}
	}
}
