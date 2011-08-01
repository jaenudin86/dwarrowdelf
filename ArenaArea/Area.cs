﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Dwarrowdelf;
using Dwarrowdelf.Server;
using Environment = Dwarrowdelf.Server.Environment;
using Dwarrowdelf.AI;

namespace MyArea
{
	public class Area
	{
		Random m_random = new Random(1234);

		const int NUM_SHEEP = 5;

		public void InitializeWorld(World world)
		{
			var envBuilder = new EnvironmentBuilder(new IntSize3D(64, 64, 4), VisibilityMode.AllVisible);

			TileData td;

			int surfaceLevel = 2;

			td = new TileData() { TerrainID = TerrainID.NaturalWall, TerrainMaterialID = MaterialID.Granite, InteriorID = InteriorID.Empty };
			FillVolume(envBuilder, new IntCuboid(envBuilder.Bounds.Plane, 0), td);
			FillVolume(envBuilder, new IntCuboid(envBuilder.Bounds.Plane, 1), td);

			td = new TileData() { TerrainID = TerrainID.NaturalFloor, TerrainMaterialID = MaterialID.Granite, InteriorID = InteriorID.Empty };
			FillVolume(envBuilder, new IntCuboid(envBuilder.Bounds.Plane, 2), td);

			td = new TileData() { TerrainID = TerrainID.Empty, InteriorID = InteriorID.Empty };
			FillVolume(envBuilder, new IntCuboid(envBuilder.Bounds.Plane, 3), td);

			td = new TileData() { TerrainID = TerrainID.NaturalWall, TerrainMaterialID = MaterialID.Granite, InteriorID = InteriorID.Empty };
			DrawRect(envBuilder, new IntRectZ(envBuilder.Bounds.Plane, 2), td);

			var env = envBuilder.Create(world);
			env.HomeLocation = new IntPoint3D(envBuilder.Width / 2, 4, surfaceLevel);


			/* Add Monsters */

			CreateSheep(env, surfaceLevel);

			{
				var builder = new LivingBuilder(LivingID.Wolf);
				var wolf = builder.Create(env.World);
				var ai = new Dwarrowdelf.AI.CarnivoreAI(wolf);
				wolf.SetAI(ai);

				wolf.MoveTo(env, GetRandomSurfaceLocation(env, surfaceLevel));
			}

			{
				var builder = new LivingBuilder(LivingID.Dragon);
				var dragon = builder.Create(env.World);
				var ai = new Dwarrowdelf.AI.MonsterAI(dragon);
				dragon.SetAI(ai);

				dragon.MoveTo(env, GetRandomSurfaceLocation(env, surfaceLevel));
			}
		}

		void CreateSheep(Environment env, int surfaceLevel)
		{
			var herd = new Herd();

			for (int i = 0; i < NUM_SHEEP; ++i)
			{
				var sheepBuilder = new LivingBuilder(LivingID.Sheep)
				{
					Color = this.GetRandomColor(),
				};
				var sheep = sheepBuilder.Create(env.World);
				var ai = new HerbivoreAI(sheep);
				ai.Herd = herd;
				sheep.SetAI(ai);

				CreateItems(sheep, i);

				sheep.MoveTo(env, GetRandomSurfaceLocation(env, surfaceLevel));
			}
		}

		void CreateItems(Living living, int numItems)
		{
			var gemMaterials = Materials.GetMaterials(MaterialCategory.Gem).ToArray();

			for (int i = 0; i < numItems; ++i)
			{
				var material = gemMaterials[m_random.Next(gemMaterials.Length)].ID;
				var builder = new ItemObjectBuilder(ItemID.Gem, material);
				var item = builder.Create(living.World);

				for (int t = 0; t < i; ++t)
				{
					// XXX gems inside gems
					var material2 = gemMaterials[m_random.Next(gemMaterials.Length)].ID;
					builder = new ItemObjectBuilder(ItemID.Gem, material2);
					var item2 = builder.Create(living.World);
					item2.MoveTo(item);
				}

				item.MoveTo(living);
			}
		}

		static void FillVolume(EnvironmentBuilder env, IntCuboid volume, TileData data)
		{
			foreach (var p in volume.Range())
				env.SetTileData(p, data);
		}

		static void DrawRect(EnvironmentBuilder env, IntRectZ area, TileData data)
		{
			foreach (var p in area.Range())
			{
				if ((p.Y != area.Y1 && p.Y != area.Y2 - 1) &&
					(p.X != area.X1 && p.X != area.X2 - 1))
					continue;

				env.SetTileData(p, data);
			}
		}

		GameColor GetRandomColor()
		{
			return (GameColor)m_random.Next((int)GameColor.NumColors - 1) + 1;
		}

		IntPoint3D GetRandomSurfaceLocation(Environment env, int zLevel)
		{
			IntPoint3D p;
			int iter = 0;

			do
			{
				if (iter++ > 10000)
					throw new Exception();

				p = new IntPoint3D(m_random.Next(env.Width), m_random.Next(env.Height), zLevel);
			} while (!EnvironmentHelpers.CanEnter(env, p));

			return p;
		}
	}
}