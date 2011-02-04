﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using Dwarrowdelf.Client.TileControl;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	class RenderViewDetailed : RenderViewBase<RenderTileDetailed>
	{
		/* How many levels to show */
		const int MAXLEVEL = 4;

		public RenderViewDetailed()
		{
		}

		/*
		ISymbolDrawingCache m_symbolDrawingCache;
		public ISymbolDrawingCache SymbolDrawingCache
		{
			get { return m_symbolDrawingCache; }
			set
			{
				m_symbolDrawingCache = value;
				m_renderer.SymbolDrawingCache = value;
			}
		}

		public void InvalidateSymbols()
		{
			m_renderer.InvalidateSymbols();
		}
		*/

		protected override void MapChangedOverride(IntPoint3D ml)
		{
			// Note: invalidates the rendertile regardless of ml.Z
			// invalidate only if the change is within resolve limits (MAXLEVEL?)

			var x = ml.X - m_centerPos.X + m_renderData.Size.Width / 2;
			var y = ml.Y - m_centerPos.Y + m_renderData.Size.Height / 2;

			var p = new IntPoint(x, y);

			if (m_renderData.ArrayGrid.Bounds.Contains(p))
				m_renderData.ArrayGrid.Grid[p.Y, p.X].IsValid = false;
		}



		public override void Resolve()
		{
			//Debug.WriteLine("RenderView.Resolve");

			//var sw = Stopwatch.StartNew();

			var columns = m_renderData.Size.Width;
			var rows = m_renderData.Size.Height;
			var grid = m_renderData.ArrayGrid.Grid;

			if (m_invalid || (m_environment != null && (m_environment.VisibilityMode != VisibilityMode.AllVisible || m_environment.VisibilityMode != VisibilityMode.GlobalFOV)))
			{
				//Debug.WriteLine("RenderView.Resolve All");
				m_renderData.Clear();
				m_invalid = false;
			}

			bool isSeeAll = GameData.Data.User.IsSeeAll;

			int offsetX = m_centerPos.X - columns / 2;
			int offsetY = m_centerPos.Y - rows / 2;
			int offsetZ = m_centerPos.Z;

			// Note: we cannot access WPF stuff from different threads
			Parallel.For(0, rows, y =>
			{
				for (int x = 0; x < columns; ++x)
				{
					var p = new IntPoint(x, y);

					if (m_renderData.ArrayGrid.Grid[y, x].IsValid)
						continue;

					var ml = new IntPoint3D(offsetX + x, offsetY + (rows - y - 1), offsetZ);

					ResolveDetailed(out m_renderData.ArrayGrid.Grid[y, x], this.Environment, ml, m_showVirtualSymbols, isSeeAll);
				}
			});

			//sw.Stop();
			//Trace.WriteLine(String.Format("Resolve {0} ms", sw.ElapsedMilliseconds));
		}

		static void ResolveDetailed(out RenderTileDetailed tile, Environment env, IntPoint3D ml, bool showVirtualSymbols, bool isSeeAll)
		{
			tile = new RenderTileDetailed();
			tile.IsValid = true;

			if (env == null || !env.Bounds.Contains(ml))
				return;

			bool visible;

			if (isSeeAll)
				visible = true;
			else
				visible = TileVisible(ml, env);

			for (int z = ml.Z; z > ml.Z - MAXLEVEL; --z)
			{
				bool seeThrough;

				var p = new IntPoint3D(ml.X, ml.Y, z);

				if (tile.Top.SymbolID == SymbolID.Undefined)
				{
					GetTopTile(p, env, ref tile.Top, showVirtualSymbols);

					if (tile.Top.SymbolID != SymbolID.Undefined)
						tile.TopDarknessLevel = GetDarknessForLevel(ml.Z - z + (visible ? 0 : 1));
				}

				if (tile.Object.SymbolID == SymbolID.Undefined)
				{
					GetObjectTile(p, env, ref tile.Object, showVirtualSymbols);

					if (tile.Object.SymbolID != SymbolID.Undefined)
						tile.ObjectDarknessLevel = GetDarknessForLevel(ml.Z - z + (visible ? 0 : 1));
				}

				if (tile.Interior.SymbolID == SymbolID.Undefined)
				{
					GetInteriorTile(p, env, ref tile.Interior, showVirtualSymbols, out seeThrough);

					if (tile.Interior.SymbolID != SymbolID.Undefined)
						tile.InteriorDarknessLevel = GetDarknessForLevel(ml.Z - z + (visible ? 0 : 1));

					if (!seeThrough)
						break;
				}

				GetFloorTile(p, env, ref tile.Floor, showVirtualSymbols, out seeThrough);

				if (tile.Floor.SymbolID != SymbolID.Undefined)
					tile.FloorDarknessLevel = GetDarknessForLevel(ml.Z - z + (visible ? 0 : 1));

				if (!seeThrough)
					break;
			}

			if (tile.ObjectDarknessLevel == 0)
				tile.ObjectDarknessLevel = tile.TopDarknessLevel;

			if (tile.InteriorDarknessLevel == 0)
				tile.InteriorDarknessLevel = tile.ObjectDarknessLevel;

			if (tile.FloorDarknessLevel == 0)
				tile.FloorDarknessLevel = tile.InteriorDarknessLevel;
		}

		static void GetFloorTile(IntPoint3D ml, Environment env, ref RenderTileLayer tile, bool showVirtualSymbols, out bool seeThrough)
		{
			seeThrough = false;

			var intID = env.GetInteriorID(ml);

			if (intID == InteriorID.NaturalWall)
			{
				var flrMatInfo = env.GetFloorMaterial(ml);
				tile.SymbolID = SymbolID.Wall;
				tile.Color = flrMatInfo.Color;
				tile.BgColor = GameColor.None;
				return;
			}


			var flrID = env.GetFloorID(ml);

			if (flrID == FloorID.Undefined)
				return;

			if (flrID == FloorID.Empty)
			{
				if (showVirtualSymbols)
				{
					var flrId2 = env.GetFloor(ml + Direction.Down).ID;

					if (flrId2.IsSlope())
					{
						tile.Color = env.GetFloorMaterial(ml + Direction.Down).Color;

						switch (flrId2.ToDir().Reverse())
						{
							case Direction.North:
								tile.SymbolID = SymbolID.SlopeDownNorth;
								break;
							case Direction.NorthEast:
								tile.SymbolID = SymbolID.SlopeDownNorthEast;
								break;
							case Direction.East:
								tile.SymbolID = SymbolID.SlopeDownEast;
								break;
							case Direction.SouthEast:
								tile.SymbolID = SymbolID.SlopeDownSouthEast;
								break;
							case Direction.South:
								tile.SymbolID = SymbolID.SlopeDownSouth;
								break;
							case Direction.SouthWest:
								tile.SymbolID = SymbolID.SlopeDownSouthWest;
								break;
							case Direction.West:
								tile.SymbolID = SymbolID.SlopeDownWest;
								break;
							case Direction.NorthWest:
								tile.SymbolID = SymbolID.SlopeDownNorthWest;
								break;
							default:
								throw new Exception();
						}

						if (env.GetGrass(ml + Direction.Down))
						{
							// override the material color
							tile.Color = GameColor.Green;
							tile.BgColor = GameColor.DarkGreen;
						}
					}
					else
					{
						seeThrough = true;
					}
				}
				else
				{
					seeThrough = true;
				}

				return;
			}

			var matInfo = env.GetFloorMaterial(ml);
			tile.Color = matInfo.Color;
			tile.BgColor = GameColor.None;

			switch (flrID)
			{
				case FloorID.NaturalFloor:
					if (env.GetGrass(ml))
					{
						tile.SymbolID = SymbolID.Grass;
						// override the material color
						tile.Color = GameColor.DarkGreen;
						tile.BgColor = GameColor.Green;
					}
					else
					{
						tile.SymbolID = SymbolID.Floor;
					}
					break;

				case FloorID.Hole:
					tile.SymbolID = SymbolID.Floor;
					break;

				case FloorID.SlopeNorth:
				case FloorID.SlopeNorthEast:
				case FloorID.SlopeEast:
				case FloorID.SlopeSouthEast:
				case FloorID.SlopeSouth:
				case FloorID.SlopeSouthWest:
				case FloorID.SlopeWest:
				case FloorID.SlopeNorthWest:
					switch (flrID.ToDir())
					{
						case Direction.North:
							tile.SymbolID = SymbolID.SlopeUpNorth;
							break;
						case Direction.NorthEast:
							tile.SymbolID = SymbolID.SlopeUpNorthEast;
							break;
						case Direction.East:
							tile.SymbolID = SymbolID.SlopeUpEast;
							break;
						case Direction.SouthEast:
							tile.SymbolID = SymbolID.SlopeUpSouthEast;
							break;
						case Direction.South:
							tile.SymbolID = SymbolID.SlopeUpSouth;
							break;
						case Direction.SouthWest:
							tile.SymbolID = SymbolID.SlopeUpSouthWest;
							break;
						case Direction.West:
							tile.SymbolID = SymbolID.SlopeUpWest;
							break;
						case Direction.NorthWest:
							tile.SymbolID = SymbolID.SlopeUpNorthWest;
							break;
						default:
							throw new Exception();
					}

					if (env.GetGrass(ml))
					{
						// override the material color
						tile.Color = GameColor.DarkGreen;
						tile.BgColor = GameColor.Green;
					}

					break;

				default:
					throw new Exception();
			}
		}

		static void GetInteriorTile(IntPoint3D ml, Environment env, ref RenderTileLayer tile, bool showVirtualSymbols, out bool seeThrough)
		{
			var intID = env.GetInteriorID(ml);
			var intID2 = env.GetInteriorID(ml + Direction.Down);

			seeThrough = true;

			if (intID == InteriorID.Undefined)
				return;

			var matInfo = env.GetInteriorMaterial(ml);
			tile.Color = matInfo.Color;
			tile.BgColor = GameColor.None;

			switch (intID)
			{
				case InteriorID.Stairs:
					tile.SymbolID = SymbolID.StairsUp;
					break;

				case InteriorID.Empty:
					tile.SymbolID = SymbolID.Undefined;
					break;

				case InteriorID.NaturalWall:

					switch (matInfo.MaterialClass)
					{
						// these are see through, and GetFloorTile uses Wall symbol
						case MaterialClass.Gem:
							tile.SymbolID = SymbolID.GemOre;
							break;

						case MaterialClass.Mineral:
							tile.SymbolID = SymbolID.ValuableOre;
							break;

						default:
							tile.SymbolID = SymbolID.Wall;
							seeThrough = false;
							break;
					}

					break;

				case InteriorID.Sapling:
					tile.SymbolID = SymbolID.Sapling;
					tile.Color = GameColor.ForestGreen;
					break;

				case InteriorID.Tree:
					tile.SymbolID = SymbolID.Tree;
					tile.Color = GameColor.ForestGreen;
					break;

				default:
					throw new Exception();
			}

			if (showVirtualSymbols)
			{
				if (intID2 == InteriorID.Stairs)
				{
					if (intID == InteriorID.Stairs)
					{
						tile.SymbolID = SymbolID.StairsUpDown;
					}
					else if (intID == InteriorID.Empty)
					{
						tile.SymbolID = SymbolID.StairsDown;
						var downMatInfo = env.GetInteriorMaterial(ml + Direction.Down);
						tile.Color = downMatInfo.Color;
					}
				}
			}
		}

		static void GetObjectTile(IntPoint3D ml, Environment env, ref RenderTileLayer tile, bool showVirtualSymbols)
		{
			var ob = env.GetFirstObject(ml);

			if (ob == null)
				return;

			tile.SymbolID = ob.SymbolID;
			tile.Color = ob.GameColor;
			tile.BgColor = GameColor.None;
		}

		static void GetTopTile(IntPoint3D ml, Environment env, ref RenderTileLayer tile, bool showVirtualSymbols)
		{
			SymbolID id;

			DesignationType dt = env.Designations.ContainsPoint(ml);

			if (dt != DesignationType.None)
			{
				switch (dt)
				{
					case DesignationType.Mine:
						id = SymbolID.Rock;
						break;

					case DesignationType.CreateStairs:
						id = SymbolID.StairsUp;
						break;

					case DesignationType.FellTree:
						id = SymbolID.Log;
						break;

					default:
						throw new Exception();
				}
				tile.SymbolID = id;
				return;
			}

			int wl = env.GetWaterLevel(ml);

			if (wl == 0)
				return;

			wl = wl * 100 / TileData.MaxWaterLevel;

			id = SymbolID.Water;

			if (wl > 80)
			{
				tile.Color = GameColor.Aqua;
			}
			else if (wl > 60)
			{
				tile.Color = GameColor.DodgerBlue;
			}
			else if (wl > 40)
			{
				tile.Color = GameColor.Blue;
			}
			else if (wl > 20)
			{
				tile.Color = GameColor.Blue;
			}
			else
			{
				tile.Color = GameColor.MediumBlue;
			}

			tile.BgColor = GameColor.DarkBlue;

			tile.SymbolID = id;
		}
	}
}
