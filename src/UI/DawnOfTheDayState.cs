using GraphicsLib.Primitives;
using GraphicsLib.Utility.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace MajorasTerraria.UI {
	internal class DawnOfTheDayState : UIState {
		private UIElement topTextClipping, bottomTextClipping;
		private UIText topText, bottomText;

		private UIImage background;

		public float horizontalLineWidth;
		public float verticalLineHeight;

		private int animationState = -1;

		private const int State_GrowVertical = 0;
		private const int State_GrowHorizontal = 1;
		private const int State_Wait = 2;
		private const int State_ShrinkHorizontal = 3;
		private const int State_ShrinkVertical = 4;

		private int animationTimer, alphaTimer;
		private bool increaseAlpha;

		private const float LargeUITextSize = 32f;

		public const float DisplayWidth = 1000, DisplayHeight = 400;
		private const float TopTextScale = 1.8f;

		public override void OnInitialize() {
			background = new(CoreMod.Instance.Assets.Request<Texture2D>("Assets/DayDawnBackground", AssetRequestMode.ImmediateLoad)) {
				HAlign = 0.5f,
				VAlign = 0.5f
			};

			Append(background);

			topTextClipping = new UIElement() {
				OverflowHidden = true,  //Causes clipping
				Width = new(DisplayWidth, 0f),
				Height = new(DisplayHeight / 2f, 0f),
				Top = new(-DisplayHeight / 2f - 4, 0.5f),
				HAlign = 0.5f
			};

			Append(topTextClipping);

			bottomTextClipping = new UIElement() {
				OverflowHidden = true,  //Causes clipping
				Width = new(DisplayWidth, 0f),
				Height = new(DisplayHeight / 2f, 0f),
				Top = new(4, 0.5f),
				HAlign = 0.5f
			};

			Append(bottomTextClipping);

			//The text
			topText = new("Dawn of the Xth Day", TopTextScale, large: true) {
				HAlign = 0.5f,
				Top = new(10, 1f)
			};

			topTextClipping.Append(topText);

			bottomText = new("-X Hours Remain-", large: true) {
				HAlign = 0.5f,
				Top = new(-LargeUITextSize - 10, 0f)
			};

			bottomTextClipping.Append(bottomText);
		}

		const int AlphaTimerMax = 40;

		public void SetDay(int day) {
			if (day < 0 || day > 3)
				return;

			animationState = State_GrowVertical;
			animationTimer = -1;

			horizontalLineWidth = 0;
			verticalLineHeight = 0;

			increaseAlpha = true;
			alphaTimer = AlphaTimerMax;

			string phrase = day switch {
				1 => "Final",
				2 => "Second",
				3 => "First",
				_ => "?"
			};

			topText.SetText("Dawn of the " + phrase + " Day");

			bottomText.SetText("-" + (24 * day) + " Hours Remain-");
		}

		private void SetAnimation(int state) {
			animationState = state;
			animationTimer = -1;
		}

		public override void Update(GameTime gameTime) {
			if (animationState >= State_GrowVertical)
				animationTimer++;
			else
				animationTimer = 0;

			float alphaFactor = (AlphaTimerMax - alphaTimer) / (float)AlphaTimerMax;
			if (!increaseAlpha)
				alphaFactor = 1f - alphaFactor;

			background.Color = Color.White * alphaFactor;

			if (alphaTimer > 0)
				alphaTimer--;

			const float barHeight = 6;

			const float textMovementTotal = LargeUITextSize + 20;

			const int growVerticalTime = 12;

			const int growHorizontalTime = 65;
			const int growTextMoveStart = 32;
			const int textMoveDuration = 43;

			const int waitTime = 180;

			const int shrinkHorizontalTime = 65;
			const int shrinkHorizontalStart = 18;

			const int shrinkVerticalTime = 12;
			
			switch (animationState) {
				case State_GrowVertical:
					if (animationTimer == 0)
						horizontalLineWidth = barHeight;
					else if (animationTimer <= growVerticalTime) {
						float lerp = GetLerp(animationTimer, 0, growVerticalTime);

						verticalLineHeight = barHeight * lerp;
					} else if (animationTimer > growVerticalTime)
						SetAnimation(State_GrowHorizontal);
					
					break;
				case State_GrowHorizontal:
					if (animationTimer <= growHorizontalTime) {
						float lerp = GetLerp(animationTimer, 0, growHorizontalTime);

						horizontalLineWidth = (DisplayWidth - 10 - barHeight) * lerp + barHeight;
					}

					if (animationTimer >= growTextMoveStart && animationTimer <= growTextMoveStart + textMoveDuration) {
						float lerp = GetLerp(animationTimer, growTextMoveStart, growTextMoveStart + textMoveDuration);

						float moveTo = textMovementTotal * lerp;

						topText.Top.Pixels = 10
							- moveTo * TopTextScale;
						bottomText.Top.Pixels = -LargeUITextSize - 10
							+ moveTo;
					}

					if (animationTimer > growTextMoveStart + textMoveDuration)
						SetAnimation(State_Wait);
					
					break;
				case State_Wait:
					horizontalLineWidth = DisplayWidth - 10;
					topText.Top.Pixels = 10 - textMovementTotal * TopTextScale;
					bottomText.Top.Pixels = -LargeUITextSize - 10 + textMovementTotal;
					
					if (animationTimer >= waitTime)
						SetAnimation(State_ShrinkHorizontal);

					break;
				case State_ShrinkHorizontal:
					if (animationTimer >= shrinkHorizontalStart && animationTimer <= shrinkHorizontalStart + shrinkHorizontalTime) {
						float lerp = GetLerp(animationTimer, shrinkHorizontalStart, shrinkHorizontalStart + shrinkHorizontalTime);

						horizontalLineWidth = (DisplayWidth - 10 - barHeight) * (1f - lerp) + barHeight;
					}

					if (animationTimer <= textMoveDuration) {
						float lerp = GetLerp(animationTimer, 0, textMoveDuration);

						float moveTo = textMovementTotal * lerp;

						topText.Top.Pixels = 10
							- textMovementTotal * TopTextScale
							+ moveTo * TopTextScale;
						bottomText.Top.Pixels = -LargeUITextSize - 10
							+ textMovementTotal
							- moveTo;
					}

					if (animationTimer == shrinkHorizontalStart + shrinkHorizontalTime + 1 - (AlphaTimerMax - shrinkVerticalTime)) {
						alphaTimer = AlphaTimerMax;
						increaseAlpha = false;
					}

					if (animationTimer > shrinkHorizontalStart + shrinkHorizontalTime)
						SetAnimation(State_ShrinkVertical);
					
					break;
				case State_ShrinkVertical:
					if (animationTimer <= shrinkVerticalTime) {
						float lerp = GetLerp(animationTimer, 0, shrinkVerticalTime);

						verticalLineHeight = barHeight * (1f - lerp);
					} else if (animationTimer > shrinkVerticalTime) {
						SetAnimation(-1);
						horizontalLineWidth = 0;
					}
					
					break;
			}
		}

		private static float GetLerp(int timer, int start, int target) {
			int diff = target - start;

			float lerp = (float)(timer - start) / diff;

			return lerp * lerp * lerp;
		}

		protected override void DrawChildren(SpriteBatch spriteBatch) {
			if (horizontalLineWidth <= 0 || verticalLineHeight <= 0)
				return;
			
			base.DrawChildren(spriteBatch);

			spriteBatch.End();

			//Draw the horizontal line using GPU primitives
			Vector2 center = Main.ScreenSize.ToVector2() / 2f;
			float midX = horizontalLineWidth / 2f;
			float midY = verticalLineHeight / 2f;

			float borderMidX = Math.Max(midX + 2, midX * 1.5f);
			float borderMidY = Math.Max(midY + 2, midY * 1.5f);

			PrimitivePacket packet = new(PrimitiveType.TriangleList);

			packet.AddDraw(
				//Border, upper-left triangle
				new VertexPositionColor((center + new Vector2(-borderMidX, -borderMidY)).ScreenCoord(), Color.Black),  //0
				new VertexPositionColor((center + new Vector2(borderMidX, -borderMidY)).ScreenCoord(), Color.Black),   //1
				new VertexPositionColor((center + new Vector2(-borderMidX, borderMidY)).ScreenCoord(), Color.Black)    //2
			);
			packet.AddDraw(
				//Border, lower-right triangle
				new VertexPositionColor((center + new Vector2(-borderMidX, borderMidY)).ScreenCoord(), Color.Black),   //2
				new VertexPositionColor((center + new Vector2(borderMidX, -borderMidY)).ScreenCoord(), Color.Black),   //1
				new VertexPositionColor((center + new Vector2(borderMidX, borderMidY)).ScreenCoord(), Color.Black)     //3
			);

			PrimitiveDrawing.SubmitPacket(packet);
			
			packet = new(PrimitiveType.TriangleList);

			packet.AddDraw(
				//Inner line, upper-left triangle
				new VertexPositionColor((center + new Vector2(-midX, -midY)).ScreenCoord(), Color.White),  //0
				new VertexPositionColor((center + new Vector2(midX, -midY)).ScreenCoord(), Color.White),   //1
				new VertexPositionColor((center + new Vector2(-midX, midY)).ScreenCoord(), Color.White)    //2
			);
			packet.AddDraw(
				//Inner line, lower-right triangle
				new VertexPositionColor((center + new Vector2(-midX, midY)).ScreenCoord(), Color.White),   //2
				new VertexPositionColor((center + new Vector2(midX, -midY)).ScreenCoord(), Color.White),   //1
				new VertexPositionColor((center + new Vector2(midX, midY)).ScreenCoord(), Color.White)     //3
			);

			PrimitiveDrawing.SubmitPacket(packet);

			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, spriteBatch.GraphicsDevice.RasterizerState, null, Main.UIScaleMatrix);
		}
	}
}
