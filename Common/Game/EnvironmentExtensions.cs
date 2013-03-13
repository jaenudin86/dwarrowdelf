﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf
{
	public static class EnvironmentExtensions
	{
		/// <summary>
		/// Return all possible move directions.
		/// XXX Some room for optimization...
		/// </summary>
		public static IEnumerable<Direction> GetDirectionsFrom(this IEnvironmentObject env, IntPoint3 p)
		{
			foreach (var dir in DirectionExtensions.PlanarUpDownDirections)
			{
				var d = AdjustMoveDir(env, p, dir);
				if (d != Direction.None)
					yield return d;
			}
		}

		/// <summary>
		/// Determine if a living can move from srcLoc to dir
		/// </summary>
		public static bool CanMoveFromTo(this IEnvironmentObject env, IntPoint3 srcLoc, Direction dir)
		{
			var dstLoc = srcLoc + dir;
			return CanMoveFrom(env, srcLoc, dir) && CanMoveTo(env, dstLoc, dir);
		}

		/// <summary>
		/// Determine if a living can move from srcLoc to dir, without considering the destination
		/// </summary>
		public static bool CanMoveFrom(this IEnvironmentObject env, IntPoint3 srcLoc, Direction dir)
		{
			Debug.Assert(dir.IsValid());

			if (env.Contains(srcLoc) == false)
				return false;

			var td = env.GetTileData(srcLoc);

			if (td.IsUndefined)
				return false;

			var srcTerrain = Terrains.GetTerrain(td.TerrainID);
			var srcInter = Interiors.GetInterior(td.InteriorID);
			var itemBlocks = (td.Flags & TileFlags.ItemBlocks) != 0;

			if (srcInter.IsBlocker || srcTerrain.IsBlocker || itemBlocks)
				return false;

			if (dir.IsPlanar())
				return true;

			if (dir == Direction.Up)
				return srcInter.ID == InteriorID.Stairs;

			if (dir == Direction.Down)
				return srcTerrain.ID == TerrainID.StairsDown;

			if (dir.ContainsDown())
			{
				return true;
			}

			if (dir.ContainsUp())
			{
				if (!srcTerrain.ID.IsSlope())
					return false;

				if (env.GetTerrainID(srcLoc + Direction.Up) != TerrainID.Empty)
					return false;

				return true;
			}

			return false;
		}

		/// <summary>
		/// Determine if a living can move to dir to dstLoc, without considering the source
		/// </summary>
		public static bool CanMoveTo(this IEnvironmentObject env, IntPoint3 dstLoc, Direction dir)
		{
			Debug.Assert(dir.IsValid());

			if (!env.Contains(dstLoc))
				return false;

			var td = env.GetTileData(dstLoc);

			if (td.IsUndefined)
				return false;

			var dstTerrain = Terrains.GetTerrain(td.TerrainID);
			var dstInter = Interiors.GetInterior(td.InteriorID);
			var itemBlocks = (td.Flags & TileFlags.ItemBlocks) != 0;

			if (dstInter.IsBlocker || dstTerrain.IsBlocker || !dstTerrain.IsSupporting || itemBlocks)
				return false;

			if (dir.IsPlanar())
				return true;

			if (dir == Direction.Up)
				return dstTerrain.ID == TerrainID.StairsDown;

			if (dir == Direction.Down)
				return dstInter.ID == InteriorID.Stairs;

			if (dir.ContainsUp())
			{
				return true;
			}

			if (dir.ContainsDown())
			{
				if (!dstTerrain.ID.IsSlope())
					return false;

				if (env.GetTerrainID(dstLoc + Direction.Up) != TerrainID.Empty)
					return false;

				return true;
			}

			return false;
		}

		/// <summary>
		/// Tile can be entered and stood upon
		/// </summary>
		public static bool CanEnter(this IEnvironmentObject env, IntPoint3 location)
		{
			if (!env.Contains(location))
				return false;

			var td = env.GetTileData(location);

			if (td.IsUndefined)
				return false;

			var terrain = Terrains.GetTerrain(td.TerrainID);
			var interior = Interiors.GetInterior(td.InteriorID);
			var itemBlocks = (td.Flags & TileFlags.ItemBlocks) != 0;

			return terrain.IsSupporting && !terrain.IsBlocker && !interior.IsBlocker && !itemBlocks;
		}

		/// <summary>
		/// Can the given tile be seen from any adjacent tile
		/// </summary>
		public static bool CanBeSeen(this IEnvironmentObject env, IntPoint3 location)
		{
			foreach (var d in DirectionExtensions.PlanarDirections)
			{
				var p = location + d;
				if (env.Contains(p) && env.GetTileData(p).IsSeeThrough)
					return true;
			}

			var pu = location + Direction.Up;
			if (env.Contains(pu) && env.GetTileData(pu).IsSeeThroughDown)
				return true;

			return false;
		}

		/// <summary>
		/// For PlanarUpDown directions, return Direction.None if the direction cannot be entered,
		/// or the direction, adjusted by slopes (i.e. or'ed with Up or Down)
		/// </summary>
		public static Direction AdjustMoveDir(this IEnvironmentObject env, IntPoint3 location, Direction dir)
		{
			Debug.Assert(dir.IsValid());
			Debug.Assert(dir != Direction.None);
			Debug.Assert(dir.IsPlanarUpDown());

			if (EnvironmentExtensions.CanMoveFromTo(env, location, dir))
				return dir;

			if (dir == Direction.Up || dir == Direction.Down)
				return Direction.None;

			if (EnvironmentExtensions.CanMoveFromTo(env, location, dir | Direction.Up))
				return dir | Direction.Up;

			if (EnvironmentExtensions.CanMoveFromTo(env, location, dir | Direction.Down))
				return dir | Direction.Down;

			return Direction.None;
		}

		/// <summary>
		/// Return enterable positions around the given location, based on positioning
		/// </summary>
		public static IEnumerable<IntPoint3> GetPositioningLocations(this IEnvironmentObject env, IntPoint3 pos,
			DirectionSet positioning)
		{
			return positioning.ToDirections().Select(d => pos + d).Where(p => CanEnter(env, p));
		}
	}
}