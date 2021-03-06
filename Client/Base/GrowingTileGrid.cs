﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Client
{
	public sealed class GrowingTileGrid
	{
		TileData[,,] m_grid;
		public IntSize3 Size { get; private set; }

		public GrowingTileGrid()
			: this(new IntSize3())
		{
		}

		public GrowingTileGrid(IntSize3 size)
		{
			SetSize(size);
		}

		public void SetSize(IntSize3 size)
		{
			if (!this.Size.IsEmpty)
				throw new Exception();

			this.Size = size;
			m_grid = new TileData[size.Depth, size.Height, size.Width];

			Debug.Print("GrowingTileGrid.SetSize({0})", this.Size);
		}

		public void Grow(IntVector3 p)
		{
			if (!this.Size.Contains(p))
				DoGrow(p);
		}

		int Align256(int x)
		{
			if (x >= 0)
				return (x + 0xff) & ~0xff;
			else
				return x & ~0xff;
		}

		int Align16(int x)
		{
			if (x >= 0)
				return (x + 0xf) & ~0xf;
			else
				return x & ~0xf;
		}

		void DoGrow(IntVector3 p)
		{
			int nw, nh, nd;

			if (p.X < 0 || p.Y < 0 || p.Z < 0)
				throw new Exception();

			nw = Align256(Math.Max(this.Size.Width, p.X + 1));
			nh = Align256(Math.Max(this.Size.Height, p.Y + 1));
			nd = Align16(Math.Max(this.Size.Depth, p.Z + 1));

			var newGrid = new TileData[nd, nh, nw];

			/* XXX Array.Copy will probably give better speed */
			foreach (var l in this.Size.Range())
			{
				var src = m_grid[l.Z, l.Y, l.X];
				newGrid[l.Z, l.Y, l.X] = src;
			}

			m_grid = newGrid;
			this.Size = new IntSize3(nw, nh, nd);

			Debug.Print("GrowingTileGrid.Grow({0})", this.Size);
		}

		public void SetTileDataRange(ulong[] tileData, IntGrid3 bounds)
		{
			int i = 0;

			for (int z = bounds.Z; z < bounds.Z + bounds.Depth; ++z)
				for (int y = bounds.Y; y < bounds.Y + bounds.Rows; ++y)
					for (int x = bounds.X; x < bounds.X + bounds.Columns; ++x)
					{
						ulong v = tileData[i++];
						m_grid[z, y, x].Raw = v;
					}
		}

		public void SetTileData(IntVector3 p, TileData data)
		{
			m_grid[p.Z, p.Y, p.X] = data;
		}

		public TileData GetTileData(IntVector3 p)
		{
			return m_grid[p.Z, p.Y, p.X];
		}

		public TileID GetTileID(IntVector3 p)
		{
			return m_grid[p.Z, p.Y, p.X].ID;
		}

		public MaterialID GetMaterialID(IntVector3 p)
		{
			return m_grid[p.Z, p.Y, p.X].MaterialID;
		}

		public MaterialID GetSecondaryMaterialID(IntVector3 p)
		{
			return m_grid[p.Z, p.Y, p.X].SecondaryMaterialID;
		}

		public byte GetWaterLevel(IntVector3 p)
		{
			return m_grid[p.Z, p.Y, p.X].WaterLevel;
		}

		public TileFlags GetFlags(IntVector3 p)
		{
			return m_grid[p.Z, p.Y, p.X].Flags;
		}
	}
}
