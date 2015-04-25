﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Dwarrowdelf;

using Dwarrowdelf.TerrainGen;
using System.Threading.Tasks;
using System.Threading;

namespace Dwarrowdelf.TerrainGen
{
	public static class TerrainHelpers
	{
		public static void CreateSoil(TerrainData data, int soilLimit)
		{
			int w = data.Width;
			int h = data.Height;

			for (int y = 0; y < h; ++y)
			{
				for (int x = 0; x < w; ++x)
				{
					int z = data.GetSurfaceLevel(x, y);

					if (z < soilLimit)
					{
						var p = new IntVector3(x, y, z - 1);

						data.SetTileDataNoHeight(p, TileData.GetNaturalWall(MaterialID.Loam));
					}
				}
			}
		}

		public static void CreateVegetation(TerrainData terrain, Random random, int vegetationLimit)
		{
			var grassMaterials = Materials.GetMaterials(MaterialCategory.Grass).ToArray();
			var woodMaterials = Materials.GetMaterials(MaterialCategory.Wood).ToArray();
			var berryMaterials = Materials.GetMaterials(MaterialCategory.Berry).ToArray();

			int baseSeed = random.Next();
			if (baseSeed == 0)
				baseSeed = 1;

			terrain.Size.Plane.Range().AsParallel().ForAll(p2d =>
			{
				int z = terrain.GetSurfaceLevel(p2d);

				var p = new IntVector3(p2d, z);

				if (z >= vegetationLimit)
					return;

				var td = terrain.GetTileData(p);

				if (td.WaterLevel > 0)
					return;

				if (terrain.GetMaterial(p.Down).Category != MaterialCategory.Soil)
					return;

				var r = new MWCRandom(p, baseSeed);

				int v = r.Next(100);

				if (v >= 95)
				{
					td.ID = TileID.Sapling;
					td.MaterialID = woodMaterials[r.Next(woodMaterials.Length)].ID;
				}
				else if (v >= 90)
				{
					td.ID = TileID.Tree;
					td.MaterialID = woodMaterials[r.Next(woodMaterials.Length)].ID;
				}
				else if (v >= 80)
				{
					td.ID = TileID.Shrub;
					td.MaterialID = berryMaterials[r.Next(berryMaterials.Length)].ID;
				}
				else
				{
					td.ID = TileID.Grass;
					td.MaterialID = grassMaterials[r.Next(grassMaterials.Length)].ID;
				}

				terrain.SetTileDataNoHeight(p, td);
			});
		}
	}
}
