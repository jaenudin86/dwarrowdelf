﻿//#define NONPARALLEL

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using Dwarrowdelf.Client.TileControl;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	sealed class RenderView : RenderViewBase<RenderTile>
	{
		/* How many levels to show */
		const int MAXLEVEL = 4;
		static bool m_symbolToggler;

		public RenderView(TileControl.RenderData<TileControl.RenderTile> renderData)
			: base(renderData)
		{
			GameData.Data.Blink += OnBlink;
		}

		void OnBlink()
		{
			// XXX we should invalidate only the needed tiles
			Invalidate();
			m_symbolToggler = !m_symbolToggler;
		}

		protected override void MapChangedOverride(IntPoint3 ml)
		{
			Invalidate(ml);
		}

		public override bool Invalidate(IntPoint3 ml)
		{
			if (Contains(ml))
			{
				var p = MapLocationToRenderDataLocation(ml);
				int idx = m_renderData.GetIdx(p);
				m_renderData.Grid[idx].IsValid = false;
				return true;
			}
			else
			{
				return false;
			}
		}

		public override void Resolve()
		{
			//Debug.WriteLine("RenderView.Resolve");

			//var sw = Stopwatch.StartNew();

			var columns = m_renderData.Width;
			var rows = m_renderData.Height;

			// render everything when using LOS
			if (m_environment != null && m_environment.VisibilityMode == VisibilityMode.LivingLOS)
				m_renderData.Invalid = true;

			if (m_renderData.Invalid)
			{
				//Debug.WriteLine("RenderView.Resolve All");
				m_renderData.Clear();
				m_renderData.Invalid = false;
			}

			if (m_environment == null)
				return;

#if NONPARALLEL
			for(int y = 0; y < rows; ++y)
#else
			// Note: we cannot access WPF stuff from different threads
			Parallel.For(0, rows, y =>
#endif
			{
				int idx = m_renderData.GetIdx(0, y);

				for (int x = 0; x < columns; ++x, ++idx)
				{
					if (m_renderData.Grid[idx].IsValid)
						continue;

					var ml = RenderDataLocationToMapLocation(x, y);

					ResolveDetailed(out m_renderData.Grid[idx], this.Environment, ml, this.IsVisibilityCheckEnabled);
				}
			}
#if !NONPARALLEL
);
#endif

			//sw.Stop();
			//Trace.WriteLine(String.Format("Resolve {0} ms", sw.ElapsedMilliseconds));
		}

		static void ResolveDetailed(out RenderTile tile, EnvironmentObject env, IntPoint3 ml, bool isVisibilityCheckEnabled)
		{
			tile = new RenderTile();
			tile.IsValid = true;

			if (!env.Contains(ml))
				return;

			bool visible;

			if (isVisibilityCheckEnabled == false)
				visible = true;
			else
				visible = TileVisible(ml, env);

			for (int z = ml.Z; z > ml.Z - MAXLEVEL; --z)
			{
				bool seeThrough;

				var p = new IntPoint3(ml.X, ml.Y, z);

				if (tile.Top.SymbolID == SymbolID.Undefined)
				{
					GetTopTile(p, env, ref tile.Top);

					if (tile.Top.SymbolID != SymbolID.Undefined)
						tile.TopDarknessLevel = GetDarknessForLevel(ml.Z - z + (visible ? 0 : 1));
				}

				if (tile.Object.SymbolID == SymbolID.Undefined)
				{
					GetObjectTile(p, env, ref tile.Object);

					if (tile.Object.SymbolID != SymbolID.Undefined)
						tile.ObjectDarknessLevel = GetDarknessForLevel(ml.Z - z + (visible ? 0 : 1));
				}

				if (tile.Interior.SymbolID == SymbolID.Undefined)
				{
					GetInteriorTile(p, env, ref tile.Interior, out seeThrough);

					if (tile.Interior.SymbolID != SymbolID.Undefined)
						tile.InteriorDarknessLevel = GetDarknessForLevel(ml.Z - z + (visible ? 0 : 1));

					if (!seeThrough)
						break;
				}

				GetTerrainTile(p, env, ref tile.Terrain, out seeThrough);

				if (tile.Terrain.SymbolID != SymbolID.Undefined)
					tile.TerrainDarknessLevel = GetDarknessForLevel(ml.Z - z + (visible ? 0 : 1));

				if (!seeThrough)
					break;
			}

			if (tile.ObjectDarknessLevel == 0)
				tile.ObjectDarknessLevel = tile.TopDarknessLevel;

			if (tile.InteriorDarknessLevel == 0)
				tile.InteriorDarknessLevel = tile.ObjectDarknessLevel;

			if (tile.TerrainDarknessLevel == 0)
				tile.TerrainDarknessLevel = tile.InteriorDarknessLevel;
		}

		static void GetTerrainTile(IntPoint3 ml, EnvironmentObject env, ref RenderTileLayer tile, out bool seeThrough)
		{
			seeThrough = false;

			var td = env.GetTileData(ml);

			if (td.TerrainID == TerrainID.Undefined)
			{
				tile.SymbolID = SymbolID.Hidden;
				tile.Color = GameColor.DimGray;
				tile.BgColor = GameColor.None;
				return;
			}

			if (td.TerrainID == TerrainID.Empty)
			{
				tile.SymbolID = SymbolID.Empty;
				tile.Color = GameColor.None;
				tile.BgColor = GameColor.None;
				seeThrough = true;
				return;
			}

			var matInfo = Materials.GetMaterial(td.TerrainMaterialID);
			tile.Color = matInfo.Color;
			tile.BgColor = GameColor.None;

			switch (td.TerrainID)
			{
				case TerrainID.NaturalFloor:
					tile.SymbolID = SymbolID.Floor;

					if (matInfo.Category == MaterialCategory.Soil)
						tile.SymbolID = SymbolID.Sand;

					tile.BgColor = GetTerrainBackgroundColor(matInfo);

					// If the interior is "green", override the color to make the terrain greenish
					if (InteriorIsGreen(td.InteriorID))
					{
						tile.SymbolID = SymbolID.Empty;
						tile.BgColor = GameColor.Green;
						return;
					}

					break;

				case TerrainID.BuiltFloor:
					tile.SymbolID = SymbolID.Floor;
					break;

				case TerrainID.NaturalWall:
					tile.SymbolID = SymbolID.Wall;
					tile.BgColor = GameColor.SlateGray;
					break;

				case TerrainID.StairsDown:
					tile.SymbolID = SymbolID.StairsDown;
					break;

				case TerrainID.SlopeNorth:
				case TerrainID.SlopeNorthEast:
				case TerrainID.SlopeEast:
				case TerrainID.SlopeSouthEast:
				case TerrainID.SlopeSouth:
				case TerrainID.SlopeSouthWest:
				case TerrainID.SlopeWest:
				case TerrainID.SlopeNorthWest:
					switch (td.TerrainID.ToDir())
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

					// If the interior is "green", override the color to make the terrain greenish
					if (InteriorIsGreen(td.InteriorID))
					{
						// override the material color
						tile.Color = GameColor.DarkGreen;
						tile.BgColor = GameColor.Green;
					}
					else
					{
						tile.BgColor = GetTerrainBackgroundColor(matInfo);
					}

					break;

				default:
					throw new Exception();
			}
		}

		static GameColor GetTerrainBackgroundColor(MaterialInfo matInfo)
		{
			if (matInfo.Category == MaterialCategory.Rock)
				return GameColor.DarkSlateGray;
			else if (matInfo.Category == MaterialCategory.Soil)
				return GameColor.Sienna;
			else
				throw new Exception();
		}

		static bool InteriorIsGreen(InteriorID id)
		{
			switch (id)
			{
				case InteriorID.Grass:
				case InteriorID.Tree:
				case InteriorID.DeadTree:
				case InteriorID.Sapling:
				case InteriorID.Shrub:
					return true;

				default:
					return false;
			}
		}

		static void GetInteriorTile(IntPoint3 ml, EnvironmentObject env, ref RenderTileLayer tile, out bool seeThrough)
		{
			var td = env.GetTileData(ml);

			seeThrough = true;

			if (td.InteriorID == InteriorID.Undefined)
				return;

			var matInfo = Materials.GetMaterial(td.InteriorMaterialID);
			tile.Color = matInfo.Color;
			tile.BgColor = GameColor.None;

			switch (td.InteriorID)
			{
				case InteriorID.Stairs:
					if (td.TerrainID == TerrainID.StairsDown)
					{
						tile.SymbolID = SymbolID.StairsUpDown;
						// disable seethrough so that the terrain's stairsdown are not visible
						seeThrough = false;
					}
					else
					{
						tile.SymbolID = SymbolID.StairsUp;
					}
					break;

				case InteriorID.BuiltWall:
					tile.SymbolID = SymbolID.Wall;
					seeThrough = false;
					break;

				case InteriorID.Pavement:
					tile.SymbolID = SymbolID.Floor;
					seeThrough = false;
					break;

				case InteriorID.Empty:
					tile.SymbolID = SymbolID.Undefined;
					break;

				case InteriorID.Grass:
					switch (matInfo.ID)
					{
						case MaterialID.ReedGrass:
							tile.SymbolID = SymbolID.Grass4;
							break;

						case MaterialID.RyeGrass:
							tile.SymbolID = SymbolID.Grass2;
							break;

						case MaterialID.MeadowGrass:
							tile.SymbolID = SymbolID.Grass3;
							break;

						case MaterialID.HairGrass:
							tile.SymbolID = SymbolID.Grass;
							break;

						default:
							throw new Exception();
					}

					// Grass color should come from the symbol definition
					tile.Color = GameColor.None;
					break;

				case InteriorID.Sapling:
					{
						switch (td.InteriorMaterialID)
						{
							case MaterialID.Fir:
								tile.SymbolID = SymbolID.ConiferousSapling;
								break;

							case MaterialID.Pine:
								tile.SymbolID = SymbolID.ConiferousSapling2;
								break;

							case MaterialID.Birch:
								tile.SymbolID = SymbolID.DeciduousSapling;
								break;

							case MaterialID.Oak:
								tile.SymbolID = SymbolID.DeciduousSapling2;
								break;

							default:
								throw new Exception();
						}

						tile.Color = GameColor.ForestGreen;
					}
					break;

				case InteriorID.Tree:
					{
						switch (td.InteriorMaterialID)
						{
							case MaterialID.Fir:
								tile.SymbolID = SymbolID.ConiferousTree;
								break;

							case MaterialID.Pine:
								tile.SymbolID = SymbolID.ConiferousTree2;
								break;

							case MaterialID.Birch:
								tile.SymbolID = SymbolID.DeciduousTree;
								break;

							case MaterialID.Oak:
								tile.SymbolID = SymbolID.DeciduousTree2;
								break;

							default:
								throw new Exception();
						}

						tile.Color = GameColor.ForestGreen;
					}
					break;

				case InteriorID.DeadTree:
					{
						tile.SymbolID = SymbolID.DeadTree;
					}
					break;

				case InteriorID.Shrub:
					{
						tile.SymbolID = SymbolID.Shrub;
					}
					break;

				case InteriorID.Ore:
					switch (matInfo.Category)
					{
						case MaterialCategory.Gem:
							tile.SymbolID = SymbolID.GemOre;
							break;

						case MaterialCategory.Mineral:
							tile.SymbolID = SymbolID.ValuableOre;
							break;

						default:
							tile.SymbolID = SymbolID.Undefined;
							break;
					}
					break;

				default:
					throw new Exception();
			}
		}

		static void GetObjectTile(IntPoint3 ml, EnvironmentObject env, ref RenderTileLayer tile)
		{
			var ob = (ConcreteObject)env.GetFirstObject(ml);

			if (ob == null)
				return;

			tile.SymbolID = ob.SymbolID;
			tile.Color = ob.EffectiveColor;
			tile.BgColor = GameColor.None;
		}

		static void GetTopTile(IntPoint3 ml, EnvironmentObject env, ref RenderTileLayer tile)
		{
			SymbolID id;

			if (m_symbolToggler)
			{
				id = GetDesignationSymbolAt(env.Designations, ml);
				if (id != SymbolID.Undefined)
				{
					tile.SymbolID = id;
					tile.BgColor = GameColor.DimGray;
					return;
				}
			}

			id = GetConstructSymbolAt(env.ConstructManager, ml);
			if (id != SymbolID.Undefined)
			{
				tile.SymbolID = id;
				if (m_symbolToggler)
					tile.Color = GameColor.DarkGray;
				else
					tile.Color = GameColor.LightGray;
				return;
			}

			if (!m_symbolToggler)
			{
				id = GetInstallSymbolAt(env.InstallItemManager, ml);
				if (id != SymbolID.Undefined)
				{
					tile.SymbolID = id;
					tile.Color = GameColor.DarkGray;
					return;
				}
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

		public static SymbolID GetDesignationSymbolAt(Designation designation, IntPoint3 p)
		{
			var dt = designation.ContainsPoint(p);

			switch (dt)
			{
				case DesignationType.None:
					return SymbolID.Undefined;

				case DesignationType.Mine:
					return SymbolID.DesignationMine;

				case DesignationType.CreateStairs:
					return SymbolID.StairsUp;

				case DesignationType.Channel:
					return SymbolID.DesignationChannel;

				case DesignationType.FellTree:
					return SymbolID.Log;

				default:
					throw new Exception();
			}
		}

		public static SymbolID GetConstructSymbolAt(ConstructManager mgr, IntPoint3 p)
		{
			var dt = mgr.ContainsPoint(p);

			switch (dt)
			{
				case ConstructMode.None:
					return SymbolID.Undefined;

				case ConstructMode.Pavement:
					return SymbolID.Floor;

				case ConstructMode.Floor:
					return SymbolID.Floor;

				case ConstructMode.Wall:
					return SymbolID.Wall;

				default:
					throw new Exception();
			}
		}

		static SymbolID GetInstallSymbolAt(InstallItemManager mgr, IntPoint3 p)
		{
			var item = mgr.ContainsPoint(p);

			if (item == null)
				return SymbolID.Undefined;

			return item.SymbolID;
		}
	}
}