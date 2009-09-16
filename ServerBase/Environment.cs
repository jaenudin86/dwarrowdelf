﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;


namespace MyGame
{
	public delegate void MapChanged(Environment map, IntPoint3D l, int terrainID);

	public class Environment : ServerGameObject 
	{
		public event MapChanged MapChanged;

		TileGrid m_tileGrid;
		// XXX this is quite good for add/remove child, but bad for gettings objects at certain location
		KeyedObjectCollection[] m_contentArray;

		public uint Version { get; private set; }

		public VisibilityMode VisibilityMode { get; private set; }
		public int Width { get; private set; }
		public int Height { get; private set; }
		public int Depth { get; private set; }

		public Environment(World world, int width, int height, int depth, VisibilityMode visibilityMode)
			: base(world)
		{
			this.Version = 1;
			base.Name = "unnamed map";
			this.VisibilityMode = visibilityMode;

			this.Width = width;
			this.Height = height;
			this.Depth = depth;

			m_tileGrid = new TileGrid(this.Width, this.Height, this.Depth);
			m_contentArray = new KeyedObjectCollection[this.Depth];
			for (int i = 0; i < depth; ++i)
				m_contentArray[i] = new KeyedObjectCollection();
		}

		public IntRect Bounds2D
		{
			get { return new IntRect(0, 0, this.Width, this.Height); }
		}

		public IntCube Bounds
		{
			get { return new IntCube(0, 0, 0, this.Width, this.Height, this.Depth); }
		}

		public delegate bool ActionHandlerDelegate(ServerGameObject ob, GameAction action);

		Dictionary<IntPoint3D, ActionHandlerDelegate> m_actionHandlers = new Dictionary<IntPoint3D, ActionHandlerDelegate>();
		public void SetActionHandler(IntPoint3D p, ActionHandlerDelegate handler)
		{
			m_actionHandlers[p] = handler;
		}

		public override bool HandleChildAction(ServerGameObject child, GameAction action)
		{
			ActionHandlerDelegate handler;
			if (m_actionHandlers.TryGetValue(child.Location, out handler) == false)
				return false;

			return handler(child, action);
		}

		public int GetTerrainID(IntPoint3D l)
		{
			return m_tileGrid.GetTerrainID(l);
		}

		public void SetTerrain(IntPoint3D l, int terrainID)
		{
			Debug.Assert(this.World.IsWriteable);

			this.Version += 1;

			m_tileGrid.SetTerrainType(l, terrainID);

			if (MapChanged != null)
				MapChanged(this, l, terrainID);
		}

		public bool IsWalkable(IntPoint3D l)
		{
			return this.World.AreaData.Terrains[GetTerrainID(l)].IsWalkable;
		}

		// XXX not a good func. contents can be changed by the caller
		public IEnumerable<ServerGameObject> GetContents(IntPoint3D l)
		{
			var list = m_contentArray[l.Z];
			return list.Where(o => o.Location == l);
		}

		protected override void OnChildAdded(ServerGameObject child)
		{
			var list = m_contentArray[child.Z];
			Debug.Assert(!list.Contains(child));
			list.Add(child);
		}

		protected override void OnChildRemoved(ServerGameObject child)
		{
			var list = m_contentArray[child.Z];
			Debug.Assert(list.Contains(child));
			list.Remove(child);
		}

		protected override bool OkToAddChild(ServerGameObject ob, IntPoint3D p)
		{
			Debug.Assert(this.World.IsWriteable);

			if (!this.Bounds.Contains(p))
				return false;

			if (!this.IsWalkable(p))
				return false;

			return true;
		}

		protected override bool OkToMoveChild(ServerGameObject ob, IntVector3D dirVec, IntPoint3D p)
		{
			Debug.Assert(this.World.IsWriteable);

			if (!this.Bounds.Contains(p))
				return false;

			if (!this.IsWalkable(p))
				return false;

			if (dirVec.Z == 0)
				return true;

			if (dirVec.X != 0 || dirVec.Y != 0)
				return false;

			int tileID = m_tileGrid.GetTerrainID(ob.Location);
			// XXX Aaeeeaaaaargh!!
			int upID = this.World.AreaData.Terrains.Single(t => t.Name == "Stairs Up").ID;
			int downID = this.World.AreaData.Terrains.Single(t => t.Name == "Stairs Down").ID;

			if (tileID == upID && dirVec.Z == 1)
				return true;

			if (tileID == downID && dirVec.Z == -1)
				return true;

			return false;
		}

		protected override void OnChildMoved(ServerGameObject child, IntPoint3D oldLocation, IntPoint3D newLocation)
		{
			if (oldLocation.Z == newLocation.Z)
				return;

			var list = m_contentArray[oldLocation.Z];
			Debug.Assert(list.Contains(child));
			list.Remove(child);

			list = m_contentArray[newLocation.Z];
			Debug.Assert(!list.Contains(child));
			list.Add(child);
		}

		public override ClientMsgs.Message Serialize()
		{
			int[] arr = new int[this.Width * this.Height * this.Depth];
			List<ClientMsgs.Message> obList = new List<ClientMsgs.Message>();

			foreach (var p in this.Bounds.Range())
			{
				arr[p.X + p.Y * this.Width + p.Z * this.Width * this.Height] = GetTerrainID(p);
				var obs = GetContents(p);
				if (obs != null)
					obList.AddRange(obs.Select(o => o.Serialize()));
			}

			var msg = new ClientMsgs.FullMapData()
			{
				ObjectID = this.ObjectID,
				VisibilityMode = this.VisibilityMode,
				Bounds = this.Bounds,
				TerrainIDs = arr,
				ObjectData = obList,
			};

			return msg;
		}

		public override string ToString()
		{
			return String.Format("Environment({0})", this.ObjectID);
		}


		struct TileData
		{
			public int m_terrainID;
		}

		class TileGrid : Grid3DBase<TileData>
		{
			public TileGrid(int width, int height, int depth)
				: base(width, height, depth)
			{
			}

			public void SetTerrainType(IntPoint3D l, int terrainType)
			{
				base.Grid[GetIndex(l)].m_terrainID = terrainType;
			}

			public int GetTerrainID(IntPoint3D l)
			{
				return base.Grid[GetIndex(l)].m_terrainID;
			}

		}
	}
}
