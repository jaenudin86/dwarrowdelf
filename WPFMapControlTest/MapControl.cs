﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
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

using MyGame;
using MyGame.Client;

namespace WPFMapControlTest
{
	class MapControl : MapControlBase
	{
		Grid2D<byte> m_map;

		public MapControl()
		{
			m_map = new Grid2D<byte>(512, 512);
			for (int y = 0; y < m_map.Height; ++y)
			{
				for (int x = 0; x < m_map.Width; ++x)
				{
					m_map[x, y] = (x + (y % 2)) % 2 == 0 ? (byte)50 : (byte)255;
				}
			}
		}

		protected override UIElement CreateTile()
		{
			return new MapControlTile();
		}

		// called for each visible tile
		protected override void UpdateTile(UIElement _tile, IntPoint ml)
		{
			MapControlTile tile = (MapControlTile)_tile;

			Color c;

			if (m_map.Bounds.Contains(ml))
			{

				byte b = m_map[ml.X, ml.Y];
				c = Color.FromRgb(b, b, b);
			}
			else
			{
				c = Color.FromRgb(0, 0, 0);
			}

			if (c != tile.Color)
			{
				tile.Color = c;
			}
		}

		class MapControlTile : UIElement
		{
			public MapControlTile()
			{
				this.IsHitTestVisible = false;
			}

			public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
				"Color", typeof(Color), typeof(MapControlTile),
				new PropertyMetadata(ValueChangedCallback));

			public Color Color
			{
				get { return (Color)GetValue(ColorProperty); }
				set { SetValue(ColorProperty, value); }
			}

			static void ValueChangedCallback(DependencyObject ob, DependencyPropertyChangedEventArgs e)
			{
				((MapControlTile)ob).InvalidateVisual();
			}

			protected override void OnRender(DrawingContext drawingContext)
			{
				drawingContext.DrawRectangle(new SolidColorBrush(this.Color), null, new Rect(this.RenderSize));
			}
		}
	}
}
