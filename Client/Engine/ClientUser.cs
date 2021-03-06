﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Dwarrowdelf.Messages;

namespace Dwarrowdelf.Client
{
	public sealed class ClientUser
	{
		static Dictionary<Type, Action<ClientUser, ClientMessage>> s_handlerMap;

		static ClientUser()
		{
			var messageTypes = Helpers.GetNonabstractSubclasses(typeof(ClientMessage));

			s_handlerMap = new Dictionary<Type, Action<ClientUser, ClientMessage>>(messageTypes.Count());

			foreach (var type in messageTypes)
			{
				var method = WrapperGenerator.CreateActionWrapper<ClientUser, ClientMessage>("HandleMessage", type);
				if (method != null)
					s_handlerMap[type] = method;
			}
		}

		public enum ClientUserState
		{
			None,
			LoggingIn,
			ReceivingLoginData,
			LoggedIn,
			Disconnected,
		}

		ClientUserState m_state;

		public event Action DisconnectEvent;

		public event Action SaveEvent;

		ReportHandler m_reportHandler;
		ChangeHandler m_changeHandler;

		IConnection m_connection;

		public int PlayerID { get; private set; }
		public bool IsSeeAll { get; private set; }

		World m_world;
		public World World { get { return m_world; } }

		MyTraceSource trace = new MyTraceSource("Client.User", "ClientUser");

		SynchronizationContext m_syncCtx;

		public event Action<string> IPMessageReceived;

		public ClientUser(IConnection connection)
		{
			m_state = ClientUserState.None;
			m_connection = connection;
		}

		// XXX add cancellationtoken
		public async Task LogOn(string name, IProgress<string> prog)
		{
			if (m_state != ClientUserState.None)
				throw new Exception();

			m_state = ClientUserState.LoggingIn;

			prog.Report("Logging in");

			Send(new Messages.LogOnRequestMessage() { Name = name });

			prog.Report("Waiting for logon reply");

			bool ok = false;

			while (true)
			{
				var msg = await m_connection.GetMessageAsync();

				if (msg == null)
					break;

				InvokeMessageHandler((ClientMessage)msg);

				if (msg is LogOnReplyEndMessage)
				{
					ok = true;
					break;
				}
			}

			if (m_connection.IsConnected == false)
				throw new Exception("Disconnected");

			if (!ok)
				throw new Exception("No LogOnReplyEnd");

			m_state = ClientUserState.LoggedIn;

			prog.Report("Logged in");
		}

		public void StartProcessingMessages()
		{
			m_syncCtx = SynchronizationContext.Current;

			m_connection.NewMessageEvent += _OnNewMessages;
			m_syncCtx.Post(OnNewMessages, null);
		}

		public void Send(ServerMessage msg)
		{
			if (m_connection == null)
			{
				trace.TraceWarning("Send: m_connection == null");
				return;
			}

			if (!m_connection.IsConnected)
			{
				trace.TraceWarning("Send: m_connection.IsConnected == false");
				return;
			}

			m_connection.Send(msg);
		}

		public void SendLogOut()
		{
			Send(new Messages.LogOutRequestMessage());
		}

		volatile bool m_onNewMessagesInvoked;

		void _OnNewMessages()
		{
			if (m_onNewMessagesInvoked)
				return;

			m_onNewMessagesInvoked = true;

			m_syncCtx.Post(OnNewMessages, null);
		}

		void OnNewMessages(object state)
		{
			Message msg;

			while (m_connection.TryGetMessage(out msg))
				InvokeMessageHandler((ClientMessage)msg);

			m_onNewMessagesInvoked = false;

			while (m_connection.TryGetMessage(out msg))
				InvokeMessageHandler((ClientMessage)msg);

			if (m_connection.IsConnected == false)
			{
				trace.TraceInformation("OnDisconnect");

				m_world = null;

				m_state = ClientUserState.Disconnected;

				if (DisconnectEvent != null)
					DisconnectEvent();

				DH.Dispose(ref m_connection);
			}
		}

		void InvokeMessageHandler(ClientMessage msg)
		{
			//trace.TraceVerbose("Received Message {0}", msg);

			var method = s_handlerMap[msg.GetType()];
			method(this, msg);
		}

		void HandleMessage(LogOnReplyBeginMessage msg)
		{
			m_state = ClientUserState.ReceivingLoginData;

			this.PlayerID = msg.PlayerID;
			this.IsSeeAll = msg.IsSeeAll;
			// XXX move to game engine or such
			this.GameMode = msg.GameMode;
		}

		// XXX move to game engine or such
		public GameMode GameMode { get; private set; }

		void HandleMessage(WorldDataMessage msg)
		{
			m_world = new World(msg.WorldData, this.PlayerID);

			m_reportHandler = new ReportHandler(m_world);
			m_changeHandler = new ChangeHandler(m_world);
		}

		void HandleMessage(ClientDataMessage msg)
		{
			if (msg.ClientData != null)
				ClientSaveHelper.Load(m_world, msg.ClientData);
		}

		void HandleMessage(LogOnReplyEndMessage msg)
		{
		}

		void HandleMessage(LogOutReplyMessage msg)
		{
		}

		void HandleMessage(ControllablesDataMessage msg)
		{
			bool b;

			switch (msg.Operation)
			{
				case ControllablesDataMessage.Op.Add:
					b = true;
					break;

				case ControllablesDataMessage.Op.Remove:
					b = false;
					break;

				default:
					throw new Exception();
			}

			foreach (var oid in msg.Controllables)
			{
				var l = m_world.GetObject<LivingObject>(oid);
				l.IsControllable = b;
			}
		}

		void HandleMessage(SaveClientDataRequestMessage msg)
		{
			string data = ClientSaveHelper.SerializeClientObjects(m_world);

			var reply = new Messages.SaveClientDataReplyMessage() { ID = msg.ID, Data = data };
			Send(reply);

			if (SaveEvent != null)
				SaveEvent();
		}

		void HandleMessage(ObjectDataMessage msg)
		{
			var data = msg.ObjectData;

			var ob = m_world.FindObject(data.ObjectID);

			if (ob == null)
				ob = m_world.CreateObject(data.ObjectID);

			ob.ReceiveObjectData(data);
		}

		void HandleMessage(ObjectDataEndMessage msg)
		{
			var ob = m_world.GetObject(msg.ObjectID);
			ob.ReceiveObjectDataEnd();
		}

		void HandleMessage(MapDataTerrainsMessage msg)
		{
			var env = m_world.GetObject<EnvironmentObject>(msg.Environment);
			env.SetTerrains(msg.Bounds, msg.TerrainData);
		}

		void HandleMessage(MapDataTerrainsListMessage msg)
		{
			var env = m_world.GetObject<EnvironmentObject>(msg.Environment);
			trace.TraceVerbose("Received TerrainData for {0} tiles", msg.TileDataList.Count());
			env.SetTerrains(msg.TileDataList);
		}

		void HandleMessage(IPOutputMessage msg)
		{
			if (this.IPMessageReceived != null)
				this.IPMessageReceived(msg.Text);
		}

		void HandleMessage(ReportMessage msg)
		{
			m_reportHandler.HandleReportMessage(msg);
		}

		void HandleMessage(ChangeMessage msg)
		{
			m_changeHandler.HandleChangeMessage(msg);
		}
	}
}
