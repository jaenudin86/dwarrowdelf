﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Dwarrowdelf.Client.TileControl
{
	public delegate void CenterPosChanged(object control, DoubleVector3 centerPos, IntVector3 diff);

	public abstract class TileControlCore3D : TileControlCore
	{
		/// <summary>
		/// Offset between screen based tiles and content based tiles
		/// </summary>
		Vector m_contentOffset;

		public event CenterPosChanged ScreenCenterPosChanged;

		protected TileControlCore3D()
		{
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			base.TileLayoutChanged += OnTileLayoutChanged;
		}

		void OnTileLayoutChanged(IntSize2 gridSize, double tileSize)
		{
			var iscp = this.ScreenCenterPos.ToIntVector3();

			m_contentOffset = new Vector(iscp.X - gridSize.Width / 2, iscp.Y - gridSize.Height / 2);
		}

		public double ScreenZ
		{
			get { return this.ScreenCenterPos.Z; }
		}

		DoubleVector3 m_screenCenterPos;
		public DoubleVector3 ScreenCenterPos
		{
			get { return m_screenCenterPos; }

			set
			{
				if (m_screenCenterPos == value)
					return;

				var oldscp = m_screenCenterPos;
				var scp = value;

				m_screenCenterPos = scp;

				var ioldscp = oldscp.ToIntVector3();
				var iscp = scp.ToIntVector3();

				var diff = iscp - ioldscp;

				if (diff.IsNull == false)
					base.InvalidateTileData();

				m_contentOffset = new Vector(iscp.X - this.GridSize.Width / 2,
					iscp.Y - this.GridSize.Height / 2);

				this.TileOffset = new Vector(scp.X - iscp.X, scp.Y - iscp.Y);

				if (this.ScreenCenterPosChanged != null)
					this.ScreenCenterPosChanged(this, scp, diff);
			}
		}

		public Point RenderTileToScreen(Point st)
		{
			return st + m_contentOffset;
		}

		public Point ScreenToRenderTile(Point mt)
		{
			return mt - m_contentOffset;
		}

		public Point RenderPointToScreen(Point p)
		{
			var st = RenderPointToRenderTile(p);
			return RenderTileToScreen(st);
		}

		public Point ScreenToRenderPoint(Point mt)
		{
			var st = ScreenToRenderTile(mt);
			return RenderTileToRenderPoint(st);
		}

		public DoubleVector3 RenderPointToScreen3(Point rp)
		{
			var st = RenderPointToScreen(rp);
			return new DoubleVector3(st.X, st.Y, this.ScreenZ);
		}
	}
}
