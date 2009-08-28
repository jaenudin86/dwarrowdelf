﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MyGame
{
	delegate void ObjectMoved(Environment e, IntPoint l);
	class ItemCollection : ObservableCollection<ClientGameObject> { }

	class ClientGameObject : GameObject, INotifyPropertyChanged
	{
		// XXX not re-entrant
		static ILOSAlgo s_losAlgo = new LOSShadowCast1();

		static Dictionary<ObjectID, WeakReference> s_objectMap = new Dictionary<ObjectID, WeakReference>();

		static void AddObject(ClientGameObject ob)
		{
			if (ob.ObjectID == ObjectID.NullObjectID)
				throw new ArgumentException();

			lock (s_objectMap)
				s_objectMap.Add(ob.ObjectID, new WeakReference(ob));
			GameData.Data.Objects = null;
		}

		public static ClientGameObject FindObject(ObjectID objectID)
		{
			if (objectID == ObjectID.NullObjectID)
				throw new ArgumentException();

			lock (s_objectMap)
			{
				if (s_objectMap.ContainsKey(objectID))
				{
					WeakReference weakref = s_objectMap[objectID];
					if (weakref.IsAlive)
						return (ClientGameObject)s_objectMap[objectID].Target;
					else
						s_objectMap.Remove(objectID);
				}

				return null;
			}
		}

		public static T FindObject<T>(ObjectID objectID) where T : ClientGameObject
		{
			if (objectID == ObjectID.NullObjectID)
				throw new ArgumentException();

			lock (s_objectMap)
			{
				if (s_objectMap.ContainsKey(objectID))
				{
					WeakReference weakref = s_objectMap[objectID];
					if (weakref.IsAlive)
						return (T)s_objectMap[objectID].Target;
					else
						s_objectMap.Remove(objectID);
				}

				return default(T);
			}
		}
		
		public static ClientGameObject[] GetObjects()
		{
			lock (s_objectMap)
			{
				return s_objectMap.
					Where(kvp => kvp.Value.IsAlive).
					Select(kvp => (ClientGameObject)kvp.Value.Target).
					ToArray();
			}
		}
		
		public ItemCollection Inventory { get; private set; }
		IntPoint m_location;
		Environment m_environment;

		uint m_losMapVersion;
		IntPoint m_losLocation;
		int m_visionRange;
		LocationGrid<bool> m_visionMap;

		public int VisionRange
		{
			get { return m_visionRange; }
			set { m_visionRange = value; m_visionMap = null; }
		}

		public int X { get { return this.Location.X; } }
		public int Y { get { return this.Location.Y; } }

		public event ObjectMoved ObjectMoved;

		public ClientGameObject(ObjectID objectID)
			: base(objectID)
		{
			this.Inventory = new ItemCollection();
			AddObject(this);
			this.Color = Colors.Black;
		}

		string m_name;
		public string Name {
			get { return m_name; }
			set { m_name = value; Notify("Name"); }
		}

		int m_symbolID;
		public int SymbolID
		{
			get { return m_symbolID; }
			set
			{
				m_symbolID = value;
				Notify("SymbolID");
				Notify("Drawing");
			}
		}

		public Color Color { 
			get; 
			set;
		}

		public DrawingImage Drawing
		{
			get
			{
				return new DrawingImage(GameData.Data.SymbolDrawings.GetDrawing(m_symbolID, this.Color));
			}
		}

		public void SetEnvironment(Environment map, IntPoint l)
		{
			if (this.Environment != null)
				this.Environment.RemoveObject(this, m_location);

			m_environment = map;
			m_location = l;

			if (m_environment != null)
				this.Environment.AddObject(this, m_location);

			if (ObjectMoved != null)
				ObjectMoved(m_environment, m_location);
		}

		public Environment Environment
		{
			get { return m_environment; }
		}

		public IntPoint Location
		{
			get { return m_location; }

			set
			{
				this.Environment.RemoveObject(this, m_location);

				m_location = value;

				this.Environment.AddObject(this, m_location);

				if(ObjectMoved != null)
					ObjectMoved(this.Environment, this.Location);
			}
		}

		public LocationGrid<bool> VisionMap
		{
			get
			{
				UpdateLOS();
				return m_visionMap;
			}
		}

		void UpdateLOS()
		{
			if (this.Environment == null)
				return;

			if (this.Environment.VisibilityMode != VisibilityMode.LOS)
				throw new Exception();

			if (m_losLocation == m_location &&
				m_losMapVersion == this.Environment.Version)
				return;

			if (m_visionMap == null)
			{
				m_visionMap = new LocationGrid<bool>(m_visionRange * 2 + 1, m_visionRange * 2 + 1,
					m_visionRange, m_visionRange);
				m_losMapVersion = 0;
			}

			var terrains = this.Environment.World.AreaData.Terrains;

			s_losAlgo.Calculate(m_location, m_visionRange,
				m_visionMap, this.Environment.Bounds,
				l => terrains[this.Environment.GetTerrainID(l)].IsWalkable == false);

			m_losMapVersion = this.Environment.Version;
			m_losLocation = m_location;
		}

		void Notify(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}
}
