﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;

namespace Dwarrowdelf.Client.UI
{
	sealed class TileView : INotifyPropertyChanged
	{
		EnvironmentObject m_environment;
		IntPoint3D m_location;
		MovableObjectCollection m_objects;

		public TileView()
		{
			m_objects = new MovableObjectCollection();
		}

		public EnvironmentObject Environment
		{
			get { return m_environment; }

			set
			{
				if (m_environment == value)
					return;

				if (m_environment != null)
				{
					m_environment.MapTileTerrainChanged -= OnMapTerrainChanged;
					m_environment.MapTileObjectChanged -= OnMapObjectChanged;
				}

				m_environment = value;

				if (m_environment != null)
				{
					m_environment.MapTileTerrainChanged += OnMapTerrainChanged;
					m_environment.MapTileObjectChanged += OnMapObjectChanged;
				}

				Notify("Environment");
				NotifyTileChanges();
			}
		}

		public IntPoint3D Location
		{
			get { return m_location; }

			set
			{
				if (m_location == value)
					return;

				m_location = value;

				Notify("Location");
				NotifyTileChanges();
			}
		}

		void NotifyTileChanges()
		{
			NotifyTileTerrainChanges();
			NotifyTileObjectChanges();
		}

		void NotifyTileTerrainChanges()
		{
			Notify("Interior");
			Notify("Terrain");
			Notify("TerrainMaterial");
			Notify("InteriorMaterial");
			Notify("WaterLevel");
			Notify("Flags");
			Notify("MapElement");
		}

		void NotifyTileObjectChanges()
		{
			m_objects.Clear();

			if (this.Environment != null)
			{
				var obs = this.Environment.GetContents(this.Location);
				foreach (var ob in obs)
					m_objects.Add(ob);
			}
		}

		void OnMapTerrainChanged(IntPoint3D l)
		{
			if (l != this.Location)
				return;

			NotifyTileTerrainChanges();
		}

		void OnMapObjectChanged(MovableObject ob, IntPoint3D l, MapTileObjectChangeType changeType)
		{
			if (l != this.Location)
				return;

			switch (changeType)
			{
				case MapTileObjectChangeType.Add:
					Debug.Assert(!m_objects.Contains(ob));
					m_objects.Add(ob);
					break;

				case MapTileObjectChangeType.Remove:
					bool ok = m_objects.Remove(ob);
					Debug.Assert(ok);
					break;

				case MapTileObjectChangeType.Update:
					break;

				default:
					throw new Exception();
			}
		}

		public IEnumerable<MovableObject> Objects { get { return m_objects; } }

		public InteriorInfo Interior
		{
			get
			{
				if (this.Environment == null)
					return null;

				return this.Environment.GetInterior(this.Location);
			}
		}

		public MaterialInfo InteriorMaterial
		{
			get
			{
				if (this.Environment == null)
					return null;

				return this.Environment.GetInteriorMaterial(this.Location);
			}
		}

		public MaterialInfo TerrainMaterial
		{
			get
			{
				if (this.Environment == null)
					return null;

				return this.Environment.GetTerrainMaterial(this.Location);
			}
		}

		public TerrainInfo Terrain
		{
			get
			{
				if (this.Environment == null)
					return null;

				return this.Environment.GetTerrain(this.Location);
			}
		}

		public byte WaterLevel
		{
			get
			{
				if (this.Environment == null)
					return 0;

				return this.Environment.GetWaterLevel(this.Location);
			}
		}

		public TileFlags Flags
		{
			get
			{
				if (this.Environment == null)
					return TileFlags.None;

				return this.Environment.GetTileFlags(this.Location);
			}
		}

		public IDrawableElement MapElement
		{
			get
			{
				if (this.Environment == null)
					return null;

				return this.Environment.GetElementAt(this.Location);
			}
		}

		#region INotifyPropertyChanged Members

		void Notify(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}
}
