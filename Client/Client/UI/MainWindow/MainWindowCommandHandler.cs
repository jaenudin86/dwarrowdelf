﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Dwarrowdelf.Client.UI
{
	sealed class MainWindowCommandHandler
	{
		MainWindow m_mainWindow;

		public MainWindowCommandHandler(MainWindow mainWindow)
		{
			m_mainWindow = mainWindow;
		}

		public void AddCommandBindings(GameMode mode)
		{
			m_mainWindow.CommandBindings.AddRange(new CommandBinding[] {
				new CommandBinding(ClientCommands.OpenConsoleCommand, OpenConsoleHandler),
				new CommandBinding(ClientCommands.OpenFocusDebugCommand, OpenFocusDebugHandler),
			});

			if (mode == GameMode.Undefined)
				return;

			// add common bindings
			m_mainWindow.CommandBindings.AddRange(new CommandBinding[] {
				new CommandBinding(ClientCommands.AutoAdvanceTurnCommand, AutoAdvanceTurnHandler),
			});

			m_mainWindow.CommandBindings.AddRange(new CommandBinding[] {
				new CommandBinding(ClientCommands.ToggleFullScreen, ToggleFullScreenHandler),
			});

			// add mode specific bindings
			switch (mode)
			{
				case GameMode.Fortress:
					foreach (var kvp in ClientTools.ToolDatas)
					{
						var toolMode = kvp.Value.Mode;
						m_mainWindow.CommandBindings.Add(new CommandBinding(kvp.Value.Command,
							(s, e) => m_mainWindow.ClientTools.ToolMode = toolMode));
					}
					break;

				case GameMode.Adventure:
					break;
			}
		}

		void ToggleFullScreenHandler(object sender, ExecutedRoutedEventArgs e)
		{
			App.GameWindow.ToggleFullScreen();
		}

		void AutoAdvanceTurnHandler(object sender, ExecutedRoutedEventArgs e)
		{
			GameData.Data.IsAutoAdvanceTurn = !GameData.Data.IsAutoAdvanceTurn;
		}

		void OpenConsoleHandler(object sender, ExecutedRoutedEventArgs e)
		{
			var dialog = new ConsoleDialog();
			dialog.Owner = m_mainWindow;
			dialog.Show();
		}

		void OpenFocusDebugHandler(object sender, ExecutedRoutedEventArgs e)
		{
			var dialog = new Dwarrowdelf.Client.UI.FocusDebugWindow();
			dialog.Owner = m_mainWindow;
			dialog.Show();
		}
	}
}
