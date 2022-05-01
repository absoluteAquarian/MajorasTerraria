using MajorasTerraria.API;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.Exceptions;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace MajorasTerraria.IO {
	/// <summary>
	/// An object representing data in a world
	/// </summary>
	internal static class WorldData {
		static int _versionNumber;

		public static string LoadingMessage { get; private set; }

		/// <summary>
		/// Parses the WLD and TWLD data for a world, then constructs it into a file that Terrible Fate will use when regenerating the world<br/>
		/// The generated file will have the same name as the world file
		/// </summary>
		/// <param name="worldFilePath">The path to the world file</param>
		/// <param name="isCloudSave">Whether the world file exists on the cloud</param>
		/// <remarks>Invoking this method WILL clobber variables used to store world information</remarks>
		/// <exception cref="IOException"/>
		public static void Save(string worldFilePath, bool isCloudSave) {
			//Read the .wld and .twld files and process them into an object
			if (Path.GetExtension(worldFilePath) != ".wld")
				throw new IOException("Invalid file extension");

			if (!FileUtilities.Exists(worldFilePath, isCloudSave))
				throw new IOException("World file does not exist");

			Main.statusText = "Caching copy of the world...";

			string path = Program.SavePath;
			path = Path.Combine(path, "aA Mods", CoreMod.Instance.Name, "World Cache");
			Directory.CreateDirectory(path);

			path = Path.Combine(path, Path.GetFileNameWithoutExtension(worldFilePath) + ".dat");

			using MemoryStream writerstream = new();
			using BinaryWriter writer = new(writerstream);
			using MemoryStream readerStream = new(FileUtilities.ReadAllBytes(worldFilePath, isCloudSave));
			using BinaryReader reader = new(readerStream);

			_versionNumber = reader.ReadInt32();

			writer.Write(_versionNumber);

			Save_Version2(writer, reader);

			Save_WorldIO(writer, worldFilePath, isCloudSave);

			byte[] array = writerstream.ToArray();

			using BinaryWriter outputWriter = new(File.Open(path, FileMode.Create));

			outputWriter.Write(array);

			Main.statusText = "";
		}

		private static void Save_Version2(BinaryWriter writer, BinaryReader reader) {
			reader.BaseStream.Position = 0;

			if (!WorldFile.LoadFileFormatHeader(reader, out bool[] importance, out int[] positions))
				throw new IOException("Invalid file format header");

			void CheckPosition(int position) {
				if (reader.BaseStream.Position != positions[position])
					throw new IOException($"Mismatched position data from header (position {position})");
			}

			CheckPosition(0);

			WorldFile.LoadHeader(reader);

			Save_Version2_WriteHeaderData(writer);

			CheckPosition(1);

			WorldFile.LoadWorldTiles(reader, importance);

			Save_Version2_WriteTileData(writer);

			CheckPosition(2);

			WorldFile.LoadChests(reader);

			Save_Version2_WriteChestData(writer);

			CheckPosition(3);

			WorldFile.LoadSigns(reader);

			Save_Version2_WriteSignData(writer);

			CheckPosition(4);

			Mimick_LoadNPCs(reader, out int numTownies, out int numOtherNPCs);

			Save_Version2_WriteNPCData(writer, numTownies, numOtherNPCs);

			CheckPosition(5);

			WorldFile.LoadTileEntities(reader);

			Save_Version2_WriteTileEntityData(writer);

			CheckPosition(6);

			Mimick_LoadWeightedPressurePlates(reader, out var points);

			Save_Version2_WriteWeightedPressurePlateData(writer, points);

			CheckPosition(7);

			WorldFile.LoadTownManager(reader);

			WorldFile.SaveTownManager(writer);

			CheckPosition(8);

			WorldFile.LoadBestiary(reader, _versionNumber);

			WorldFile.SaveBestiary(writer);

			CheckPosition(9);

			WorldFile.LoadCreativePowers(reader, _versionNumber);

			WorldFile.SaveCreativePowers(writer);

			CheckPosition(10);

			if (WorldFile.LoadFooter(reader) != 0)
				throw new IOException("Invalid footer");
		}

		private static void Save_Version2_WriteHeaderData(BinaryWriter writer) {
			writer.Write(Main.worldID);

			writer.Write(Main.leftWorld);
			writer.Write(Main.rightWorld);
			writer.Write(Main.topWorld);
			writer.Write(Main.bottomWorld);

			writer.Write(Main.maxTilesX);
			writer.Write(Main.maxTilesY);

			writer.Write(Main.GameMode);

			writer.Write(Main.drunkWorld);
			writer.Write(Main.getGoodWorld);
			writer.Write(Main.tenthAnniversaryWorld);
			writer.Write(Main.dontStarveWorld);
			writer.Write(Main.notTheBeesWorld);

			writer.Write(WorldGen.crimson);

			short num = (short)TileID.Count;
			writer.Write(num);
			byte b = 0;
			byte b2 = 1;
			for (int i = 0; i < num; i++) {
				if (Main.tileFrameImportant[i])
					b = (byte)(b | b2);

				if (b2 == 128) {
					writer.Write(b);
					b = 0;
					b2 = 1;
				}
				else {
					b2 = (byte)(b2 << 1);
				}
			}

			if (b2 != 1)
				writer.Write(b);
		}

		private static void Save_Version2_WriteTileData(BinaryWriter writer) {
			byte[] array = new byte[15];

			Type TileIO = typeof(Mod).Assembly.GetType("Terraria.ModLoader.IO.TileIO");

			for (int x = 0; x < Main.maxTilesX; x++) {
				for (int y = 0; y < Main.maxTilesY; y++) {
					Tile tile = Main.tile[x, y];
					int num3 = 3;
					byte b;
					byte b2;
					byte b3 = b2 = (b = 0);
					bool flag = false;
					if (tile.HasTile && tile.TileType < TileID.Count)
						flag = true;

					if (flag) {
						b3 = (byte)(b3 | 2);
						array[num3] = (byte)tile.TileType;
						num3++;
						if (tile.TileType > 255) {
							array[num3] = (byte)(tile.TileType >> 8);
							num3++;
							b3 = (byte)(b3 | 0x20);
						}

						if (Main.tileFrameImportant[tile.TileType]) {
							short frameX = tile.TileFrameX;
							object[] param = new object[] { tile, null };
							TileIO.GetCachedMethod("VanillaSaveFrames").Invoke(null, param);
							frameX = (short)param[1];
							array[num3] = (byte)(frameX & 0xFF);
							num3++;
							array[num3] = (byte)((frameX & 0xFF00) >> 8);
							num3++;
							array[num3] = (byte)(tile.TileFrameY & 0xFF);
							num3++;
							array[num3] = (byte)((tile.TileFrameY & 0xFF00) >> 8);
							num3++;
						}

						if (tile.TileColor != 0) {
							b = (byte)(b | 8);
							array[num3] = tile.TileColor;
							num3++;
						}
					}

					if (tile.WallType != 0 && tile.WallType < WallID.Count) {
						b3 = (byte)(b3 | 4);
						array[num3] = (byte)tile.WallType;
						num3++;
						if (tile.WallColor != 0) {
							b = (byte)(b | 0x10);
							array[num3] = tile.WallColor;
							num3++;
						}
					}

					if (tile.LiquidAmount != 0) {
						b3 = (tile.LiquidType == LiquidID.Lava ? ((byte)(b3 | 0x10)) : ((tile.LiquidType != LiquidID.Honey) ? ((byte)(b3 | 8)) : ((byte)(b3 | 0x18))));
						array[num3] = tile.LiquidAmount;
						num3++;
					}

					if (tile.RedWire)
						b2 = (byte)(b2 | 2);

					if (tile.GreenWire)
						b2 = (byte)(b2 | 4);

					if (tile.BlueWire)
						b2 = (byte)(b2 | 8);

					int num4 = tile.IsHalfBlock ? 16 : ((tile.Slope != 0) ? ((int)tile.Slope + 1 << 4) : 0);
					b2 = (byte)(b2 | (byte)num4);
					if (tile.HasActuator)
						b = (byte)(b | 2);

					if (tile.IsActuated)
						b = (byte)(b | 4);

					if (tile.YellowWire)
						b = (byte)(b | 0x20);

					if (tile.WallType > 255) {
						array[num3] = (byte)(tile.WallType >> 8);
						num3++;
						b = (byte)(b | 0x40);
					}

					int num5 = 2;
					if (b != 0) {
						b2 = (byte)(b2 | 1);
						array[num5] = b;
						num5--;
					}

					if (b2 != 0) {
						b3 = (byte)(b3 | 1);
						array[num5] = b2;
						num5--;
					}

					short num6 = 0;
					int num7 = y + 1;
					int num8 = Main.maxTilesY - y - 1;
					while (num8 > 0 && Tile_isTheSameAs(tile, Main.tile[x, num7]) && TileID.Sets.AllowsSaveCompressionBatching[tile.TileType]) {
						num6 = (short)(num6 + 1);
						num8--;
						num7++;
					}

					y += num6;
					if (num6 > 0) {
						array[num3] = (byte)(num6 & 0xFF);
						num3++;
						if (num6 > 255) {
							b3 = (byte)(b3 | 0x80);
							array[num3] = (byte)((num6 & 0xFF00) >> 8);
							num3++;
						}
						else {
							b3 = (byte)(b3 | 0x40);
						}
					}

					array[num5] = b3;
					writer.Write(array, num5, num3 - num5);
				}
			}
		}

		private static bool Tile_isTheSameAs(Tile tile, Tile compTile) {
			if (tile.Get<TileWallWireStateData>().NonFrameBits != compTile.Get<TileWallWireStateData>().NonFrameBits)
				return false;

			if (tile.WallType != compTile.WallType || tile.LiquidAmount != compTile.LiquidAmount)
				return false;

			if (tile.LiquidAmount > 0 && tile.LiquidType != compTile.LiquidType)
				return false;

			if (tile.HasTile) {
				if (tile.TileType != compTile.TileType)
					return false;

				if (Main.tileFrameImportant[tile.TileType] && (tile.TileFrameX != compTile.TileFrameX || tile.TileFrameY != compTile.TileFrameY))
					return false;
			}

			return true;
		}

		private static void Save_Version2_WriteChestData(BinaryWriter writer) {
			writer.Write(Main.chest.Count(c => c is not null));

			for (int i = 0; i < Main.maxChests; i++) {
				if (Main.chest[i] is not Chest chest)
					continue;

				writer.Write(chest.x);
				writer.Write(chest.y);
				writer.Write(chest.name);

				for (int c = 0; c < chest.item.Length; c++) {
					Item item = chest.item[c];

					if (item.IsAir || item.type >= ItemID.Count) {
						//Data in the .twld would overwrite data loaded here anyway
						writer.Write((byte)0);
						continue;
					}

					writer.Write((byte)1);

					writer.Write(item.type);
					writer.Write(item.stack);
					writer.Write(item.prefix);
				}
			}
		}

		private static void Save_Version2_WriteSignData(BinaryWriter writer) {
			writer.Write(Main.sign.Count(s => s is not null));

			for (int i = 0; i < 1000; i++) {
				if (Main.sign[i] is not Sign sign)
					continue;

				writer.Write(sign.x);
				writer.Write(sign.y);
				writer.Write(sign.text);
			}
		}

		private static void Mimick_LoadNPCs(BinaryReader reader, out int numTownies, out int numOtherNPCs) {
			int num = 0;
			bool flag = reader.ReadBoolean();
			numTownies = 0;
			while (flag) {
				NPC nPC = Main.npc[num];
				if (_versionNumber >= 190)
					nPC.SetDefaults(reader.ReadInt32());
				else
					nPC.SetDefaults(NPCID.FromLegacyName(reader.ReadString()));

				nPC.GivenName = reader.ReadString();
				nPC.position.X = reader.ReadSingle();
				nPC.position.Y = reader.ReadSingle();
				nPC.homeless = reader.ReadBoolean();
				nPC.homeTileX = reader.ReadInt32();
				nPC.homeTileY = reader.ReadInt32();
				if (_versionNumber >= 213 && ((BitsByte)reader.ReadByte())[0])
					nPC.townNpcVariationIndex = reader.ReadInt32();

				num++;
				flag = reader.ReadBoolean();
				
				numTownies++;
			}

			numOtherNPCs = 0;
			if (_versionNumber < 140)
				return;

			flag = reader.ReadBoolean();
			while (flag) {
				NPC nPC = Main.npc[num];
				if (_versionNumber >= 190)
					nPC.SetDefaults(reader.ReadInt32());
				else
					nPC.SetDefaults(NPCID.FromLegacyName(reader.ReadString()));

				nPC.position = reader.ReadVector2();
				num++;
				flag = reader.ReadBoolean();

				numOtherNPCs++;
			}
		}

		private static void Save_Version2_WriteNPCData(BinaryWriter writer, int numTownies, int numOtherNPCs) {
			writer.Write(numTownies);
			
			int i;
			for (i = 0; i < numTownies; i++) {
				NPC npc = Main.npc[i];

				writer.Write(npc.netID);
				writer.Write(npc.GivenName);
				writer.WriteVector2(npc.position);
				writer.Write(npc.homeless);
				writer.Write(npc.homeTileX);
				writer.Write(npc.homeTileY);
				writer.Write(npc.townNpcVariationIndex);
			}

			writer.Write(numOtherNPCs);

			int target = numTownies + numOtherNPCs;

			for (; i < target; i++) {
				NPC npc = Main.npc[i];

				writer.Write(npc.netID);
				writer.WriteVector2(npc.position);
			}
		}

		private static void Save_Version2_WriteTileEntityData(BinaryWriter writer) {
			int num = TileEntity.TileEntitiesNextID;

			writer.Write(num);
			for (int i = 0; i < num; i++)
				TileEntity.Write(writer, TileEntity.ByID[i], networkSend: true, lightSend: true);
		}

		private static void Mimick_LoadWeightedPressurePlates(BinaryReader reader, out IList<Point> weightedPressurePlates) {
			weightedPressurePlates = new List<Point>();

			PressurePlateHelper.Reset();
			PressurePlateHelper.NeedsFirstUpdate = true;

			int num = reader.ReadInt32();
			
			for (int i = 0; i < num; i++) {
				Point key = new(reader.ReadInt32(), reader.ReadInt32());
				PressurePlateHelper.PressurePlatesPressed.Add(key, new bool[255]);

				weightedPressurePlates.Add(key);
			}
		}

		private static void Save_Version2_WriteWeightedPressurePlateData(BinaryWriter writer, IList<Point> weightedPressurePlates) {
			writer.Write(weightedPressurePlates.Count);

			foreach (Point plate in weightedPressurePlates) {
				writer.Write(plate.X);
				writer.Write(plate.Y);
			}
		}

		private static void Save_WorldIO(BinaryWriter writer, string worldFilePath, bool isCloudSave) {
			worldFilePath = Path.ChangeExtension(worldFilePath, ".twld");

			if (!FileUtilities.Exists(worldFilePath, isCloudSave)) {
				writer.Write(0);
				return;  //No modded data to save
			}

			byte[] buf = FileUtilities.ReadAllBytes(worldFilePath, isCloudSave);

			if (buf[0] != 0x1F || buf[1] != 0x8B) {
				//Invalid file.  Do nothing
				writer.Write(0);
				return;
			}

			//Assume that the data is valid for brevity's sake.  It will still be read later
			writer.Write(buf.Length);
			writer.Write(buf);
		}

		/// <summary>
		/// Overwrite's the world's data from a Terrible Fate world data file
		/// </summary>
		/// <param name="dataPath">The path to the constructed file</param>
		/// <exception cref="IOException"/>
		public static void Load(string dataFile) {
			if (Path.GetExtension(dataFile) != ".dat")
				throw new IOException("Invalid file extension");

			string path = Program.SavePath;
			path = Path.Combine(path, "aA Mods", CoreMod.Instance.Name, "World Cache", dataFile);

			LoadingMessage = "Resetting world";

			WorldGen.clearWorld();

			using BinaryReader reader = new(File.OpenRead(path));

			_versionNumber = reader.ReadInt32();

			Load_Version2(reader);

			Load_WorldIO(reader);
		}

		private static void Load_Version2(BinaryReader reader) {
			Load_Version2_ReadHeaderData(reader, out bool[] importance);

			Load_Version2_ReadTileData(reader, importance);

			Load_Version2_ReadChestData(reader);

			Load_Version2_ReadSignData(reader);

			Load_Version2_ReadNPCData(reader);

			Load_Version2_ReadTileEntityData(reader);

			Load_Version2_ReadWeightedPressurePlateData(reader);

			LoadingMessage = "Loading miscellaneous data";

			WorldFile.LoadTownManager(reader);

			WorldFile.LoadBestiary(reader, _versionNumber);

			WorldFile.LoadCreativePowers(reader, _versionNumber);
		}

		private static void Load_Version2_ReadHeaderData(BinaryReader reader, out bool[] importance) {
			LoadingMessage = "Loading world header";

			Main.worldID = reader.ReadInt32();

			Main.leftWorld = reader.ReadSingle();
			Main.rightWorld = reader.ReadSingle();
			Main.topWorld = reader.ReadSingle();
			Main.bottomWorld = reader.ReadSingle();

			Main.maxTilesX = reader.ReadInt32();
			Main.maxTilesY = reader.ReadInt32();

			Main.GameMode = reader.ReadInt32();

			Main.drunkWorld = reader.ReadBoolean();
			Main.getGoodWorld = reader.ReadBoolean();
			Main.tenthAnniversaryWorld = reader.ReadBoolean();
			Main.dontStarveWorld = reader.ReadBoolean();
			Main.notTheBeesWorld = reader.ReadBoolean();

			WorldGen.crimson = reader.ReadBoolean();

			short num2 = reader.ReadInt16();
			importance = new bool[num2];
			byte b = 0;
			byte b2 = 128;
			for (int i = 0; i < num2; i++) {
				if (b2 == 128) {
					b = reader.ReadByte();
					b2 = 1;
				}
				else {
					b2 = (byte)(b2 << 1);
				}

				if ((b & b2) == b2)
					importance[i] = true;
			}
		}

		private static void Load_Version2_ReadTileData(BinaryReader reader, bool[] importance) {
			for (int x = 0; x < Main.maxTilesX; x++) {
				float percentage = (float)x / Main.maxTilesX;

				LoadingMessage = "Loading tile data - " + (int)(percentage * 100 + 1) + "%";

				for (int y = 0; y < Main.maxTilesY; y++) {
					int num2 = -1;
					byte b;
					byte b2 = b = 0;
					Tile tile = Main.tile[x, y];
					byte b3 = reader.ReadByte();
					if ((b3 & 1) == 1) {
						b2 = reader.ReadByte();
						if ((b2 & 1) == 1)
							b = reader.ReadByte();
					}

					byte b4;
					if ((b3 & 2) == 2) {
						tile.HasTile = true;
						if ((b3 & 0x20) == 32) {
							b4 = reader.ReadByte();
							num2 = reader.ReadByte();
							num2 = ((num2 << 8) | b4);
						}
						else {
							num2 = reader.ReadByte();
						}

						tile.TileType = (ushort)num2;
						if (importance[num2]) {
							tile.TileFrameX = reader.ReadInt16();
							tile.TileFrameY = reader.ReadInt16();
							if (tile.TileType == 144)
								tile.TileFrameY = 0;
						} else {
							tile.TileFrameX = -1;
							tile.TileFrameY = -1;
						}

						if ((b & 8) == 8)
							tile.TileColor = reader.ReadByte();
					}

					if ((b3 & 4) == 4) {
						tile.WallType = reader.ReadByte();
						if (tile.WallType >= WallID.Count)
							tile.WallType = 0;

						if ((b & 0x10) == 16)
							tile.WallColor = reader.ReadByte();
					}

					b4 = (byte)((b3 & 0x18) >> 3);
					if (b4 != 0) {
						tile.LiquidAmount = reader.ReadByte();
						if (b4 > 1) {
							if (b4 == 2)
								tile.LiquidType = LiquidID.Lava;
							else
								tile.LiquidType = LiquidID.Honey;
						}
					}

					if (b2 > 1) {
						if ((b2 & 2) == 2)
							tile.RedWire = true;

						if ((b2 & 4) == 4)
							tile.GreenWire = true;

						if ((b2 & 8) == 8)
							tile.BlueWire = true;

						b4 = (byte)((b2 & 0x70) >> 4);
						if (b4 != 0 && (Main.tileSolid[tile.TileType] || TileID.Sets.NonSolidSaveSlopes[tile.TileType])) {
							if (b4 == 1)
								tile.IsHalfBlock = true;
							else
								tile.Slope = (SlopeType)(b4 - 1);
						}
					}

					if (b > 0) {
						if ((b & 2) == 2)
							tile.HasActuator = true;

						if ((b & 4) == 4)
							tile.IsActuated = true;

						if ((b & 0x20) == 32)
							tile.YellowWire = true;

						if ((b & 0x40) == 64) {
							b4 = reader.ReadByte();
							tile.WallType = (ushort)((b4 << 8) | tile.WallType);
							if (tile.WallType >= WallID.Count)
								tile.WallType = 0;
						}
					}

					var num3 = (byte)((b3 & 0xC0) >> 6) switch {
						0 => 0,
						1 => reader.ReadByte(),
						_ => reader.ReadInt16(),
					};

					if (num2 != -1) {
						if (y <= Main.worldSurface) {
							if (y + num3 <= Main.worldSurface) {
								WorldGen.tileCounts[num2] += (num3 + 1) * 5;
							} else {
								int num4 = (int)(Main.worldSurface - y + 1.0);
								int num5 = num3 + 1 - num4;
								WorldGen.tileCounts[num2] += num4 * 5 + num5;
							}
						} else {
							WorldGen.tileCounts[num2] += num3 + 1;
						}
					}

					while (num3 > 0) {
						y++;
						num3--;
						// tML - significantly improve performance by directly accessing the relevant blocking data to copy. No need to copy mod data in this method.
						var tile2 = Main.tile[x, y];
						tile2.Get<LiquidData>() = tile.Get<LiquidData>();
						tile2.Get<WallTypeData>() = tile.Get<WallTypeData>();
						tile2.Get<TileTypeData>() = tile.Get<TileTypeData>();
						tile2.Get<TileWallWireStateData>() = tile.Get<TileWallWireStateData>();
					}
				}
			}
		}

		private static void Load_Version2_ReadChestData(BinaryReader reader) {
			LoadingMessage = "Loading chest data";

			int numChests = reader.ReadInt32();

			int i;
			for (i = 0; i < numChests; i++) {
				Chest chest = new() {
					x = reader.ReadInt32(),
					y = reader.ReadInt32(),
					name = reader.ReadString()
				};

				for (int c = 0; c < chest.item.Length; c++) {
					if (reader.ReadByte() == 0) {
						//Air item or its ID was a modded item
						continue;
					}

					chest.item[c] = new(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
				}
			}

			for (; i < Main.maxChests; i++)
				Main.chest[i] = null;
		}

		private static void Load_Version2_ReadSignData(BinaryReader reader) {
			LoadingMessage = "Loading sign data";

			int numSigns = reader.ReadInt32();

			int i;
			for (i = 0; i < numSigns; i++) {
				Main.sign[i] = new() {
					x = reader.ReadInt32(),
					y = reader.ReadInt32(),
					text = reader.ReadString()
				};
			}

			for (; i < 1000; i++)
				Main.sign[i] = null;
		}

		private static void Load_Version2_ReadNPCData(BinaryReader reader) {
			LoadingMessage = "Loading NPC data";

			int numTownies = reader.ReadInt32();

			int i;
			for (i = 0; i < numTownies; i++) {
				NPC npc = new();
				npc.SetDefaults(reader.ReadInt32());
				npc.GivenName = reader.ReadString();
				npc.position = reader.ReadVector2();
				npc.homeless = reader.ReadBoolean();
				npc.homeTileX = reader.ReadInt32();
				npc.homeTileY = reader.ReadInt32();
				npc.townNpcVariationIndex = reader.ReadInt32();

				Main.npc[i] = npc;
			}

			int numOtherNPCs = reader.ReadInt32();

			int target = numTownies + numOtherNPCs;

			for (; i < target; i++) {
				NPC npc = new();
				npc.SetDefaults(reader.ReadInt32());
				npc.position = reader.ReadVector2();

				Main.npc[i] = npc;
			}

			for (; i < Main.maxNPCs; i++)
				Main.npc[i] = new();
		}

		private static void Load_Version2_ReadTileEntityData(BinaryReader reader) {
			LoadingMessage = "Loading tile entity data";

			int num = reader.ReadInt32();

			TileEntity.Clear();

			for (int i = 0; i < num; i++) {
				TileEntity entity = TileEntity.Read(reader, networkSend: true, lightSend: true);
				entity.ID = TileEntity.AssignNewID();

				TileEntity.ByID[entity.ID] = entity;
				TileEntity.ByPosition[entity.Position] = entity;
			}
		}

		private static void Load_Version2_ReadWeightedPressurePlateData(BinaryReader reader) {
			LoadingMessage = "Loading weighted pressure plate data";

			int numPlates = reader.ReadInt32();

			PressurePlateHelper.Reset();
			PressurePlateHelper.NeedsFirstUpdate = true;

			for (int i = 0; i < numPlates; i++) {
				Point point = new(reader.ReadInt32(), reader.ReadInt32());

				PressurePlateHelper.PressurePlatesPressed.Add(point, new bool[255]);
			}
		}

		private static void Load_WorldIO(BinaryReader reader) {
			int bufferLength = reader.ReadInt32();

			if (bufferLength == 0) {
				//No modded data to read
				return;
			}

			byte[] buffer = reader.ReadBytes(bufferLength);

			var tag = TagIO.FromStream(new MemoryStream(buffer));

			var TileIO = typeof(Mod).Assembly.GetType("Terraria.ModLoader.IO.TileIO");
			var WorldIO = typeof(Mod).Assembly.GetType("Terraria.ModLoader.IO.WorldIO");

			LoadingMessage = "Loading modded tile data";

			TileIO.GetCachedMethod("LoadBasics").Invoke(null, new object[] { tag.GetCompound("tiles") });

			LoadingMessage = "Loading modded item frame data";

			TileIO.GetCachedMethod("LoadContainers").Invoke(null, new object[] { tag.GetCompound("containers") });
			
			LoadingMessage = "Loading modded NPCs";

			WorldIO.GetCachedMethod("LoadNPCs").Invoke(null, new object[] { tag.GetList<TagCompound>("npcs") });
			
			LoadingMessage = "Loading modded tile entities";

			try {
				TileIO.GetCachedMethod("LoadTileEntities").Invoke(null, new object[] { tag.GetList<TagCompound>("tileEntities") });
			}catch (CustomModDataException e) {
				throw new IOException("Invalid tile entity data was read", e);
			}

			LoadingMessage = "Loading modded chest data";
			
			WorldIO.GetCachedMethod("LoadChestInventory").Invoke(null, new object[] { tag.GetList<TagCompound>("chests") }); // Must occur after tiles are loaded
			
			LoadingMessage = "Loading modded bestiary data";

			WorldIO.GetCachedMethod("LoadNPCKillCounts").Invoke(null, new object[] { tag.GetList<TagCompound>("killCounts") });
			
			WorldIO.GetCachedMethod("LoadNPCBestiaryKills").Invoke(null, new object[] { tag.GetList<TagCompound>("bestiaryKills") });
			
			WorldIO.GetCachedMethod("LoadNPCBestiarySights").Invoke(null, new object[] { tag.GetList<TagCompound>("bestiarySights") });
			
			WorldIO.GetCachedMethod("LoadNPCBestiaryChats").Invoke(null, new object[] { tag.GetList<TagCompound>("bestiaryChats") });
			
			LoadingMessage = "Loading miscellaneous modded data";

			WorldIO.GetCachedMethod("LoadAnglerQuest").Invoke(null, new object[] { tag.GetCompound("anglerQuest") });
			
			WorldIO.GetCachedMethod("LoadTownManager").Invoke(null, new object[] { tag.GetList<TagCompound>("townManager") });
			
			LoadingMessage = "Loading ModSystem data";

			try {
				WorldIO.GetCachedMethod("LoadModData").Invoke(null, new object[] { tag.GetList<TagCompound>("modData") });
			}catch (CustomModDataException e) {
				throw new IOException("Invalid ModSystem data was read", e);
			}

			LoadingMessage = "Finalizing loading";

			WorldIO.GetCachedMethod("LoadAlteredVanillaFields").Invoke(null, new object[] { tag.GetCompound("alteredVanillaFields") });
		}
	}
}
