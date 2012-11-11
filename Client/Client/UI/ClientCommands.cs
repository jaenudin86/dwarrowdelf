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

namespace Dwarrowdelf.Client.UI
{
	public static class ClientCommands
	{
		public static RoutedUICommand AutoAdvanceTurnCommand;
		public static RoutedUICommand OpenConsoleCommand;
		public static RoutedUICommand OpenFocusDebugCommand;

		static ClientCommands()
		{
			AutoAdvanceTurnCommand = new RoutedUICommand("Auto-advance turn", "AutoAdvanceTurn", typeof(ClientCommands),
				new InputGestureCollection() { new KeyGesture(Key.Space) });

			OpenConsoleCommand = new RoutedUICommand("Open Console", "OpenConsole", typeof(ClientCommands),
				new InputGestureCollection() { new KeyGesture(Key.Enter, ModifierKeys.Control) });

			OpenFocusDebugCommand = new RoutedUICommand("Open FocusDebug", "OpenFocusDebug", typeof(ClientCommands));
		}
	}
}
