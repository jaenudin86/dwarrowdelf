﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.ComponentModel;
using System.Diagnostics;

using MyGame;
using MyGame.Client;
using AStarTest;

namespace AStarTest
{
	public class MapControl : MapControlBase, INotifyPropertyChanged
	{
		public class TileInfo
		{
			public IntPoint Location { get; set; }
		}

		struct MapTile
		{
			public int Weight;
			public bool Blocked;
		}

		Grid2D<MapTile> m_map;

		const int MapWidth = 400;
		const int MapHeight = 400;

		int m_state;
		IntPoint m_from, m_to;

		bool m_removing;

		public TileInfo CurrentTileInfo { get; private set; } // used to inform the UI

		public MapControl()
		{
			this.CurrentTileInfo = new TileInfo();

			this.Focusable = true;

			this.TileSize = 32;

			m_map = new Grid2D<MapTile>(MapWidth, MapHeight);
			for (int y = 0; y < 350; ++y)
				m_map[5, y] = new MapTile() { Blocked = true };

			for (int y = 4; y < 14; ++y)
				m_map[15, y] = new MapTile() { Blocked = true };

			for (int y = 6; y < 11; ++y)
				m_map[10, y] = new MapTile() { Weight = 40 };

			base.CenterPos = new IntPoint(10, 10);
			ClearMap();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);
			/*
			m_from = new IntPoint(6, 0);
			m_to = new IntPoint(4, 0);
			DoAStar(m_from, m_to);
			Application.Current.Shutdown();*/
		}

		void ClearMap()
		{
			m_result = null;
			InvalidateTiles();
		}

		protected override UIElement CreateTile()
		{
			return new MapControlTile();
		}

		protected override void UpdateTile(UIElement _tile, IntPoint ml)
		{
			MapControlTile tile = (MapControlTile)_tile;

			tile.ClearTile();

			if (!m_map.Bounds.Contains(ml))
			{
				tile.Brush = Brushes.DarkBlue;
			}
			else
			{
				tile.Weight = m_map[ml].Weight;

				if (m_result != null && m_result.Nodes.ContainsKey(ml))
				{
					var node = m_result.Nodes[ml];
					tile.G = node.G;
					tile.H = node.H;

					if (node.Parent == null)
						tile.From = Direction.None;
					else
						tile.From = (node.Parent.Loc - node.Loc).ToDirection();

					if (m_path != null && m_path.Contains(ml))
						tile.Brush = Brushes.DarkGray;
				}

				if (m_map[ml].Blocked)
				{
					tile.Brush = Brushes.Blue;
				}
				else if (m_state > 0 && ml == m_from)
				{
					tile.Brush = Brushes.Green;
				}
				else if (m_state > 1 && ml == m_to)
				{
					tile.Brush = Brushes.Red;
				}
			}

			tile.InvalidateVisual();
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			IntPoint ml = ScreenPointToMapLocation(e.GetPosition(this));

			if (!m_map.Bounds.Contains(ml))
			{
				Console.Beep();
				return;
			}

			if (e.LeftButton == MouseButtonState.Pressed)
			{
				if (m_state == 0 || m_state == 3)
				{
					m_from = ml;
					m_state = 1;
					ClearMap();
				}
				else
				{
					m_to = ml;
					m_state = 2;
					DoAStar(m_from, ml);
					m_state = 3;
				}
			}
			else
			{
				var s = m_map[ml];
				m_removing = s.Blocked;
				s.Blocked = !s.Blocked;
				m_map[ml] = s;
			}

			InvalidateTiles();
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			IntPoint ml = ScreenPointToMapLocation(e.GetPosition(this));

			if (this.CurrentTileInfo.Location != ml)
			{
				this.CurrentTileInfo.Location = ml;
				Notify("CurrentTileInfo");
			}

			if (e.RightButton == MouseButtonState.Pressed)
			{
				if (!m_map.Bounds.Contains(ml))
				{
					Console.Beep();
					return;
				}

				var s = m_map[ml];
				s.Blocked = !m_removing;
				m_map[ml] = s;

				InvalidateTiles();
			}
		}

		bool LocValid(IntPoint p)
		{
			if (!m_map.Bounds.Contains(p))
				return false;

			if (m_map[p].Blocked)
				return false;

			return true;
		}

		long m_memUsed;
		public long MemUsed
		{
			get { return m_memUsed; }
			set { m_memUsed = value; Notify("MemUsed"); }
		}

		long m_ticksUsed;
		public long TicksUsed
		{
			get { return m_ticksUsed; }
			set { m_ticksUsed = value; Notify("TicksUsed"); }
		}


		IEnumerable<IntPoint> m_path;
		AStarResult m_result;

		void DoAStar(IntPoint src, IntPoint dst)
		{
			long startBytes, stopBytes;
			Stopwatch sw = new Stopwatch();
			startBytes = GC.GetTotalMemory(true);
			sw.Start();
			m_result = AStar.Find(src, dst, true, LocValid, l => m_map[l].Weight);
			sw.Stop();
			stopBytes = GC.GetTotalMemory(true);

			this.MemUsed = stopBytes - startBytes;
			this.TicksUsed = sw.ElapsedTicks;

			if (!m_result.PathFound)
			{
				m_path = null;
				return;
			}

			List<IntPoint> pathList = new List<IntPoint>();
			var n = m_result.LastNode;
			while (n.Parent != null)
			{
				pathList.Add(n.Loc);
				n = n.Parent;
			}
			m_path = pathList;
		}

		void Notify(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}

	class MapControlTile : UIElement
	{
		static Pen s_edgePen;

		static MapControlTile()
		{
			s_edgePen = new Pen(Brushes.Gray, 1);
			s_edgePen.Freeze();
		}

		public MapControlTile()
		{
			this.IsHitTestVisible = false;
		}

		public Brush Brush;
		public int G;
		public int H;
		public Direction From;
		public int Weight;

		public void ClearTile()
		{
			this.Brush = Brushes.Black;
			this.G = 0;
			this.H = 0;
			this.From = Direction.None;
			this.Weight = 0;
		}

		protected override void OnRender(DrawingContext dc)
		{
			dc.DrawRectangle(this.Brush, s_edgePen, new Rect(this.RenderSize));

			if (this.RenderSize.Width < 32)
				return;

			if (From != Direction.None)
			{
				var iv = IntVector.FromDirection(From);
				iv *= (int)Math.Round(this.RenderSize.Width / 3);
				Point mp = new Point(this.RenderSize.Width / 2, this.RenderSize.Height / 2);
				dc.DrawEllipse(Brushes.White, null, mp, 3, 3);
				dc.DrawLine(new Pen(Brushes.White, 2), mp, mp + new Vector(iv.X, -iv.Y));
			}

			if (this.Weight != 0)
			{
				var ft = new FormattedText(this.Weight.ToString(), System.Globalization.CultureInfo.CurrentCulture,
					FlowDirection.LeftToRight, new Typeface("Verdana"), 8, Brushes.White);
				dc.DrawText(ft, new Point(this.RenderSize.Width - ft.Width - 2, 2));
			}

			if (G != 0 || H != 0)
			{
				var ft = new FormattedText(G.ToString(), System.Globalization.CultureInfo.CurrentCulture,
					FlowDirection.LeftToRight, new Typeface("Verdana"), 8, Brushes.White);
				dc.DrawText(ft, new Point(2, this.RenderSize.Height - ft.Height - 2));

				ft = new FormattedText(H.ToString(), System.Globalization.CultureInfo.CurrentCulture,
					FlowDirection.LeftToRight, new Typeface("Verdana"), 8, Brushes.White);
				dc.DrawText(ft, new Point(this.RenderSize.Width - ft.Width - 2, this.RenderSize.Height - ft.Height - 2));

				ft = new FormattedText((G + H).ToString(), System.Globalization.CultureInfo.CurrentCulture,
					FlowDirection.LeftToRight, new Typeface("Verdana"), 8, Brushes.White);
				dc.DrawText(ft, new Point(2, 2));
			}
		}
	}
}
