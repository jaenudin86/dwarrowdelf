﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using AvalonDock;

namespace Dwarrowdelf.Client.UI
{
	partial class ListItemEditTemplateDictionary
	{
		public void Button_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;

			var ob = (BaseObject)button.DataContext;

			var dlg = new ObjectEditDialog();
			dlg.DataContext = ob;
			dlg.Show();
		}
	}
}