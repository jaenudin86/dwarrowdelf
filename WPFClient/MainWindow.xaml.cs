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
using System.Diagnostics;

namespace MyGame
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	partial class MainWindow : Window
	{
		public static MainWindow s_mainWindow; // xxx

		public MainWindow()
		{
			s_mainWindow = this;
			Application.Current.MainWindow = this;
			this.WindowState = WindowState.Maximized;

			InitializeComponent();

			this.Width = 1024;
			this.Height = 600;

			this.PreviewKeyDown += Window_PreKeyDown;
			map.MouseDown += MapControl_MouseDown;
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			map.Focus();
		}

		Direction KeyToDir(Key key)
		{
			Direction dir;

			switch (key)
			{
				case Key.Up: dir = Direction.North; break;
				case Key.Down: dir = Direction.South; break;
				case Key.Left: dir = Direction.West; break;
				case Key.Right: dir = Direction.East; break;
				case Key.Home: dir = Direction.NorthWest; break;
				case Key.End: dir = Direction.SouthWest; break;
				case Key.PageUp: dir = Direction.NorthEast; break;
				case Key.PageDown: dir = Direction.SouthEast; break;
				default:
					throw new Exception();
			}

			return dir;
		}

		bool KeyIsDir(Key key)
		{
			switch (key)
			{
				case Key.Up: break;
				case Key.Down: break;
				case Key.Left: break;
				case Key.Right: break;
				case Key.Home: break;
				case Key.End: break;
				case Key.PageUp: break;
				case Key.PageDown: break;
				default:
					return false;
			}
			return true;
		}

		void Window_PreKeyDown(object sender, KeyEventArgs e)
		{
			//MyDebug.WriteLine("OnMyKeyDown");
			if (GameData.Data.Connection == null)
				return;

			if (KeyIsDir(e.Key))
			{
				e.Handled = true;
				Direction dir = KeyToDir(e.Key);
				if (GameData.Data.Player != null)
				{
					int tid = GameData.Data.Connection.GetNewTransactionID();
					GameData.Data.Connection.DoAction(new MoveAction(tid, GameData.Data.Player, dir));
				}
				else
				{
					map.CenterPos += IntVector.FromDirection(dir);
				}
			}
			else if (e.Key == Key.Space)
			{
				e.Handled = true;
				if (GameData.Data.Player != null)
				{
					int wtid = GameData.Data.Connection.GetNewTransactionID();
					GameData.Data.Connection.DoAction(new WaitAction(wtid, GameData.Data.Player, 1));
				}
				else
				{
					GameData.Data.Connection.Server.ProceedTurn();
				}
			}
			else if (e.Key == Key.Add)
			{
				e.Handled = true;
				map.TileSize += 8;
			}

			else if (e.Key == Key.Subtract)
			{
				e.Handled = true;
				if (map.TileSize <= 16)
					return;
				map.TileSize -= 8;
			}

		}

		void MapControl_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.RightButton == MouseButtonState.Pressed)
			{
				IntRect r = map.SelectionRect;

				if (r.Width > 1 || r.Height > 1)
					return;

				IntPoint ml = map.ScreenPointToMapLocation(e.GetPosition(map));

				map.SelectionRect = new IntRect(ml, new IntSize(1, 1));
			}
		}

		private void OnClearLogClicked(object sender, RoutedEventArgs e)
		{
			App.DebugWindow.logTextBox.Clear();
		}

		internal Environment Map
		{
			get { return map.Environment; }
			set { map.Environment = value; }
		}

		private void MenuItem_Click_Floor(object sender, RoutedEventArgs e)
		{
			var terrain = this.Map.World.AreaData.Terrains.Single(t => t.Name == "Dungeon Floor");
			IntRect r = map.SelectionRect;
			// ZZZ level 0
			GameData.Data.Connection.Server.SetTiles(new IntCube(r, 0), terrain.ID);
		}

		private void MenuItem_Click_Wall(object sender, RoutedEventArgs e)
		{
			var terrain = this.Map.World.AreaData.Terrains.Single(t => t.Name == "Dungeon Wall");
			IntRect r = map.SelectionRect;
			// ZZZ level 0
			GameData.Data.Connection.Server.SetTiles(new IntCube(r, 0), terrain.ID);
		}

		private void Get_Button_Click(object sender, RoutedEventArgs e)
		{
			var plr = GameData.Data.Player;
			if (!(plr.Environment is Environment))
				throw new Exception();

			var list = currentTileItems.SelectedItems.Cast<ClientGameObject>();

			if (list.Count() == 0)
				return;

			if (list.Contains(plr))
				return;

			Debug.Assert(list.All(o => o.Environment == plr.Environment));
			Debug.Assert(list.All(o => o.Location == plr.Location));

			int wtid = GameData.Data.Connection.GetNewTransactionID();
			GameData.Data.Connection.DoAction(new GetAction(wtid, plr, list.Cast<GameObject>()));
		}

		private void Drop_Button_Click(object sender, RoutedEventArgs e)
		{
			var list = inventoryListBox.SelectedItems.Cast<ClientGameObject>();

			if (list.Count() == 0)
				return;

			var plr = GameData.Data.Player;

			int wtid = GameData.Data.Connection.GetNewTransactionID();
			GameData.Data.Connection.DoAction(new DropAction(wtid, plr, list.Cast<GameObject>()));
		}

		private void LogOn_Button_Click(object sender, RoutedEventArgs e)
		{
			if (GameData.Data.Player == null)
				GameData.Data.Connection.Server.LogOnChar("tomba");
		}

		private void LogOff_Button_Click(object sender, RoutedEventArgs e)
		{
			if (GameData.Data.Player != null)
				GameData.Data.Connection.Server.LogOffChar();
		}
	}
}
