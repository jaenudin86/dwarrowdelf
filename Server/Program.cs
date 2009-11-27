﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;


namespace MyGame
{
	class ServerLauncher
	{
		static void Main(string[] args)
		{
			bool debugServer = Properties.Settings.Default.DebugServer;

			Server server = new Server();

			if (debugServer)
			{
				server.TraceListener = new ConsoleDebugListener();
			}

			server.RunServer(false, null, null);
		}
	}
}